using Connect3Dp.Constants;
using Connect3Dp.Plugins.OME;
using Connect3Dp.SourceGeneration;
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

    [GenerateTracked]
    internal partial class MachineState(string uID, string company, string model, string? nickname) : IReadOnlyMachineState
    {
        public string ID { get; internal set; } = uID;
        public string? Nickname { get; internal set; } = nickname;
        public string Company { get; internal set; } = company;
        public string Model { get; internal set; } = model;

        public bool IsConnected { get; internal set; } = false;

        public MachineCapabilities Capabilities { get; internal set; } = MachineCapabilities.None;
        public MachineStatus Status { get; internal set; } = MachineStatus.Unknown;

        public PrintJob? Job { get; internal set; } = null;
        public HashSet<HistoricPrintJob> JobHistory { get; internal set; } = [];
        public HashSet<LocalPrintJob> LocalJobs { get; internal set; } = [];
        public HashSet<MachineMessage> Messages { get; internal set; } = [];
        public HashSet<MachineNozzle> Nozzles { get; internal set; } = [];
        public HashSet<MaterialUnit> MaterialUnits { get; internal set; } = [];
        public MachineAirDuctMode? AirDuctMode { get; internal set; }
        public Dictionary<string, int> Fans { get; internal set; } = [];
        public Dictionary<string, bool> Lights { get; internal set; } = [];

        public string? StreamingOMEURL { get; internal set; }
        public string? ThumbnailOMEURL { get; internal set; }

        IEnumerable<MachineNozzle> IReadOnlyMachineState.Nozzles => Nozzles;
        IReadOnlyDictionary<string, bool> IReadOnlyMachineState.Lights => Lights;
        IReadOnlyDictionary<string, int> IReadOnlyMachineState.Fans => Fans;
        IReadOnlyMachinePrintJob? IReadOnlyMachineState.Job => Job;
        IEnumerable<HistoricPrintJob> IReadOnlyMachineState.JobHistory => JobHistory;
        IEnumerable<MachineMessage> IReadOnlyMachineState.Messages => Messages;
        IEnumerable<IReadOnlyMaterialUnit> IReadOnlyMachineState.MaterialUnits => MaterialUnits;

        /// <summary>
        /// Applies a partial update to this machine state.
        /// Only properties that were explicitly set in the update will be modified.
        /// </summary>
        internal void AppendUpdate(MachineStateUpdate update)
        {
            // TODO: Make a source generator to automatically generate {class_name}Update classes and this AppendUpdate function.

            if (update.IsConnected.HasValue) IsConnected = update.IsConnected.Value;

            if (update.Status.HasValue) Status = update.Status.Value;

            if (update.CurrentJobUpdate != null)
            {
                if (this.Job != null)
                {
                    Job.ApplyUpdate(update.CurrentJobUpdate);
                }
                else if (update.CurrentJobUpdate.TryConstructBase(out var printJob))
                {
                    Job = printJob;
                }
            }

            if (update.Capabilities.HasValue) Capabilities = update.Capabilities.Value;

            if (update.AirDuctModeSet) AirDuctMode = update.AirDuctMode;

            if (update.Nickname != null) Nickname = update.Nickname;

            if (update.Company != null) Company = update.Company;

            if (update.Model != null) Model = update.Model;

            if (update.StreamingOMEURLSet) StreamingOMEURL = update.StreamingOMEURL;

            if (update.ThumbnailOMEURLSet) ThumbnailOMEURL = update.ThumbnailOMEURL;

            if (update.NozzlesToSet.Count > 0)
            {
                foreach (var item in update.NozzlesToSet)
                {
                    Nozzles.Add(item);
                }
            }

            if (update.FanUpdates != null)
            {
                foreach (var (fanId, speed) in update.FanUpdates)
                {
                    Fans[fanId] = speed;
                }
            }

            if (update.LightUpdates != null)
            {
                foreach (var (lightId, on) in update.LightUpdates)
                {
                    Lights[lightId] = on;
                }
            }

            if (update.ClearMaterialUnits)
            {
                MaterialUnits.Clear();
            }

            if (update.MaterialUnitsToAdd != null)
            {
                foreach (var unit in update.MaterialUnitsToAdd)
                {
                    MaterialUnits.Add(unit);
                }
            }

            if (update.MaterialUnitsToRemove != null)
            {
                foreach (var unit in update.MaterialUnitsToRemove)
                {
                    MaterialUnits.Remove(unit);
                }
            }

            if (update.MaterialUnitUpdates != null)
            {
                foreach (var (unitId, unitUpdate) in update.MaterialUnitUpdates)
                {
                    var unit = MaterialUnits.FirstOrDefault(u => u.ID == unitId);

                    if (unit == null)
                    {
                        // Create new unit if it doesn't exist
                        if (!unitUpdate.Capacity.HasValue)
                        {
                            continue;
                        }

                        unit = new MaterialUnit(unitId, unitUpdate.Capacity.Value);
                        MaterialUnits.Add(unit);
                    }

                    // Apply the update (works for both new and existing units)
                    unit.ApplyUpdate(unitUpdate);
                }
            }

            if (update.ClearMessages)
            {
                Messages.Clear();
            }

            if (update.MessagesToAdd != null)
            {
                foreach (var message in update.MessagesToAdd)
                {
                    Messages.Add(message);
                }
            }

            if (update.MessagesToRemove != null)
            {
                foreach (var message in update.MessagesToRemove)
                {
                    Messages.Remove(message);
                }
            }

            if (update.JobsToAdd != null)
            {
                foreach (var job in update.JobsToAdd)
                {
                    JobHistory.Add(job);
                }
            }

            if (update.JobsToRemove != null)
            {
                foreach (var job in update.JobsToRemove)
                {
                    JobHistory.Remove(job);
                }
            }
        }
    }
}
