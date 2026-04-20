namespace Connect3Dp.Relays.MediaMTX
{
	/// <summary>
	/// Control-plane abstraction for a MediaMTX container. Registers and removes paths at runtime
	/// via MediaMTX's HTTP control API and produces the URLs consumers use for playback and publishing.
	/// </summary>
	public interface IMediaMTXRelay
	{
		/// <summary>
		/// Adds a path whose source MediaMTX pulls from <paramref name="upstream"/>
		/// (e.g. an RTSPS URL from a Bambu X1, or an MJPEG HTTP URL from an ELEGOO).
		/// </summary>
		Task AddPullPath(string name, Uri upstream, CancellationToken ct = default);

		/// <summary>
		/// Adds a path that MediaMTX expects an external publisher to push into
		/// (via <see cref="GetRtspPublishUrl"/>).
		/// </summary>
		Task AddPublishPath(string name, CancellationToken ct = default);

		/// <summary>
		/// Removes <paramref name="name"/> from MediaMTX. Idempotent — missing paths are ignored.
		/// </summary>
		Task RemovePath(string name, CancellationToken ct = default);

		/// <summary>
		/// The public WebRTC playback URL browsers should use to play this path.
		/// </summary>
		Uri GetWebRTCUrl(string name);

		/// <summary>
		/// The public HLS URL clients should use to play this path (e.g. for expo-video / AVPlayer).
		/// </summary>
		Uri GetHlsUrl(string name);

		/// <summary>
		/// The internal RTSP URL an in-process publisher should push to when the path was
		/// registered via <see cref="AddPublishPath"/>.
		/// </summary>
		Uri GetRtspPublishUrl(string name);
	}
}
