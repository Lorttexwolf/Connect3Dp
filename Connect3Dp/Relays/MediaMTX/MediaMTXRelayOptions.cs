namespace Connect3Dp.Relays.MediaMTX
{
	public record MediaMTXRelayOptions(
		Uri ApiUrl,
		Uri PublishUrl,
		Uri PublicWebRTCUrl,
		Uri PublicHlsUrl,
		string? AdminUsername = null,
		string? AdminPassword = null,
		string? ViewerUsername = null,
		string? ViewerPassword = null
	);
}
