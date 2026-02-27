using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public interface IMachineState : IUniquelyIdentifiable
	{
		string? Brand { get; }
		string? Model { get; }
		string? Nickname { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineCapabilities Capabilities { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineStatus Status { get; }
		IMachinePrintJob? Job { get; }
		IEnumerable<HistoricPrintJob> JobHistory { get; }
		IReadOnlySet<LocalPrintJob> LocalJobs { get; }
		IEnumerable<ScheduledPrint> ScheduledPrints { get; }
		IEnumerable<MachineExtruder> Extruders { get; }
		IEnumerable<MachineNozzle> Nozzles { get; }
		IEnumerable<IMaterialUnit> MaterialUnits { get; }
		IReadOnlyDictionary<MachineMessage, Notification> Notifications { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineAirDuctMode AirDuctMode { get; }
		IReadOnlyDictionary<string, bool> Lights { get; }
		IReadOnlyDictionary<string, int> Fans { get; }
		string? StreamingOMEURL { get; }
		string? ThumbnailOMEURL { get; }
	}
}
