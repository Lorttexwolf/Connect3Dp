using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State;

[GeneratePartialBuilder]
public class Notification
{
	public DateTimeOffset IssuedAt { get; init; }
	public DateTimeOffset LastSeenAt { get; set; }
}
