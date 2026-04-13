using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Lib3Dp.Connectors.BambuLab
{
	/// <summary>
	/// Captures camera (JPEG) streams for the Bambu Lab A1 & P1 series of machines. An avg of 30 FPM.
	/// </summary>
	internal class BBLEspLANCameraStreamer(string hostname, string accessCode)
	{
		public string HostName { get; } = hostname;
		public string AccessCode { get; } = accessCode;

		public event Action<byte[]>? OnJPEG;

		private CancellationTokenSource cts;

		private readonly Stopwatch frameStopwatch = Stopwatch.StartNew();
		private readonly long[] frameTimes = new long[256];
		private int frameIndex;
		private int frameCount;

		public double AverageFPS { get; private set; }

		public void Start()
		{
			cts = new CancellationTokenSource();
			Task.Run(() => RunLoop(cts.Token));
		}

		public void Stop()
		{
			cts?.Cancel();
		}

		private async Task RunLoop(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				try
				{
					using var tcp = new TcpClient();
					await tcp.ConnectAsync(HostName, 6000);

					using var ssl = new SslStream(tcp.GetStream(), false, (sender, cert, chain, errors) => true);

					await ssl.AuthenticateAsClientAsync(HostName);

					var authPacket = BuildAuthPacket(AccessCode);
					await ssl.WriteAsync(authPacket, ct);
					await ssl.FlushAsync(ct);

					await ReadLoop(ssl, ct);
				}
				catch (Exception)
				{
					await Task.Delay(2000, ct);
				}
			}
		}

		private async Task ReadLoop(Stream stream, CancellationToken ct)
		{
			var headerBuffer = new byte[16];

			while (!ct.IsCancellationRequested)
			{
				// Read exactly 16 bytes (header)
				await FillBuffer(stream, headerBuffer, ct);

				// Little endian payload size in bytes 0–3
				int frameSize = BitConverter.ToInt32(headerBuffer, 0);

				if (frameSize <= 0) continue;

				var jpegBuffer = new byte[frameSize];
				await FillBuffer(stream, jpegBuffer, ct);

				// Emit JPEG frame
				UpdateFrameTiming();
				OnJPEG?.Invoke(jpegBuffer);
			}
		}

		private void UpdateFrameTiming()
		{
			long now = frameStopwatch.ElapsedTicks;

			frameTimes[frameIndex] = now;
			frameIndex = (frameIndex + 1) % frameTimes.Length;
			if (frameCount < frameTimes.Length) frameCount++;

			// Average over last N seconds
			const double windowSeconds = 10.0;
			long windowTicks = (long)(windowSeconds * Stopwatch.Frequency);

			int validFrames = 0;
			long oldest = now;

			for (int i = 0; i < frameCount; i++)
			{
				long t = frameTimes[i];
				if (now - t <= windowTicks)
				{
					validFrames++;
					if (t < oldest) oldest = t;
				}
			}

			if (validFrames > 1)
			{
				double spanSeconds = (now - oldest) / (double)Stopwatch.Frequency;
				if (spanSeconds > 0)
					AverageFPS = validFrames / spanSeconds;
			}
		}

		private static async Task FillBuffer(Stream stream, byte[] buffer, CancellationToken ct)
		{
			int offset = 0;
			while (offset < buffer.Length)
			{
				int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
				if (read == 0) throw new EndOfStreamException();
				offset += read;
			}
		}

		private static byte[] BuildAuthPacket(string accessCode)
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms, Encoding.ASCII);

			bw.Write(0x40); // magic / version
			bw.Write(0x3000); // opcode/flags
			bw.Write(0); // reserved
			bw.Write(0); // reserved

			WriteFixed(bw, "bblp", 32); // username, bblp
			WriteFixed(bw, accessCode, 32); // access code

			return ms.ToArray();
		}

		private static void WriteFixed(BinaryWriter bw, string s, int length)
		{
			var bytes = Encoding.ASCII.GetBytes(s);
			bw.Write(bytes);
			if (bytes.Length < length) bw.Write(new byte[length - bytes.Length]);
		}
	}
}