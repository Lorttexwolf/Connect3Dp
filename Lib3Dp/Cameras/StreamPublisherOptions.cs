namespace Lib3Dp.Cameras
{
	public record StreamPublisherOptions(
		int? MaxWidth,
		int? MaxHeight,
		int Crf,
		int GopSize,
		string Framerate
	);
}
