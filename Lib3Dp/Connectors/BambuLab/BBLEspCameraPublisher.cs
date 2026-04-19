using Lib3Dp.Cameras;
using Lib3Dp.Utilities;
using System.Diagnostics;

namespace Lib3Dp.Connectors.BambuLab
{
	/// <summary>
	/// Bridges a <see cref="BBLEspLANCameraStreamer"/> (proprietary TCP + JPEG from Bambu A1/P1 series)
	/// into MediaMTX by piping the JPEG frames through <c>ffmpeg</c> as MJPEG, transcoding to H264,
	/// and publishing RTSP to <paramref name="rtspTarget"/>.
	/// </summary>
	internal static class BBLEspCameraPublisher
	{
		public static async Task Run(string hostname, string accessCode, Uri rtspTarget, StreamPublisherOptions options, Logger logger, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				Process? ffmpeg = null;
				var streamer = new BBLEspLANCameraStreamer(hostname, accessCode);

				try
				{
					ffmpeg = StartFfmpeg(rtspTarget, options, logger);

					void OnJpeg(byte[] buffer)
					{
						try
						{
							ffmpeg?.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
						}
						catch (Exception ex)
						{
							logger.Warning($"Dropping frame: {ex.Message}");
						}
					}

					streamer.OnJPEG += OnJpeg;
					streamer.Start();

					await ffmpeg.WaitForExitAsync(ct);

					streamer.OnJPEG -= OnJpeg;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Camera publisher failed, restarting in 10s");
				}
				finally
				{
					try { streamer.Stop(); } catch { }
					try { ffmpeg?.Kill(entireProcessTree: true); } catch { }
					ffmpeg?.Dispose();
				}

				if (!ct.IsCancellationRequested)
				{
					try { await Task.Delay(TimeSpan.FromSeconds(10), ct); }
					catch (OperationCanceledException) { break; }
				}
			}
		}

		private static Process StartFfmpeg(Uri rtspTarget, StreamPublisherOptions options, Logger logger)
		{
			var psi = new ProcessStartInfo
			{
				FileName = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg",
				RedirectStandardInput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			var scaleW = options.MaxWidth.HasValue ? $"min(iw,{options.MaxWidth})" : "iw";
			var scaleH = options.MaxWidth.HasValue ? "-2" : (options.MaxHeight.HasValue ? $"min(ih,{options.MaxHeight})" : "ih");
			var scaleFilter = $"scale=w='{scaleW}':h={scaleH}:in_range=full:out_range=limited,format=yuv420p";

			psi.ArgumentList.Add("-hide_banner");
			psi.ArgumentList.Add("-loglevel"); psi.ArgumentList.Add("warning");
			psi.ArgumentList.Add("-probesize"); psi.ArgumentList.Add("32");
			psi.ArgumentList.Add("-analyzeduration"); psi.ArgumentList.Add("0");
			psi.ArgumentList.Add("-framerate"); psi.ArgumentList.Add(options.Framerate);
			psi.ArgumentList.Add("-f"); psi.ArgumentList.Add("mjpeg");
			psi.ArgumentList.Add("-i"); psi.ArgumentList.Add("pipe:0");
			psi.ArgumentList.Add("-c:v"); psi.ArgumentList.Add("libx264");
			psi.ArgumentList.Add("-preset"); psi.ArgumentList.Add("ultrafast");
			psi.ArgumentList.Add("-tune"); psi.ArgumentList.Add("zerolatency");
			psi.ArgumentList.Add("-crf"); psi.ArgumentList.Add(options.Crf.ToString());
			psi.ArgumentList.Add("-vf"); psi.ArgumentList.Add(scaleFilter);
			psi.ArgumentList.Add("-g"); psi.ArgumentList.Add(options.GopSize.ToString());
			psi.ArgumentList.Add("-keyint_min"); psi.ArgumentList.Add("1");
			psi.ArgumentList.Add("-f"); psi.ArgumentList.Add("rtsp");
			psi.ArgumentList.Add("-rtsp_transport"); psi.ArgumentList.Add("tcp");
			psi.ArgumentList.Add(rtspTarget.ToString());

			var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ffmpeg");

			_ = Task.Run(async () =>
			{
				string? line;
				while ((line = await p.StandardError.ReadLineAsync()) != null)
				{
					logger.Warning($"ffmpeg: {line}");
				}
			});

			return p;
		}
	}
}
