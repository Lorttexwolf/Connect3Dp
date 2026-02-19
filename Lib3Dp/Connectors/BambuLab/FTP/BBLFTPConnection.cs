using FluentFTP;
using FluentFTP.GnuTLS;
using FluentFTP.Monitors;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using Lib3Dp.Files;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;
using Lib3Dp.Connectors.BambuLab.Constants;

namespace Lib3Dp.Connectors.BambuLab.FTP
{
	// https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection

	// TODO: Fix reconnection, push events for disconnected, and etc.

	internal class BBLFTPConnection : IDisposable
	{
		private readonly Logger Logger;

		public AsyncFtpClient? FTP;
		private BlockingAsyncFtpMonitor? Monitor;
		private CancellationTokenSource? MonitorCancellationSource;

		public event Action<string, string, BambuLab3MF>? OnLocal3MFAdded;
		public event Action<string[]>? OnLocal3MFRemoved;
		public event Action? OnDisconnected;

		public bool IsConnected => FTP != null && FTP.IsConnected;

		private const int MaxCacheSize = 100;

		private readonly ConcurrentDictionary<string, BambuLab3MF> Cached3MF = new();
		private readonly ConcurrentDictionary<string, string> PathToHash = new();

		private readonly LinkedList<string> LRU = new();
		private readonly object LRULock = new();

		private readonly SemaphoreSlim FTPDownloadLock = new(1, 1);
		private readonly SemaphoreSlim FTPReconnectLock = new(1, 1);

		private Channel<string> DownloadQueue = Channel.CreateUnbounded<string>();

		private readonly List<Task> DownloadWorkers = new();
		
		private readonly IMachineFileStore FileStore;
		private string? MachineID;

		public BBLFTPConnection(IMachineFileStore fileStore, Logger logger)
		{
			this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			this.FileStore = fileStore;

			this.MonitorCancellationSource = new();
		}

		private async Task Monitor_ChangeDetected(FtpMonitorEventArgs e)
		{
			Logger.Trace($"Monitor detected changes! {e}");

			var removed = e.Deleted.Where(e => e.EndsWith(".3mf")).ToArray();
			OnLocal3MFRemoved?.Invoke(removed);

			foreach (var r in removed)
			{
				PathToHash.TryRemove(r, out _);
			}

			foreach (var added in e.Added.Where(e => e.EndsWith(".3mf")))
			{
				await DownloadQueue.Writer.WriteAsync(added);
			}
		}

		public async Task ConnectAsync(string address, string accessCode, string machineID)
		{
			// store machine id for file handle generation
			this.MachineID = machineID;

			// If a previous queue was completed (from a disconnect), recreate it so workers can run again
			if (DownloadQueue.Reader.Completion.IsCompleted)
			{
				DownloadQueue = Channel.CreateUnbounded<string>();
				DownloadWorkers.Clear();
			}

			// Create FTP client and monitor for this connection
			FTP = new AsyncFtpClient(address.ToString(), "bblp", accessCode, port: 990, new FtpConfig()
			{
				EncryptionMode = FtpEncryptionMode.Explicit,
				ValidateAnyCertificate = true
			});

			MonitorCancellationSource = new();

			Monitor = new BlockingAsyncFtpMonitor(FTP, [ "/" ])
			{
				PollInterval = TimeSpan.FromSeconds(30),
				WaitForUpload = true,
				Recursive = true,
				ChangeDetected = Monitor_ChangeDetected
			};

			await FTP.AutoConnect();

			StartWorkers();

			if (!this.Monitor!.Active)
			{
				_ = Task.Run(async () =>
				{
					try
					{
						await Monitor!.Start(this.MonitorCancellationSource!.Token);
					}
					catch (Exception ex)
					{
						Logger.Error($"FTP Monitor error: {ex}");
					}
				});
			}
		}

		private void StartWorkers()
		{
			if (DownloadWorkers.Count > 0)
				return;

			for (int i = 0; i < 4; i++)
			{
				DownloadWorkers.Add(Task.Run(async () =>
				{
					await foreach (var path in DownloadQueue.Reader.ReadAllAsync())
					{
						try
						{
							var nPath = path[1..];

							var (bbl3MF, hash) = await Download3MF(path);

							OnLocal3MFAdded?.Invoke(nPath, hash, bbl3MF);
						}
						catch (Exception ex)
						{
							Logger.Error($"Worker failed processing {path}\n{ex}");
						}
					}
				}));
			}
		}

		public async Task DisconnectAsync()
		{
			// Cancel monitor
			try
			{
				MonitorCancellationSource?.Cancel();
			}
			catch { }

			// Complete the download queue so workers exit
			try
			{
				DownloadQueue.Writer.TryComplete();
			}
			catch { }

			// Wait for workers to finish
			try
			{
				if (DownloadWorkers.Count > 0)
				{
					await Task.WhenAll(DownloadWorkers.ToArray());
					DownloadWorkers.Clear();
				}
			}
			catch { }

			// Dispose monitor
			try
			{
				Monitor?.Dispose();
				Monitor = null;
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
				// best-effort
				try { FTP?.Dispose(); } catch { }
				FTP = null;
			}
		}

		private async Task EnsureConnected()
		{
			if (FTP != null && FTP.IsConnected)
				return;

			await FTPReconnectLock.WaitAsync();

			try
			{
				if (FTP == null || !FTP.IsConnected)
				{
					Logger.Warning("FTP disconnected. Raising OnDisconnected and Reconnecting...");
					try 
					{ 
						OnDisconnected?.Invoke(); 
					} 
					catch { }
					// We don't have connection details here, so just throw to indicate disconnected state
					throw new InvalidOperationException("FTP is not connected and auto-reconnect requires calling ConnectAsync with credentials.");
				}
			}
			finally
			{
				FTPReconnectLock.Release();
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
			await EnsureConnected();

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
				await EnsureConnected();

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

		public void Dispose()
		{
			try { MonitorCancellationSource?.Cancel(); } catch { }
			try { DownloadQueue.Writer.TryComplete(); } catch { }

			try { Monitor?.Dispose(); } catch { }
			try { FTP?.Dispose(); } catch { }

			GC.SuppressFinalize(this);
		}
	}
}
