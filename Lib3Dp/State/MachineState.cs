using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{
	public record MachineStateConfiguration(IEnumerable<IMaterialUnit> MaterialUnits, IEnumerable<MachineNozzle> Nozzles, string? Nickname);

	[GeneratePartialBuilder]
	public class MachineState : IMachineState
	{
		public required string ID { get; set; }
		public string? Brand { get; set; }
		public string? Model { get; set; }
		public string? Nickname { get; set; }

		public bool IsConnected { get; set; } = false;

		public MachineCapabilities Capabilities { get; set; } = MachineCapabilities.None;
		public MachineStatus Status { get; set; } = MachineStatus.Unknown;

		public PrintJob? CurrentJob { get; set; } = null;
		public HashSet<HistoricPrintJob> JobHistory { get; set; } = [];
		public HashSet<LocalPrintJob> LocalJobs { get; set; } = [];
		public HashSet<ScheduledPrint> ScheduledPrints { get; set; } = [];
		public HashSet<MachineNotification> Notifications { get; set; } = [];
		public Dictionary<int, MachineExtruder> Extruders { get; set; } = [];
		public Dictionary<int, MachineNozzle> Nozzles { get; set; } = [];
		public Dictionary<string, MUnit> MaterialUnits { get; set; } = [];
		public MachineAirDuctMode AirDuctMode { get; set; }
		public Dictionary<string, int> Fans { get; set; } = [];
		public Dictionary<string, bool> Lights { get; set; } = [];
		public Dictionary<string, HeatingElement> HeatingElements { get; set; } = [];

		public string? StreamingOMEURL { get; set; }
		public string? ThumbnailOMEURL { get; set; }

		IReadOnlyDictionary<string, bool> IMachineState.Lights => Lights;
		IReadOnlyDictionary<string, int> IMachineState.Fans => Fans;
		IMachinePrintJob? IMachineState.Job => CurrentJob;
		IEnumerable<HistoricPrintJob> IMachineState.JobHistory => JobHistory;
		IEnumerable<MachineNotification> IMachineState.Notifications => Notifications;
		IEnumerable<IMaterialUnit> IMachineState.MaterialUnits => MaterialUnits.Values;
		IReadOnlySet<LocalPrintJob> IMachineState.LocalJobs => LocalJobs;
		IEnumerable<ScheduledPrint> IMachineState.ScheduledPrints => ScheduledPrints;
		IEnumerable<MachineExtruder> IMachineState.Extruders => Extruders.Values;
		IEnumerable<MachineNozzle> IMachineState.Nozzles => Nozzles.Values;
	}
}
