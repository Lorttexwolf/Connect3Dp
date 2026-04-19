namespace Lib3Dp.Cameras
{
	/// <summary>
	/// Describes how a machine's camera feed should be acquired by the MediaMTX relay.
	/// </summary>
	public abstract record CameraSource
	{
		public sealed record NoCamera() : CameraSource;

		public sealed record PullCameraSource(Uri Upstream, CameraSpec Spec) : CameraSource;

		/// <summary>
		/// The <see cref="Connectors.MachineConnection"/> will publish to MediaMTX itself by implementing <see cref="Connectors.MachineConnection.RunRTSPCameraPublisher"/>.
		/// </summary>
		public sealed record PublisherCameraSource(StreamPublisherOptions Full, CameraSpec FullSpec, StreamPublisherOptions? Glance, CameraSpec? GlanceSpec) : CameraSource;
	}
}
