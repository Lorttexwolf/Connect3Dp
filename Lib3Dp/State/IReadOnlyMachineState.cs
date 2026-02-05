using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public interface IReadOnlyMachineState : IUniquelyIdentifiable
	{
		string? Brand { get; }
		string? Model { get; }
		string? Nickname { get; }
		bool IsConnected { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineCapabilities Capabilities { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineStatus Status { get; }
		IReadOnlyMachinePrintJob? Job { get; }
		IEnumerable<HistoricPrintJob> JobHistory { get; }
		IReadOnlySet<LocalPrintJob> LocalJobs { get; }
		IEnumerable<ScheduledPrint> ScheduledPrints { get; }
		IEnumerable<IMachineExtruder> Extruders { get; }
		IEnumerable<IMachineNozzle> Nozzles { get; }
		IEnumerable<IReadOnlyMaterialUnit> MaterialUnits { get; }
		IEnumerable<MachineMessage> Messages { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MachineAirDuctMode AirDuctMode { get; }
		IReadOnlyDictionary<string, bool> Lights { get; }
		IReadOnlyDictionary<string, int> Fans { get; }
		string? StreamingOMEURL { get; }
		string? ThumbnailOMEURL { get; }
	}
}
