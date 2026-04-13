using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public readonly record struct MachineMessage(
		string Id,
		string Title,
		string Body,
		[property: JsonConverter(typeof(JsonStringEnumConverter))] MachineMessageSeverity Severity,
		[property: JsonConverter(typeof(JsonStringEnumConverter))] MachineMessageActions ManualResolve,
		MachineMessageAutoResole AutoResolve)
	{
		public override string ToString()
		{
			return $"{Severity}: {Title} {Body}";
		}
	}

	public enum MachineMessageSeverity
	{
		Info = 0,
		Success = 1,
		Warning = 2,
		Error = 3
	}

	/// <summary>
	/// Actions which can be taken to resolve this message.
	/// </summary>
	[Flags]
	public enum MachineMessageActions
	{
		None = 0,
		Resume = 1,
		Pause = 2,
		Cancel = 4,
		Refresh = 8,
		CheckConfiguration = 16,
		UnsupportedFirmware = 32,
		ClearBed = 64
	}

	public record struct MachineMessageAutoResole(bool? WhenConnected, MachineStatus? WhenStatus, bool? WhenPrinting);
}
