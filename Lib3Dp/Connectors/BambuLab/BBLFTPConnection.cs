using FluentFTP;
using FluentFTP.GnuTLS;
using System.Net;

namespace Lib3Dp.Connectors.BambuLab
{
	internal class BBLFTPConnection
	{
		private readonly AsyncFtpClient FTP;

		public bool IsConnected => FTP.IsConnected;

		public BBLFTPConnection(IPAddress address, string accessCode)
		{
			this.FTP = new AsyncFtpClient(address.ToString(), "bblp", accessCode, port: 990, new FtpConfig()
			{
				EncryptionMode = FtpEncryptionMode.Explicit,
				CustomStream = typeof(GnuTlsStream),
				CustomStreamConfig = new GnuConfig(),
				ValidateAnyCertificate = true
			});
		}

		public async Task Connect()
		{
			await FTP.AutoConnect();
		}

		public Task<bool> DownloadStream(Stream outStream, string remotePath)
		{
			return FTP.DownloadStream(outStream, remotePath);
		}

		public async Task<IEnumerable<string>> List3MFFiles()
		{
			// Only look for files on the USB, or SD Card.
			// I've confirmed that the A1 series detects from the main FTP folder.
			// When the SD Card is removed from the A1 FTP cannot be used.
			// TODO: Determine one, or multiple file locations on other machines.

			var files3MF = (await FTP.GetListing()).Where(item => item.Type == FtpObjectType.File && item.Name.EndsWith(".3mf")).Select(item => item.Name);

			return files3MF;
		}
	}
}
