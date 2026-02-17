using FluentFTP;
using FluentFTP.GnuTLS;
using FluentFTP.Monitors;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace Lib3Dp.Connectors.BambuLab.FTP
{
	// https://github.com/robinrodricks/FluentFTP/wiki/FTP-Connection

	internal class BBLFTPConnection : IDisposable
	{
		private readonly Logger Logger;

		public class Local3MFChangesEventArgs(List<LocalPrintJob> Added, List<string> Removed) : EventArgs
		{
			public List<LocalPrintJob> Added { get; } = Added;
			public List<string> Removed { get; } = Removed;
		}

		public readonly AsyncFtpClient FTP;
		private readonly BlockingAsyncFtpMonitor Monitor;
		private readonly CancellationTokenSource MonitorCancellationSource;

		public event Action<string, string, BambuLab3MF>? OnLocal3MFAdded;
		public event Action<string[]>? OnLocal3MFRemoved;

		public bool IsConnected => FTP.IsConnected;

		private const int MaxCacheSize = 100;

		private readonly ConcurrentDictionary<string, BambuLab3MF> Cached3MF = new();
		private readonly ConcurrentDictionary<string, string> PathToHash = new();

		private readonly LinkedList<string> LRU = new();
		private readonly object LRULock = new();

		private readonly SemaphoreSlim FTPDownloadLock = new(1, 1);
		private readonly SemaphoreSlim FTPReconnectLock = new(1, 1);

		private readonly Channel<string> DownloadQueue =
			Channel.CreateUnbounded<string>();

		private readonly List<Task> DownloadWorkers = new();

		public BBLFTPConnection(IPAddress address, string accessCode)
		{
			Logger = Logger.OfCategory($"{nameof(BBLFTPConnection)} {address}");

			FTP = new AsyncFtpClient(address.ToString(), "bblp", accessCode, port: 990, new FtpConfig()
			{
				EncryptionMode = FtpEncryptionMode.Explicit,
				ValidateAnyCertificate = true
			});

			Monitor = new BlockingAsyncFtpMonitor(FTP, ["/"])
			{
				PollInterval = TimeSpan.FromSeconds(30),
				WaitForUpload = true,
				Recursive = true,
				ChangeDetected = Monitor_ChangeDetected
			};

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

		public async Task ConnectAsync()
		{
			await FTP.AutoConnect();

			StartWorkers();

			if (!this.Monitor.Active)
			{
				_ = Task.Run(async () =>
				{
					try
					{
						await Monitor.Start(this.MonitorCancellationSource.Token);
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

							var bbl3MF = await Download3MF(path);

							OnLocal3MFAdded?.Invoke(nPath, bbl3MF.Item2, bbl3MF.Item1);
						}
						catch (Exception ex)
						{
							Logger.Error($"Worker failed processing {path}\n{ex}");
						}
					}
				}));
			}
		}

		private async Task EnsureConnected()
		{
			if (FTP.IsConnected)
				return;

			await FTPReconnectLock.WaitAsync();

			try
			{
				if (!FTP.IsConnected)
				{
					Logger.Warning("FTP disconnected. Reconnecting...");
					await FTP.AutoConnect();
					Logger.Info("FTP reconnected.");
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
				await FTP.DownloadStream(destinationStream, remotePath);
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

				await using var fs = new FileStream(
					Path.GetTempFileName(),
					FileMode.Create,
					FileAccess.ReadWrite,
					FileShare.None,
					81920,
					FileOptions.DeleteOnClose | FileOptions.SequentialScan
				);

				var hash = await DownloadFile(remotePath, fs);

				if (Cached3MF.TryGetValue(hash, out var cached))
				{
					PathToHash[remotePath] = hash;
					TouchLRU(hash);
					return (cached, hash);
				}

				var loaded = BambuLab3MF.Load(fs);

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
			MonitorCancellationSource.Cancel();
			DownloadQueue.Writer.Complete();

			Monitor.Dispose();
			FTP.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
