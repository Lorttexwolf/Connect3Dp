using Connect3Dp.Exceptions;
using Connect3Dp.Extensions;
using Connect3Dp.Plugins.OME;
using Connect3Dp.Scheduling;
using Connect3Dp.State;
using Connect3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Connect3Dp.Connectors
{
    public abstract class MachineConnector
    {
        private static readonly Logger Logger = Logger.OfCategory(nameof(MachineConnector));
        private static readonly CronScheduler Scheduler = new(TimeSpan.FromMinutes(1));

        public IReadOnlyMachineState State => _State;
        public event EventHandler<IReadOnlyMachineState>? OnChange;

        private readonly MachineState _State;
        private readonly TrackedMachineState Previous_State;

        protected MachineConnector(string? nickname, string id, string company, string model)
        {
            _State = new MachineState(id, company, model, nickname);
            Previous_State = new TrackedMachineState(_State);
        }

        #region Connect()

        public Task Connect(CancellationToken cancellationToken = default)
        {
            return Do(async () =>
            {
                var connectOp = new RunnableOperation(
                    Connect_Internal,
                    (_) => CompletionStatus.Condition(this.State.IsConnected),
                    timeout: TimeSpan.FromSeconds(15));

                await connectOp.RunAsync();

            }, Constants.MachineMessages.FailedToConnect);
        }

        protected abstract Task Connect_Internal(CancellationToken cancellationToken = default);

        #endregion

        #region Control

        public Task<OperationResult> Stop()
        {
            return Do(() => {

                return new RunnableOperation(
                    Stop_Internal,
                    (_) => CompletionStatus.Condition(this.State.Status is MachineStatus.Stopped or MachineStatus.Failed),
                    timeout: TimeSpan.FromSeconds(15)).RunAsync();

            }, Constants.MachineMessages.FailedToStop);
        }

        protected virtual Task Stop_Internal(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException($"{nameof(Stop_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Material Unit Start Heating

        public async Task<OperationResult> BeginMUHeating(string unitID, HeatingSettings settings)
        {
            var unit = _State.MaterialUnits.FirstOrDefault(x => x.ID.Equals(unitID));

            if (unit == null) return OperationResult.Fail($"Unit of ID {unitID} does not exist!");

            unit.EnsureHasFeature(MaterialUnitFeatures.Heating);

            if (settings.DoSpin.HasValue) unit.EnsureHasFeature(MaterialUnitFeatures.Heating_CanSpin);

            // TODO: Check if the AMS unit is being used. And if not MaterialUnitFeatures.Heating_CanInUse.

            // Heating Constraints

            if (!unit.HeatingConstraints.HasValue || !settings.InRange(unit.HeatingConstraints.Value))
            {
                return OperationResult.Fail($"TempC must be in Range {unit.HeatingConstraints!.Value}");
            }

            var heatOperation = new RunnableOperation(
                execute: (cts) => BeginMUHeating_Internal(unitID, settings),
                isSuccess: (cts) => CompletionStatus.Condition(_State.MaterialUnits.Any((item) => item.ID.Equals(unitID) && item.HeatingJob != null)),
                undo: null,
                timeout: TimeSpan.FromSeconds(30));

            return await heatOperation.RunAsync();
        }

        protected virtual Task BeginMUHeating_Internal(string unitID, HeatingSettings settings)
        {
            throw new NotImplementedException($"{nameof(BeginMUHeating_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Material Unit Stop Heating

        public async Task<OperationResult> EndMUHeating(string unitID)
        {
            var unit = _State.MaterialUnits.FirstOrDefault(x => x.ID.Equals(unitID));

            if (unit == null)
            {
                return OperationResult.Fail($"Unit of ID {unitID} does not exist!");
            }

            if (unit.HeatingJob == null) return OperationResult.Ok(); // Not running.

            unit.EnsureHasFeature(MaterialUnitFeatures.Heating);

            var heatOperation = new RunnableOperation(
                execute: (cts) => EndMaterialUnitHeating_Internal(unitID),
                isSuccess: (cts) => CompletionStatus.Condition(_State.MaterialUnits.Any((item) => item.ID.Equals(unitID) && item.HeatingJob == null)),
                undo: null,
                timeout: TimeSpan.FromSeconds(30));

            return await heatOperation.RunAsync();
        }

        protected virtual Task EndMaterialUnitHeating_Internal(string unitID)
        {
            throw new NotImplementedException($"{nameof(EndMaterialUnitHeating_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Lights

        protected async Task SetLightFixture(string name, bool isOn)
        {
            State.EnsureHasFeature(MachineCapabilities.Lighting);

            var changeOp = new RunnableOperation(
                execute: async (_) => await SetLightFixture_Internal(name, isOn),
                isSuccess: (_) => CompletionStatus.Condition(State.Lights.ContainsKey(name) && State.Lights.GetValueOrDefault(name) == isOn),
                timeout: TimeSpan.FromSeconds(5));

            await changeOp.RunAsync();
        }

        protected virtual Task SetLightFixture_Internal(string name, bool isOn)
        {
            throw new NotImplementedException($"{nameof(SetLightFixture_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Air Duct

        public Task<OperationResult> ChangeAirDuct(MachineAirDuctMode mode)
        {
            State.EnsureHasFeature(MachineCapabilities.AirDuct);

            var changeOp = new RunnableOperation(
                async (_) => await ChangeAirDuctMode_Internal(mode),
                (_) => CompletionStatus.Condition(this.State.AirDuctMode == mode),
                timeout: TimeSpan.FromSeconds(5));

            return changeOp.RunAsync();
        }

        protected virtual Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
        {
            throw new NotImplementedException($"{nameof(ChangeAirDuctMode_Internal)} has not been implemented on the Connector");
        }

        #endregion

        #region Streaming, Oven Media Engine

        /// <summary>
        /// Supplies a PULL URL for an Oven Media Engine Origin.
        /// </summary>
        /// <remarks>
        /// <see cref="MachineCapabilities.OME"/> Feature.
        /// </remarks>
        internal virtual bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
        {
            passURL = null;
            return false;
        }

        #endregion

        #region Internal State Management

        internal bool TryValidateState(MachineStateUpdate updatedState, [NotNullWhen(false)] out string? issue)
        {
            // TODO RULE: Total time on Job cannot be zero minutes!

            // RULE: CurrentJob must only exist while status is Printing, Paused, or Failed.

            if ((State.Status.IsOccupied() || (updatedState.Status.HasValue && updatedState.Status.Value.IsOccupied())) 
                && !(State.Job != null || updatedState.CurrentJobUpdate != null))
            {
                issue = "Job must only exist while status is Printing, Paused, or Failed.";

                return false;
            }

            // RULE: With the MU Heating feature, a Heating Constraint must be applied before advertising this functionality.

            foreach (var mu in State.MaterialUnits)
            {
                var hasHeatingFeature = mu.Features.HasFlag(MaterialUnitFeatures.Heating);
                var hasHeatingConstraints = mu.HeatingConstraints != null;

                // Check if this unit is being updated
                if (updatedState.MaterialUnitUpdates?.TryGetValue(mu.ID, out var unitUpdate) == true)
                {
                    if (unitUpdate.Features.HasValue) hasHeatingFeature = unitUpdate.Features.Value.HasFlag(MaterialUnitFeatures.Heating);

                    if (unitUpdate.HeatingConstraintsSet) hasHeatingConstraints = unitUpdate.HeatingConstraints != null;
                }

                if (hasHeatingFeature && !hasHeatingConstraints)
                {
                    issue = $"Material Unit '{mu.ID}' has Heating feature but no HeatingConstraints configured.";
                    return false;
                }
            }

            if (updatedState.MaterialUnitUpdates != null)
            {
                foreach (var (unitId, unitUpdate) in updatedState.MaterialUnitUpdates)
                {
                    // Skip if this is an existing unit (already checked above)
                    if (State.MaterialUnits.Any(mu => mu.ID == unitId))
                        continue;

                    var hasHeatingFeature = unitUpdate.Features?.HasFlag(MaterialUnitFeatures.Heating) ?? false;
                    var hasHeatingConstraints = unitUpdate.HeatingConstraintsSet && unitUpdate.HeatingConstraints != null;

                    if (hasHeatingFeature && !hasHeatingConstraints)
                    {
                        issue = $"New MaterialUnit '{unitId}' has Heating feature but no HeatingConstraints configured.";
                        return false;
                    }
                }
            }

            issue = null;
            return true;

            //if (updatedState.Status == MachineStatus.Printing)
            //{

            //}
            //else if (updated.Status == MachineStatus.Printed)
            //{
            //    printJob.PercentageComplete = 100;
            //    printJob.RemainingTime = TimeSpan.Zero;
            //    printJob.Stage = null;

            //}
            //else if (data.Status == MachineStatus.Paused)
            //{

            //}
            //else if (data.Status == MachineStatus.Failed)
            //{

            //}
            //else
            //{
            //    return;
            //}

            //if (printJob.RemainingTime == TimeSpan.Zero && printJob.PercentageComplete == 0)
            //{

            //}
        }

        internal void CommitState(MachineStateUpdate updatedState, [CallerMemberName] string callerName = "")
        {
            if (!TryValidateState(updatedState, out var issue))
            {
                Logger.Warning($"Committed state rejected from {callerName}(), reason: {issue}");
                return;
            }

            // Apply Update

            this._State.AppendUpdate(updatedState);

            // Poll Changes

            var isDif = this.Previous_State.HasChanged;

            if (Previous_State.IsConnected.TryUse(markAsSeen: true, out bool isConnected))
            {
                if (isConnected)
                {
                    Logger.Info($"Machine {this.State.Nickname} ({this.State.ID}) Connected!");
                }
                else
                {
                    Logger.Warning($"Machine {this.State.Nickname} ({this.State.ID}) Disconnected!");
                }
            }

            if (Previous_State.Capabilities.TryUse(markAsSeen: true, out var updatedFeatures))
            {
                if (updatedFeatures.HasFlag(MachineCapabilities.OME))
                {
                    // Find which streaming plugin is available, currently, it's only OME.

                    if (AvailablePlugins.OME != null && !AvailablePlugins.OME.IsConnectorRegistered(this) && this.OvenMediaEnginePullURL_Internal(out _))
                    {
                        AvailablePlugins.OME.RegisterConnector(this);
                    }
                }
            }

            // Auto-resolve Messages

            foreach (var message in _State.Messages)
            {
                bool doResolve = false;

                // Auto-resolve if the current machine state matches the desired on-resolve state
                if (message.AutoResolve.WhenPrinting.HasValue && message.AutoResolve.WhenPrinting.Value && _State.Status is MachineStatus.Printing or MachineStatus.Printed)
                {
                    doResolve = true;
                }
                else if (message.AutoResolve.WhenStatus.HasValue &&  _State.Status == message.AutoResolve.WhenStatus.Value)
                {
                    doResolve = true;
                }
                else if (message.AutoResolve.WhenConnected.HasValue && message.AutoResolve.WhenConnected.Value && _State.IsConnected)
                {
                    doResolve = true;
                }

                // Don't use commit state, we are inside it!
                if (doResolve) _State.Messages.Remove(message);
            }

            // Scheduling

            foreach (var (_, MU) in updatedState.MaterialUnitUpdates ?? [])
            {
                if (MU.SchedulesToAdd != null)
                {
                    var schedules = MU.SchedulesToAdd.Where(s => !s.SchedulerID.HasValue || !Scheduler.IsTaskRunning(s.SchedulerID.Value));

                    ScheduleMUHeatingTasks(MU.ID, schedules);
                }

                if (MU.SchedulesToRemove != null)
                {
                    foreach (var schedule in MU.SchedulesToRemove)
                    {
                        if (schedule.SchedulerID.HasValue) Scheduler.End(schedule.SchedulerID.Value);
                    }
                }
            }

            Previous_State.ViewAll();

            // Invoke OnChange

            if (isDif) this.OnChange?.Invoke(this, State);

        }
        internal void CommitState(Action<MachineStateUpdate> updatedStateFunc)
        {
            var update = new MachineStateUpdate();
            updatedStateFunc?.Invoke(update);
            this.CommitState(update);
        }

        private void ScheduleMUHeatingTasks(string muID, params IEnumerable<HeatingSchedule> schedules)
        {
            foreach (var schedule in schedules)
            {
                if (!schedule.SchedulerID.HasValue || !Scheduler.IsTaskRunning(schedule.SchedulerID.Value))
                {
                    schedule.SchedulerID = Scheduler.ScheduleAsync(schedule.Timing, async (meta) =>
                    {
                        var opResult = await this.BeginMUHeating(muID, schedule.Settings);

                        opResult.ThrowIfFailed();

                    }, schedule);
                }
            }
        }

        #endregion

        #region Serialization

        public abstract object GetConfiguration();

        #endregion

        #region Runnable

        private MonoMachine Mono { get; } = new();
        public bool IsMutating => Mono.IsMutating;

        protected Task Do(Func<Task> mutateAction, MachineMessage errorMessage, bool doCommitError = true, [CallerMemberName] string callerName = "")
        {
            try
            {
                return Mono.Mutate(mutateAction, callerName);
            }
            catch (MachineException mEx)
            {
                if (doCommitError) CommitState(changes => changes.AddMessage(errorMessage));
                throw;
            }
            catch (Exception ex)
            {
                var mEx = new MachineException(errorMessage, ex);

                if (doCommitError) CommitState(changes => changes.AddMessage(errorMessage));
                throw mEx;
            }
        }

        protected async Task<T> Do<T>(Func<Task<T>> mutateAction, MachineMessage errorMessage, bool doCommitError = true, [CallerMemberName] string callerName = "")
        {
            try
            {
                return await Mono.Mutate(mutateAction, callerName);
            }
            catch (MachineException mEx)
            {
                if (doCommitError) CommitState(changes => changes.AddMessage(errorMessage));
                throw;
            }
            catch (Exception ex)
            {
                var mEx = new MachineException(errorMessage, ex);
                if (doCommitError) CommitState(changes => changes.AddMessage(errorMessage));
                throw mEx;
            }
        }

        #endregion

        internal static class AvailablePlugins
        {
            public static readonly OMEPlugin? OME;

            static AvailablePlugins()
            {
                if (OMEPlugin.TryGetInstance(out var omeInstance))
                {
                    OME = omeInstance;
                }
            }
        }
    }
}
