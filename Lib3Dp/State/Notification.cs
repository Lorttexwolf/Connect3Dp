using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State;

[GeneratePartialBuilder]
public class Notification
{
	public MachineMessage Message { get; set; }
	public DateTimeOffset IssuedAt { get; init; }
	public DateTimeOffset LastSeenAt { get; set; }
}
