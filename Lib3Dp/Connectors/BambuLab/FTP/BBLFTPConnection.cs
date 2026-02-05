using FluentFTP;
using FluentFTP.GnuTLS;
using FluentFTP.Monitors;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Collections;
using System.Net;

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

		public event Action<string, BambuLab3MF>? OnLocal3MFAdded;
		public event Action<string[]>? OnLocal3MFRemoved;

		public bool IsConnected => FTP.IsConnected;

		public BBLFTPConnection(IPAddress address, string accessCode)
		{
			Logger = Logger.OfCategory($"{nameof(BBLFTPConnection)} {address}");

			FTP = new AsyncFtpClient(address.ToString(), "bblp", accessCode, port: 990, new FtpConfig()
			{
				EncryptionMode = FtpEncryptionMode.Explicit,
				CustomStream = typeof(GnuTlsStream),
				CustomStreamConfig = new GnuConfig()
				{
					
				},
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

			OnLocal3MFRemoved?.Invoke(e.Deleted.Where(e => e.EndsWith(".3mf")).ToArray());

			foreach (var addedFile in e.Added.Where(e => e.EndsWith(".3mf")))
			{
				try
				{
					var bbl3MF = await this.Download3MF(addedFile);

					OnLocal3MFAdded?.Invoke(addedFile, bbl3MF);
				}
				catch (Exception ex)
				{
					Logger.Error($"Error processing added 3MF file {addedFile}\n{ex}");
				}
			}
		}

		public async Task Connect()
		{
			await FTP.AutoConnect();

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

		public async Task<BambuLab3MF> Download3MF(string remotePath)
		{
			var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".3mf");

			try
			{
				using var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.None);

				await FTP.DownloadStream(fs, remotePath);

				fs.Position = 0;

				var result = BambuLab3MF.Load(fs);

				return result;
			}
			catch
			{
				try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
				throw;
			}
			finally
			{
				try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
			}
		}

		public void Dispose()
		{
			MonitorCancellationSource.Cancel();
			Monitor.Dispose();
			FTP.Dispose();

			GC.SuppressFinalize(this);
		}
	}
}
