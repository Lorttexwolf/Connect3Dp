using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Connect3Dp.Relays.MediaMTX
{
	/// <summary>
	/// Default <see cref="IMediaMTXRelay"/> implementation that talks to MediaMTX's HTTP control API
	/// (default port 9997) documented at https://github.com/bluenviron/mediamtx#api.
	/// </summary>
	public class HttpMediaMTXRelay : IMediaMTXRelay, IDisposable
	{
		private readonly HttpClient Http;
		private readonly MediaMTXRelayOptions Options;
		private readonly ILogger<HttpMediaMTXRelay> Logger;

		public HttpMediaMTXRelay(MediaMTXRelayOptions options, ILogger<HttpMediaMTXRelay> logger)
		{
			Options = options;
			Logger = logger;
			Http = new HttpClient { BaseAddress = options.ApiUrl };
			if (options.AdminUsername is not null || options.AdminPassword is not null)
			{
				var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.AdminUsername}:{options.AdminPassword}"));
				Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
			}
		}

		public async Task AddPullPath(string name, Uri upstream, CancellationToken ct = default)
		{
			using var content = JsonContent.Create(new { source = upstream.ToString() });
			var response = await Http.PostAsync(PathUrl("add", name), content, ct);
			if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				// Already exists — replace it.
				response.Dispose();
				using var patch = JsonContent.Create(new { source = upstream.ToString() });
				response = await Http.PatchAsync(PathUrl("patch", name), patch, ct);
			}
			response.EnsureSuccessStatusCode();
			Logger.LogInformation("MediaMTX path {Name} pulls from {Upstream}", name, upstream);
		}

		public async Task AddPublishPath(string name, CancellationToken ct = default)
		{
			using var content = JsonContent.Create(new { source = "publisher" });
			var response = await Http.PostAsync(PathUrl("add", name), content, ct);
			if (response.StatusCode == HttpStatusCode.BadRequest)
			{
				response.Dispose();
				using var patch = JsonContent.Create(new { source = "publisher" });
				response = await Http.PatchAsync(PathUrl("patch", name), patch, ct);
			}
			response.EnsureSuccessStatusCode();
			Logger.LogInformation("MediaMTX path {Name} awaiting publisher", name);
		}

		public async Task RemovePath(string name, CancellationToken ct = default)
		{
			try
			{
				var response = await Http.PostAsync(PathUrl("delete", name), null, ct);
				if (response.StatusCode == HttpStatusCode.NotFound) return;
				response.EnsureSuccessStatusCode();
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to remove MediaMTX path {Name}", name);
			}
		}

		public Uri GetWebRTCUrl(string name)
		{
			var baseUrl = Options.PublicWebRTCUrl.ToString().TrimEnd('/');
			return new Uri($"{baseUrl}/{name}");
		}

		public Uri GetHlsUrl(string name)
		{
			var baseUrl = Options.PublicHlsUrl.ToString().TrimEnd('/');
			return new Uri($"{baseUrl}/{name}/index.m3u8");
		}

		public Uri GetRtspPublishUrl(string name)
		{
			var builder = new UriBuilder(Options.PublishUrl) { Path = $"/{name}" };
			if (Options.AdminUsername is not null || Options.AdminPassword is not null)
			{
				builder.UserName = Uri.EscapeDataString(Options.AdminUsername ?? string.Empty);
				builder.Password = Uri.EscapeDataString(Options.AdminPassword ?? string.Empty);
			}
			return builder.Uri;
		}

		private static string PathUrl(string verb, string name)
			=> $"/v3/config/paths/{verb}/{Uri.EscapeDataString(name)}";

		public void Dispose() => Http.Dispose();
	}
}
