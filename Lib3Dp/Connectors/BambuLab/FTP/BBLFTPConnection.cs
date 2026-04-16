using FluentFTP;
using FluentFTP.GnuTLS;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using Lib3Dp.Files;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Lib3Dp.Connectors.BambuLab.Constants;
using FluentFTP.Exceptions;

namespace Lib3Dp.Connectors.BambuLab.FTP
{
	// https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection

	internal class BBLFTPConnection : IAsyncDisposable
	{
		private class FluentFtpLogAdapter(Logger logger) : IFtpLogger
		{
			public void Log(FtpLogEntry entry)
			{
				if (entry.Severity == FtpTraceLevel.Error)
					logger.Error($"[FluentFTP] {entry.Message}");
				else if (entry.Severity == FtpTraceLevel.Warn)
					logger.Warning($"[FluentFTP] {entry.Message}");
			}
		}

		private readonly Logger Logger;

		public AsyncFtpClient? FTP;
		private CancellationTokenSource? cts;
		private Task? scanLoopTask;

		public event Action<string, string, BambuLab3MF>? OnLocal3MFAdded;
		public event Action<string[]>? OnLocal3MFRemoved;
		public event Action? OnDisconnected;
		public event Action? OnInitialScanComplete;

		public bool IsConnected
		{
			get
			{
				try { return FTP != null && FTP.IsConnected; }
				catch { return false; }
			}
		}

		private const int MaxCacheSize = 100;

		private readonly ConcurrentDictionary<string, BambuLab3MF> Cached3MF = new();
		private readonly ConcurrentDictionary<string, string> PathToHash = new();

		private readonly LinkedList<string> LRU = new();
		private readonly object LRULock = new();

		private readonly SemaphoreSlim FTPDownloadLock = new(1, 1);

		private readonly HashSet<string> _knownPaths = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Tracks files that failed to parse (corrupt ZIP, not a real 3MF, etc.)
		/// Keyed by path, value is (size, modified) at time of failure.
		/// Cleared when a file is removed or its size/modified changes.
		/// </summary>
		private readonly Dictionary<string, (long Size, DateTime Modified)> _failedFiles = new(StringComparer.OrdinalIgnoreCase);

		private readonly IMachineFileStore FileStore;
		private string? MachineID;

		public BBLFTPConnection(IMachineFileStore fileStore, Logger logger)
		{
			this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.FileStore = fileStore;
		}

		/// <exception cref="FtpException"></exception>
		public async Task ConnectAsync(string address, string accessCode, string machineID, bool useGnuTls = false)
		{
			if (IsConnected) return;

			_knownPaths.Clear();
			_failedFiles.Clear();
			this.MachineID = machineID;

			var ftpConfig = new FtpConfig()
			{
				EncryptionMode = FtpEncryptionMode.Implicit,
				ValidateAnyCertificate = true,
				RetryAttempts = 3,
			};

			// Some models (X1C, X1E) require SSL session reuse, which .NET's SslStream
			// doesn't support. GnuTLS handles this transparently.
			if (useGnuTls)
			{
				ftpConfig.CustomStream = typeof(GnuTlsStream);
			}

			FTP = new AsyncFtpClient(address.ToString(), "bblp", accessCode, port: 990, ftpConfig, logger: new FluentFtpLogAdapter(Logger));

			cts = new CancellationTokenSource();

			await FTP.AutoConnect();

			scanLoopTask = Task.Run(() => ScanLoop(cts.Token));
		}

		private async Task ScanLoop(CancellationToken ct)
		{
			bool initialScanDone = false;

			while (!ct.IsCancellationRequested)
			{
				try
				{
					// Reconnect if needed
					if (!IsConnected)
					{
						await Task.Delay(5_000, ct);
						await FTP!.AutoConnect();
					}

					// Get current file listing
					var items = await FTP!.GetListing("/", FtpListOption.Recursive, ct);
					var currentFiles = new Dictionary<string, FtpListItem>(StringComparer.OrdinalIgnoreCase);

					foreach (var item in items)
					{
						if (item.Type == FtpObjectType.File && item.FullName.EndsWith(".3mf", StringComparison.OrdinalIgnoreCase))
							currentFiles[item.FullName] = item;
					}

					// Detect removed files
					var removed = _knownPaths.Where(p => !currentFiles.ContainsKey(p)).ToArray();
					if (removed.Length > 0)
					{
						foreach (var r in removed)
						{
							_knownPaths.Remove(r);
							_failedFiles.Remove(r);
							PathToHash.TryRemove(r, out _);
						}
						OnLocal3MFRemoved?.Invoke(removed);
					}

					// Also clear failed entries for files that have been removed
					foreach (var failedPath in _failedFiles.Keys.ToList())
					{
						if (!currentFiles.ContainsKey(failedPath))
							_failedFiles.Remove(failedPath);
					}

					// Build list of files to download (skip known and unchanged-failed files)
					var toDownload = new List<(string Path, FtpListItem Item)>();

					foreach (var (path, item) in currentFiles)
					{
						if (_knownPaths.Contains(path))
							continue;

						if (_failedFiles.TryGetValue(path, out var prev) && prev.Size == item.Size && prev.Modified == item.Modified)
							continue;

						toDownload.Add((path, item));
					}

					// Download sequentially — FluentFTP can't handle concurrent transfers on one connection
					foreach (var (path, item) in toDownload)
					{
						ct.ThrowIfCancellationRequested();
						try
						{
							var nPath = path.StartsWith("/") ? path[1..] : path;
							var (bbl3MF, hash) = await Download3MF(path);
							OnLocal3MFAdded?.Invoke(nPath, hash, bbl3MF);
							_knownPaths.Add(path);
							_failedFiles.Remove(path);
						}
						catch (OperationCanceledException) { throw; }
						catch (Exception ex)
						{
							Logger.Warning($"Failed to process {path}: {ex.Message}");
							_failedFiles[path] = (item.Size, item.Modified);
						}
					}

					// Signal initial scan complete after first successful pass
					if (!initialScanDone)
					{
						initialScanDone = true;
						try { OnInitialScanComplete?.Invoke(); } catch { }
					}
				}
				catch (OperationCanceledException) { return; }
				catch (Exception ex)
				{
					Logger.Warning($"FTP scan failed: {ex.Message}");

					// Attempt one reconnect
					if (!await TryReconnect(ct))
					{
						try { OnDisconnected?.Invoke(); } catch { }
						return;
					}

					// Signal initial scan even on failure so consumers aren't stuck waiting
					if (!initialScanDone)
					{
						initialScanDone = true;
						try { OnInitialScanComplete?.Invoke(); } catch { }
					}
				}

				// Wait before next poll
				try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
				catch (OperationCanceledException) { return; }
			}
		}

