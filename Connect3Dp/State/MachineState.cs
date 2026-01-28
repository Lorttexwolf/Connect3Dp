using Connect3Dp.Constants;
using Connect3Dp.Plugins.OME;
using Connect3Dp.SourceGeneration;
using Connect3Dp.SourceGeneration.UpdateGen;
using Connect3Dp.Tracked;
using Connect3Dp.Utilities;
using PartialSourceGen;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Connect3Dp.State
{
    public record MachineStateConfiguration(IEnumerable<IReadOnlyMaterialUnit> MaterialUnits, IEnumerable<MachineNozzle> Nozzles, string? Nickname);

    [GenerateTracked, GenerateUpdate]
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
        public HashSet<MachineMessage> Messages { get; internal set; } = [];
        public Dictionary<int, MachineNozzle> Nozzles { get; internal set; } = [];
        public Dictionary<string, MaterialUnit> MaterialUnits { get; internal set; } = [];
        public MachineAirDuctMode AirDuctMode { get; internal set; }
        public Dictionary<string, int> Fans { get; internal set; } = [];
        public Dictionary<string, bool> Lights { get; internal set; } = [];

        public string? StreamingOMEURL { get; internal set; }
        public string? ThumbnailOMEURL { get; internal set; }

        IEnumerable<MachineNozzle> IReadOnlyMachineState.Nozzles => Nozzles.Values;
        IReadOnlyDictionary<string, bool> IReadOnlyMachineState.Lights => Lights;
        IReadOnlyDictionary<string, int> IReadOnlyMachineState.Fans => Fans;
        IReadOnlyMachinePrintJob? IReadOnlyMachineState.Job => CurrentJob;
        IEnumerable<HistoricPrintJob> IReadOnlyMachineState.JobHistory => JobHistory;
        IEnumerable<MachineMessage> IReadOnlyMachineState.Messages => Messages;
        IEnumerable<IReadOnlyMaterialUnit> IReadOnlyMachineState.MaterialUnits => MaterialUnits.Values;
    }
}
