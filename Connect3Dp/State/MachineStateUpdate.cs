using Connect3Dp.Constants;
using Connect3Dp.State;
using System;
using System.Collections.Generic;

namespace Connect3Dp
{
    /// <summary>
    /// Represents a partial update to machine state.
    /// Only properties that are explicitly set will be applied to the state.
    /// </summary>
    internal class MachineStateUpdate
    {
        internal bool? IsConnected { get; private set; }
        internal MachineStatus? Status { get; private set; }
        internal PrintJobUpdate? CurrentJobUpdate { get; private set; }
        internal MachineCapabilities? Capabilities { get; private set; }
        internal MachineAirDuctMode? AirDuctMode { get; private set; }
        internal bool AirDuctModeSet { get; private set; }
        internal string? Nickname { get; private set; }
        internal string? Company { get; private set; }
        internal string? Model { get; private set; }
        internal string? StreamingOMEURL { get; private set; }
        internal bool StreamingOMEURLSet { get; private set; }
        internal string? ThumbnailOMEURL { get; private set; }
        internal bool ThumbnailOMEURLSet { get; private set; }

        internal Dictionary<string, int>? FanUpdates { get; private set; }
        internal Dictionary<string, bool>? LightUpdates { get; private set; }

        internal HashSet<MaterialUnit>? MaterialUnitsToAdd { get; private set; }
        internal HashSet<MaterialUnit>? MaterialUnitsToRemove { get; private set; }

        internal HashSet<MachineNozzle> NozzlesToSet { get; private set; } = []; // Machines don't dynamically increase their nozzle capacity.

        internal bool ClearMaterialUnits { get; private set; }
        internal Dictionary<string, MaterialUnitUpdate>? MaterialUnitUpdates { get; private set; }

        internal HashSet<MachineMessage>? MessagesToAdd { get; private set; }
        internal HashSet<MachineMessage>? MessagesToRemove { get; private set; }
        internal bool ClearMessages { get; private set; }

        internal HashSet<HistoricPrintJob>? JobsToAdd { get; private set; }
        internal HashSet<HistoricPrintJob>? JobsToRemove { get; private set; }
        internal bool ClearJobHistory { get; private set; }

        public MachineStateUpdate SetConnected(bool connected)
        {
            IsConnected = connected;
            return this;
        }

        public MachineStateUpdate SetStatus(MachineStatus status)
        {
            Status = status;
            return this;
        }

        public MachineStateUpdate UpdateCurrentJob(Action<PrintJobUpdate> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            CurrentJobUpdate ??= new PrintJobUpdate();
            configure(CurrentJobUpdate);
            return this;
        }

        public MachineStateUpdate SetNozzle(MachineNozzle nozzle)
        {
            this.NozzlesToSet.Add(nozzle);
            return this;
        }

        public MachineStateUpdate SetFeatures(MachineCapabilities features)
        {
            Capabilities = features;
            return this;
        }

        public MachineStateUpdate SetAirDuctMode(MachineAirDuctMode? mode)
        {
            AirDuctMode = mode;
            AirDuctModeSet = true;
            return this;
        }

        public MachineStateUpdate SetNickname(string nickname)
        {
            Nickname = nickname ?? throw new ArgumentNullException(nameof(nickname));
            return this;
        }

        public MachineStateUpdate SetCompany(string company)
        {
            Company = company ?? throw new ArgumentNullException(nameof(company));
            return this;
        }

        public MachineStateUpdate SetModel(string model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            return this;
        }

        public MachineStateUpdate SetStreamingOMEURL(string? url)
        {
            StreamingOMEURL = url;
            StreamingOMEURLSet = true;
            return this;
        }

        public MachineStateUpdate SetThumbnailOMEURL(string? url)
        {
            ThumbnailOMEURL = url;
            ThumbnailOMEURLSet = true;
            return this;
        }

        public MachineStateUpdate SetFanSpeed(string fanId, int speed)
        {
            if (string.IsNullOrWhiteSpace(fanId))
                throw new ArgumentException("Fan ID cannot be null or whitespace", nameof(fanId));

            if (speed < 0 || speed > 100)
                throw new ArgumentException("Fan speed must be between 0 and 100", nameof(speed));

            FanUpdates ??= new Dictionary<string, int>();
            FanUpdates[fanId] = speed;
            return this;
        }

        public MachineStateUpdate SetLight(string lightId, bool on)
        {
            if (string.IsNullOrWhiteSpace(lightId))
                throw new ArgumentException("Light ID cannot be null or whitespace", nameof(lightId));

            LightUpdates ??= new Dictionary<string, bool>();
            LightUpdates[lightId] = on;
            return this;
        }

        public MachineStateUpdate AddMaterialUnit(MaterialUnit unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            MaterialUnitsToAdd ??= new HashSet<MaterialUnit>();
            MaterialUnitsToAdd.Add(unit);
            return this;
        }

        public MachineStateUpdate RemoveMaterialUnit(MaterialUnit unit)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            MaterialUnitsToRemove ??= new HashSet<MaterialUnit>();
            MaterialUnitsToRemove.Add(unit);
            return this;
        }

        public MachineStateUpdate UpdateMaterialUnit(string unitId, Action<MaterialUnitUpdate> configure)
        {
            if (string.IsNullOrWhiteSpace(unitId))
                throw new ArgumentException("Unit ID cannot be null or whitespace", nameof(unitId));

            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            MaterialUnitUpdates ??= new Dictionary<string, MaterialUnitUpdate>();

            if (!MaterialUnitUpdates.ContainsKey(unitId))
            {
                MaterialUnitUpdates[unitId] = new MaterialUnitUpdate(unitId);
            }

            configure(MaterialUnitUpdates[unitId]);
            return this;
        }

        public MachineStateUpdate ClearAllMaterialUnits()
        {
            ClearMaterialUnits = true;
            return this;
        }

        public MachineStateUpdate AddMessage(MachineMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            MessagesToAdd ??= new HashSet<MachineMessage>();
            MessagesToAdd.Add(message);
            return this;
        }

        public MachineStateUpdate RemoveMessage(MachineMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            MessagesToRemove ??= new HashSet<MachineMessage>();
            MessagesToRemove.Add(message);
            return this;
        }

        public MachineStateUpdate ClearAllMessages()
        {
            ClearMessages = true;
            return this;
        }

        public MachineStateUpdate AddHistoricJob(HistoricPrintJob printJob)
        {
            JobsToAdd ??= [];
            JobsToAdd.Add(printJob);
            return this;
        }

        public MachineStateUpdate RemoveHistoricJob(HistoricPrintJob printJob)
        {
            JobsToRemove ??= [];
            JobsToRemove.Add(printJob);
            return this;
        }
    }
}