		private async Task<bool> TryReconnect(CancellationToken ct)
		{
			try
			{
				if (!IsConnected)
				{
					await Task.Delay(5_000, ct);
					await FTP!.AutoConnect();
				}
				return true;
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception ex)
			{
				Logger.Error($"FTP reconnect failed: {ex.Message}");
				return false;
			}
		}

		public async Task DisconnectAsync()
		{
			// Cancel scan loop
			try { cts?.Cancel(); } catch { }

			// Wait for scan loop to exit
			try
			{
				if (scanLoopTask != null)
				{
					await Task.WhenAny(scanLoopTask, Task.Delay(10_000));
					scanLoopTask = null;
				}
			}
			catch { }

			// Disconnect FTP
			try
			{
				if (FTP != null)
				{
					await FTP.Disconnect();
					FTP.Dispose();
					FTP = null;
				}
			}
			catch
			{
				try { FTP?.Dispose(); } catch { }
				FTP = null;
			}
		}

		private void EnsureConnected()
		{
			bool connected;
			try { connected = FTP != null && FTP.IsConnected; }
			catch { connected = false; }

			if (!connected)
			{
				try { OnDisconnected?.Invoke(); } catch { }
				throw new InvalidOperationException("FTP is not connected.");
			}
		}

		private void TouchLRU(string key)
		{
			lock (LRULock)
			{
				if (LRU.Contains(key))
					LRU.Remove(key);

				LRU.AddFirst(key);

				if (LRU.Count > MaxCacheSize)
				{
					string evicted = LRU.Last!.Value;
					LRU.RemoveLast();
					Cached3MF.TryRemove(evicted, out _);
				}
			}
		}

		public async Task<string> DownloadFile(string remotePath, Stream destinationStream)
		{
			EnsureConnected();

			await FTPDownloadLock.WaitAsync();

			try
			{
				await FTP!.DownloadStream(destinationStream, remotePath);
			}
			finally
			{
				FTPDownloadLock.Release();
			}

			destinationStream.Position = 0;

			string hash = ComputeHash(destinationStream);
			destinationStream.Position = 0;

			return hash;
		}

		public async Task<(BambuLab3MF, string)> Download3MF(string remotePath)
		{
			try
			{
				EnsureConnected();

				// Download into a temporary stream first so we can compute the hash
				using var tempStream = FileUtils.CreateTempFileStream();

				var hash = await DownloadFile(remotePath, tempStream);

				if (Cached3MF.TryGetValue(hash, out var cached))
				{
					PathToHash[remotePath] = hash;
					TouchLRU(hash);
					return (cached, hash);
				}

				// Ensure stream positioned for reading
				tempStream.Position = 0;

				// Store into file store if not present
				var nPath = remotePath.StartsWith("/") ? remotePath[1..] : remotePath;
				var handle = BBLFiles.HandleAs3MF(this.MachineID ?? string.Empty, nPath, hash);

				if (!this.FileStore.Contains(handle))
				{
					try
					{
						// FileStore.Store expects the stream positioned at start
						tempStream.Position = 0;
						await this.FileStore.Store(handle, tempStream);
					}
					catch (Exception ex)
					{
						Logger.Error(ex, $"Failed to store 3MF into FileStore for {nPath}");
					}
				}

				// Rewind and load into BambuLab3MF using the temp stream
				tempStream.Position = 0;

				var loaded = BambuLab3MF.Load(tempStream);

				Cached3MF[hash] = loaded;
				PathToHash[remotePath] = hash;
				TouchLRU(hash);

				return (loaded, hash);
			}
			catch
			{
				Logger.Warning($"FTP failed for {remotePath}. Attempting cache recovery.");

				if (PathToHash.TryGetValue(remotePath, out var oldHash) && Cached3MF.TryGetValue(oldHash, out var cached))
				{
					return (cached, oldHash);
				}

				throw;
			}
		}

		private static string ComputeHash(Stream stream)
		{
			using var sha = SHA256.Create();
			byte[] hash = sha.ComputeHash(stream);
			return Convert.ToHexString(hash);
		}

		public async ValueTask DisposeAsync()
		{
			await DisconnectAsync();
			GC.SuppressFinalize(this);
		}
	}
}
