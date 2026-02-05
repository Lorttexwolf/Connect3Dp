using PartialBuilderSourceGen;
using TrackedSourceGen;

namespace Lib3Dp.State
{
	public record MachineStateConfiguration(IEnumerable<IReadOnlyMaterialUnit> MaterialUnits, IEnumerable<IMachineNozzle> Nozzles, string? Nickname);

	[GenerateTracked, GeneratePartialBuilder]
	internal partial class MachineState : IReadOnlyMachineState
	{
		public required string ID { get; set; }
		public string? Brand { get; internal set; }
		public string? Model { get; internal set; }
		public string? Nickname { get; internal set; }

		public bool IsConnected { get; internal set; } = false;

		public MachineCapabilities Capabilities { get; internal set; } = MachineCapabilities.None;
		public MachineStatus Status { get; internal set; } = MachineStatus.Unknown;

		public PrintJob? CurrentJob { get; internal set; } = null;
		public HashSet<HistoricPrintJob> JobHistory { get; internal set; } = [];
		public HashSet<LocalPrintJob> LocalJobs { get; internal set; } = [];
		public HashSet<ScheduledPrint> ScheduledPrints { get; internal set; } = [];
		public HashSet<MachineMessage> Messages { get; internal set; } = [];
		public Dictionary<int, MachineExtruder> Extruders { get; internal set; } = [];
		public Dictionary<int, MachineNozzle> Nozzles { get; internal set; } = [];
		public Dictionary<string, MMUnit> MaterialUnits { get; internal set; } = [];
		public MachineAirDuctMode AirDuctMode { get; internal set; }
		public Dictionary<string, int> Fans { get; internal set; } = [];
		public Dictionary<string, bool> Lights { get; internal set; } = [];
		public Dictionary<string, HeatingElement> HeatingElements { get; internal set; } = [];

		public string? StreamingOMEURL { get; internal set; }
		public string? ThumbnailOMEURL { get; internal set; }

		IReadOnlyDictionary<string, bool> IReadOnlyMachineState.Lights => Lights;
		IReadOnlyDictionary<string, int> IReadOnlyMachineState.Fans => Fans;
		IReadOnlyMachinePrintJob? IReadOnlyMachineState.Job => CurrentJob;
		IEnumerable<HistoricPrintJob> IReadOnlyMachineState.JobHistory => JobHistory;
		IEnumerable<MachineMessage> IReadOnlyMachineState.Messages => Messages;
		IEnumerable<IReadOnlyMaterialUnit> IReadOnlyMachineState.MaterialUnits => MaterialUnits.Values;
		IEnumerable<IMachineExtruder> IReadOnlyMachineState.Extruders => Extruders.Values;
		IEnumerable<IMachineNozzle> IReadOnlyMachineState.Nozzles => Nozzles.Values;
		IReadOnlySet<LocalPrintJob> IReadOnlyMachineState.LocalJobs => LocalJobs;
		IEnumerable<ScheduledPrint> IReadOnlyMachineState.ScheduledPrints => ScheduledPrints;
	}
}
