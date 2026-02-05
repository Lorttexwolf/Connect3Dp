using Lib3Dp.Exceptions;
using Lib3Dp.Extensions;
using Lib3Dp.Plugins.OME;
using Lib3Dp.Scheduling;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lib3Dp.Connectors
{
	public abstract class MachineConnection
	{
		private readonly Logger Logger;
		private static readonly CronScheduler Scheduler = new(TimeSpan.FromMinutes(1));

		public IReadOnlyMachineState State => _State;
		public event EventHandler<IReadOnlyMachineState>? OnChange;

		private readonly MachineState _State;
		private readonly TrackedMachineState Previous_State;

		protected MachineConnection(string? nickname, string id, string company, string model)
		{
			_State = new MachineState
			{
				ID = id,
				Nickname = nickname,
				Brand = company,
				Model = model
			};

			Logger = Logger.OfCategory($"Machine {this._State.ID}");

			Previous_State = new TrackedMachineState(_State);
		}

		#region Connect

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

		#region Print via Local File

		public Task<OperationResult> PrintLocal(string localPath, PrintOptions options)
		{
			State.EnsureHasFeature(MachineCapabilities.StartLocalJob);

			if (State.Status != MachineStatus.Idle)
			{ 
				return Task.FromResult(OperationResult.Fail($"Cannot start print: Machine is not {nameof(MachineStatus.Idle)}. Current status: {State.Status}"));
			}

			var printLocalOp = new RunnableOperation(
				(ct) => PrintLocal_Internal(localPath, options, ct),
				(_) => CompletionStatus.Condition(this.State.Status is MachineStatus.Printing),
				undo: (_) => this.Stop(),
				timeout: TimeSpan.FromSeconds(30));

			return printLocalOp.RunAsync();
		}

		protected virtual Task PrintLocal_Internal(string localPath, PrintOptions options, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException($"{nameof(PrintLocal_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Clear Bed

		/// <summary>
		/// Clears the build plate, typically by moving the bed forward to allow part removal.
		/// Requires the machine to be in Idle or Printed status.
		/// </summary>
		public Task<OperationResult> ClearBed()
		{
			return Do(() =>
			{
				if (State.Status != MachineStatus.Idle && State.Status != MachineStatus.Printed)
				{
					throw new InvalidOperationException($"Cannot clear bed: Machine must be Idle or Printed. Current status: {State.Status}");
				}

				return new RunnableOperation(
					ClearBed_Internal,
					(_) => CompletionStatus.Condition(this.State.Status is MachineStatus.Idle),
					timeout: TimeSpan.FromSeconds(60)).RunAsync();

			}, Constants.MachineMessages.FailedToClearBed);
		}

		protected virtual Task ClearBed_Internal(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException($"{nameof(ClearBed_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Control, Pause

		public Task<OperationResult> Pause()
		{
			return Do(() =>
			{
				_State.EnsureHasFeature(MachineCapabilities.Control);

				return new RunnableOperation(
					Pause_Internal,
					(_) => CompletionStatus.Condition(this.State.Status is MachineStatus.Paused),
					timeout: TimeSpan.FromSeconds(20)).RunAsync();

			}, Constants.MachineMessages.FailedToPause);
		}

		protected virtual Task Pause_Internal(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException($"{nameof(Pause_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Control, Resume

		public Task<OperationResult> Resume()
		{
			return Do(() =>
			{
				_State.EnsureHasFeature(MachineCapabilities.Control);

				return new RunnableOperation(
					Resume_Internal,
					(_) => CompletionStatus.Condition(this.State.Status is not MachineStatus.Paused),
					timeout: TimeSpan.FromSeconds(20)).RunAsync();

			}, Constants.MachineMessages.FailedToResume);
		}

		protected virtual Task Resume_Internal(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException($"{nameof(Resume_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Control, Stop

		public Task<OperationResult> Stop()
		{

			return Do(() =>
			{
				_State.EnsureHasFeature(MachineCapabilities.Control);

				return new RunnableOperation(
					Stop_Internal,
					(_) => CompletionStatus.Condition(this.State.Status is MachineStatus.Canceled),
					timeout: TimeSpan.FromSeconds(15)).RunAsync();

			}, Constants.MachineMessages.FailedToStop);
		}

		protected virtual Task Stop_Internal(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException($"{nameof(Stop_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region OME Streaming

		internal virtual bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
		{
			passURL = null;
			return false;
		}

		#endregion

		#region Air Duct Mode

		public Task<OperationResult> ChangeAirDuct(MachineAirDuctMode mode)
		{
			State.EnsureHasFeature(MachineCapabilities.AirDuct);

			var changeOp = new RunnableOperation(
				async (_) => await ChangeAirDuctMode_Internal(mode),
				(_) => CompletionStatus.Condition(this.State.AirDuctMode == mode),	
				timeout: TimeSpan.FromSeconds(10));

			return changeOp.RunAsync();
		}

		protected virtual Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
		{
			throw new NotImplementedException($"{nameof(ChangeAirDuctMode_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Material Unit Start Heating

		public async Task<OperationResult> BeginMUHeating(string unitID, HeatingSettings settings)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) return OperationResult.Fail($"Unit of ID {unitID} does not exist!");

			unit.EnsureHasFeature(MaterialUnitCapabilities.Heating);

			if (settings.DoSpin.HasValue) unit.EnsureHasFeature(MaterialUnitCapabilities.Heating_CanSpin);

			// TODO: Check if the AMS unit is being used. And if not MaterialUnitFeatures.Heating_CanInUse.

			// Heating Constraints

			if (!unit.HeatingConstraints.HasValue || !settings.IsInRange(unit.HeatingConstraints.Value))
			{
				return OperationResult.Fail($"TempC must be in Range {unit.HeatingConstraints!.Value}");
			}

			var heatOperation = new RunnableOperation(
				execute: (cts) => BeginMUHeating_Internal(unitID, settings),
				isSuccess: (cts) => CompletionStatus.Condition(_State.MaterialUnits.Values.Any((item) => item.ID.Equals(unitID) && item.HeatingJob != null)),
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
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null)
			{
				return OperationResult.Fail($"Unit of ID {unitID} does not exist!");
			}

			if (unit.HeatingJob == null) return OperationResult.Ok(); // Not running.

			unit.EnsureHasFeature(MaterialUnitCapabilities.Heating);

			var heatOperation = new RunnableOperation(
				execute: (cts) => EndMaterialUnitHeating_Internal(unitID),
				isSuccess: (cts) => CompletionStatus.Condition(_State.MaterialUnits.Values.Any((item) => item.ID.Equals(unitID) && item.HeatingJob == null)),
				undo: null,
				timeout: TimeSpan.FromSeconds(30));

			return await heatOperation.RunAsync();
		}

		protected virtual Task EndMaterialUnitHeating_Internal(string unitID)
		{
			throw new NotImplementedException($"{nameof(EndMaterialUnitHeating_Internal)} has not been implemented on the Connector");
		}

		#endregion

		#region Scheduled Tasks

		/// <summary>
		/// Schedules a print job to be executed based on a CRON expression.
		/// </summary>
		/// <param name="scheduledPrint">The scheduled print configuration.</param>
		public void SchedulePrint(ScheduledPrint scheduledPrint)
		{
			_State.EnsureHasFeature(MachineCapabilities.StartLocalJob);

			CommitState(changes => changes.SetScheduledPrints(scheduledPrint));
		}

		/// <summary>
		/// Cancels a scheduled print job.
		/// </summary>
		/// <param name="scheduledPrint">The scheduled print to cancel.</param>
		public void CancelScheduledPrint(ScheduledPrint scheduledPrint)
		{
			CommitState(changes => changes.RemoveScheduledPrints(scheduledPrint));
		}

		private void DoScheduleMUHeating(string MMID, params IEnumerable<HeatingSchedule> schedules)
		{
			foreach (var schedule in schedules)
			{
				if (!schedule.SchedulerID.HasValue || !Scheduler.IsTaskRunning(schedule.SchedulerID.Value))
				{
					schedule.SchedulerID = Scheduler.ScheduleAsync(schedule.Timing, async (meta) =>
					{
						var opResult = await this.BeginMUHeating(MMID, schedule.Settings);

						opResult.ThrowIfFailed();

					}, schedule);
				}
			}
		}

		private void DoSchedulePrint(IEnumerable<ScheduledPrint> scheduledPrints)
		{
			foreach (var scheduledPrint in scheduledPrints)
			{
				if (!scheduledPrint.SchedulerID.HasValue || !Scheduler.IsTaskRunning(scheduledPrint.SchedulerID.Value))
				{
					scheduledPrint.SchedulerID = Scheduler.ScheduleAsync(scheduledPrint.Timing, async (meta) =>
					{
						var jobName = meta.LocalJob.Name;

						if (!_State.LocalJobs.Any(lj => lj.Path.Equals(meta.LocalJob.Path, StringComparison.OrdinalIgnoreCase)))
						{
							CommitState(changes => changes
								.SetMessages(Constants.MachineMessages.ScheduledPrintSkipped(jobName, "Local file no longer exists on the machine"))
								.RemoveScheduledPrints(meta));

							return;
						}

						if (!_State.IsConnected || _State.Status is not MachineStatus.Idle)
						{
							var reason = !_State.IsConnected ? "Machine is not connected" : "Machine is not idle";

							CommitState(changes => changes.SetMessages(Constants.MachineMessages.ScheduledPrintSkipped(jobName, reason)));

							return;
						}

						var originalOptions = meta.Options;
						var required = meta.LocalJob.MaterialsToPrint;
						var resolved = new Dictionary<int, SpoolLocation>(originalOptions.MaterialMap ?? []);

						static SpoolLocation? pickCandidate(Dictionary<int, List<SpoolLocation>> source, int pid, HashSet<SpoolLocation> used)
						{
							if (!source.TryGetValue(pid, out var list) || list.Count == 0) return null;
							foreach (var l in list)
							{
								if (used.Add(l)) return l;
							}
							return null;
						}

						var matchesAll = _State.FindMatchingSpools(required);
						var usedLocations = new HashSet<SpoolLocation>();

						// If user provided no mapping, auto-map greedily preferring color matches

						if (resolved.Count == 0)
						{
							foreach (var pid in required.Keys)
							{
								var pick = pickCandidate(matchesAll.MaterialMatchesAndSimilarColor, pid, usedLocations) ?? pickCandidate(matchesAll.MaterialOnlyMatches, pid, usedLocations);

								if (pick == null)
								{
									CommitState(changes => changes.SetMessages(Constants.MachineMessages.ScheduledPrintSkipped(jobName, $"No Available Spool for Filament id {pid}")));
									return;
								}
								resolved[pid] = pick.Value;
							}
						}
						else
						{
							// Validate/replace provided mapping

							foreach (var pid in required.Keys)
							{
								if (resolved.TryGetValue(pid, out var loc) && _State.MaterialUnits.TryGetValue(loc.MMID, out var mu) && mu.Trays.TryGetValue(loc.Slot, out var tray) && tray.Material.Equals(required[pid].Material))
								{
									// Accepted

									usedLocations.Add(loc);
									continue;
								}

								// Try to find replacement

								var pick = pickCandidate(matchesAll.MaterialMatchesAndSimilarColor, pid, usedLocations) ?? pickCandidate(matchesAll.MaterialOnlyMatches, pid, usedLocations);

								if (pick == null)
								{
									CommitState(changes => changes.SetMessages(Constants.MachineMessages.ScheduledPrintSkipped(jobName, $"No Matching Spool for Filament #{pid}")));

									return;
								}
								resolved[pid] = pick.Value;
							}
						}

						var execOptions = new PrintOptions(originalOptions.LevelBed, originalOptions.FlowCalibration, originalOptions.VibrationCalibration, originalOptions.InspectFirstLayer, resolved);

						try
						{
							var opResult = await this.PrintLocal(meta.LocalJob.Path, execOptions);

							if (!opResult.Success) CommitState(changes => changes.SetMessages(Constants.MachineMessages.ScheduledPrintFailed(jobName, opResult.Message ?? "Unknown Error")));
						}
						catch (Exception ex)
						{
							Logger.Error(ex, $"Scheduled Print '{jobName}' Failed");
							CommitState(changes => changes.SetMessages(Constants.MachineMessages.ScheduledPrintFailed(jobName, ex.Message)));
						}

					}, scheduledPrint);
				}
			}
		}

		#endregion

		#region Scheduled Material Unit Heating

		/// <summary>
		/// Schedules a material unit heating/drying job to be executed based on a CRON expression.
		/// </summary>
		public void ScheduleMUHeating(string unitID, HeatingSchedule schedule)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) throw new ArgumentException($"Material Unit of ID {unitID} does not exist!", nameof(unitID));

			unit.EnsureHasFeature(MaterialUnitCapabilities.Heating);

			CommitState(changes => changes.UpdateMaterialUnits(unitID, mu => mu.SetHeatingSchedule(schedule)));
		}

		/// <summary>
		/// Cancels a scheduled material unit heating/drying job.
		/// </summary>
		public void CancelScheduledMUHeating(string unitID, HeatingSchedule schedule)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) throw new ArgumentException($"Material Unit of ID {unitID} does not exist!", nameof(unitID));

			CommitState(changes => changes.UpdateMaterialUnits(unitID, mu => mu.RemoveHeatingSchedule(schedule)));
		}

		/// <summary>
		/// Cancels all scheduled heating jobs for a material unit.
		/// </summary>
		public void CancelAllScheduledMUHeating(string unitID)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) throw new ArgumentException($"Material Unit of ID {unitID} does not exist!", nameof(unitID));

			var schedulesToRemove = unit.HeatingSchedule.ToList();

			foreach (var schedule in schedulesToRemove)
			{
				if (schedule.SchedulerID.HasValue)
				{
					Scheduler.End(schedule.SchedulerID.Value);
				}
			}

			CommitState(changes =>
			{
				foreach (var schedule in schedulesToRemove)
				{
					changes.UpdateMaterialUnits(unitID, mu => mu.RemoveHeatingSchedule(schedule));
				}
			});
		}

		#endregion

		#region Messages

		/// <summary>
		/// Dismisses a specific message from the machine's message list.
		/// </summary>
		public void DismissMessage(MachineMessage message)
		{
			_State.Messages.Remove(message);
			this.OnChange?.Invoke(this, State);
		}

		/// <summary>
		/// Dismisses a message by its ID.
		/// </summary>
		public bool DismissMessage(string messageId)
		{
			var message = _State.Messages.FirstOrDefault(m => m.ID == messageId);
			if (message == null) return false;

			_State.Messages.Remove(message);
			this.OnChange?.Invoke(this, State);
			return true;
		}

		/// <summary>
		/// Dismisses all messages from the machine's message list.
		/// </summary>
		public void DismissAllMessages()
		{
			_State.Messages.Clear();
			this.OnChange?.Invoke(this, State);
		}

		/// <summary>
		/// Dismisses all messages with the specified severity.
		/// </summary>
		public int DismissMessages(MachineMessageSeverity severity)
		{
			var toRemove = _State.Messages.Where(m => m.Severity == severity);

			foreach (var message in toRemove)
			{
				_State.Messages.Remove(message);
			}

			if (toRemove.Count() > 0)
			{
				this.OnChange?.Invoke(this, State);
			}

			return toRemove.Count();
		}

		#endregion

		#region Internal State Management

		internal bool TryValidateState(MachineStateUpdate updatedState, [NotNullWhen(false)] out string? issue)
		{
			// TODO RULE: Total time on Job cannot be zero minutes!

			// TODO RULE: CurrentJob must only exist while status is Printing, Paused, or Failed.

			//if ((State.Status != MachineStatus.Idle || (updatedState.Status.HasValue && updatedState.Status.Value != MachineStatus.Idle)) && !(State.Job != null || updatedState.CurrentJob != null))
			//{
			//	issue = "Job may only exist while status is Printed, Printing, Paused, or Failed.";

			//	return false;
			//}

			// RULE: With the MU Heating feature, a Heating Constraint must be applied before advertising this functionality.

			if (updatedState.MaterialUnitsToSet != null)
			{
				foreach (var mu in State.MaterialUnits)
				{
					var hasHeatingFeature = mu.Capabilities.HasFlag(MaterialUnitCapabilities.Heating);
					var hasHeatingConstraints = mu.HeatingConstraints != null;

					// Check if this unit is being updated
					if (updatedState.MaterialUnitsToSet.TryGetValue(mu.ID, out var unitUpdate) == true)
					{
						if (unitUpdate.Capabilities.HasValue) hasHeatingFeature = unitUpdate.Capabilities.Value.HasFlag(MaterialUnitCapabilities.Heating);

						if (unitUpdate.HeatingConstraintsIsSet) hasHeatingConstraints = unitUpdate.HeatingConstraints != null;
					}

					if (hasHeatingFeature && !hasHeatingConstraints)
					{
						issue = $"Material Unit '{mu.ID}' has Heating feature but no HeatingConstraints configured.";
						return false;
					}
				}

				foreach (var (unitId, unitUpdate) in updatedState.MaterialUnitsToSet)
				{
					// Skip if this is an existing unit (already checked above)
					if (State.MaterialUnits.Any(mu => mu.ID == unitId))
						continue;

					var hasHeatingFeature = unitUpdate.Capabilities?.HasFlag(MaterialUnitCapabilities.Heating) ?? false;
					var hasHeatingConstraints = unitUpdate.HeatingConstraintsIsSet && unitUpdate.HeatingConstraints != null;

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
			// Filters

			//if (updatedState.Status is MachineStatus.Printed or MachineStatus.Canceled && this._State.CurrentJob?.MaterialUsages != null && this._State.CurrentJob.MaterialUsages.Count != 0)
			//{
			//	// We need to use different logic depending on if the print was completed, or partial completed (canceled). We don't know exactly how much filament is used.

			//	// The issue is that we don't know exactly how much material is used on each layer. Therefore, we cannot exactly calculate how much is used, especially when a print is canceled, and is multi-color.

			//	Dictionary<MaterialLocation, int> actualAmountUsed;

			//	if (updatedState.Status is MachineStatus.Printed)
			//	{
			//		actualAmountUsed = new Dictionary<MaterialLocation, int>(this._State.CurrentJob.MaterialUsages);
			//	}
			//	else
			//	{
			//		if (this._State.CurrentJob.MaterialUsages.Count > 1)
			//		{
			//			// Multi, it's over.
			//		}
			//		else
			//		{
						
			//		}

			//			// Failed, cringe.

			//			actualAmountUsed = new Dictionary<MaterialLocation, int>(this._State.CurrentJob.MaterialUsages.ToDictionary());

			//		//if ()
			//	}

			//	foreach (var (location, usageInGrams) in actualAmountUsed)
			//	{
			//		if (!this._State.MaterialUnits.TryGetValue(location.MMID, out var blehMU) || !blehMU.Trays.TryGetValue(location.Slot, out var blehSpot) || blehSpot.GramsRemaining == null)
			//		{
			//			Logger.Warning("Couldn't update remaining filament after printing concluded: Original amount is missing");
			//			continue;
			//		}

			//		updatedState.UpdateMaterialUnits(location.MMID, mu => mu.UpdateTrays(location.Slot, tray => tray.SetGramsRemaining(blehSpot.GramsRemaining - usageInGrams)));
			//	}
			//}

			// Validate

			if (!TryValidateState(updatedState, out var issue))
			{
				Logger.Warning($"Committed state rejected from {callerName}(), reason: {issue}");
				return;
			}

			// Check if any removed local jobs have associated scheduled prints

			if (updatedState.LocalJobsToRemove != null)
			{
				foreach (var removedJob in updatedState.LocalJobsToRemove)
				{
					var orphanedScheduledPrints = _State.ScheduledPrints
						.Where(sp => sp.LocalJob.Path.Equals(removedJob.Path, StringComparison.OrdinalIgnoreCase))
						.ToList();

					foreach (var orphanedPrint in orphanedScheduledPrints)
					{
						if (orphanedPrint.SchedulerID.HasValue)
						{
							Scheduler.End(orphanedPrint.SchedulerID.Value);
						}

						updatedState.RemoveScheduledPrints(orphanedPrint);

						Logger.Info($"Removed scheduled print '{orphanedPrint.LocalJob.Name}' because its local file was deleted!");
					}
				}
			}

			// Apply Update	

			updatedState.AppendUpdate(this._State);

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
				else if (message.AutoResolve.WhenStatus.HasValue && _State.Status == message.AutoResolve.WhenStatus.Value)
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

			if (updatedState.MaterialUnitsToSet != null)
			{
				foreach (var MU in updatedState.MaterialUnitsToSet.Values)
				{
					if (MU.HeatingScheduleToSet != null)
					{
						var schedules = MU.HeatingScheduleToSet.Where(s => !s.SchedulerID.HasValue || !Scheduler.IsTaskRunning(s.SchedulerID.Value));

						DoScheduleMUHeating(MU.ID, schedules);
					}

					if (MU.HeatingScheduleToRemove != null)
					{
						foreach (var schedule in MU.HeatingScheduleToRemove)
						{
							if (schedule.SchedulerID.HasValue) Scheduler.End(schedule.SchedulerID.Value);
						}
					}
				}

				// TODO: MU for removal.
			}

			// Scheduled Prints

			if (updatedState.ScheduledPrintsToSet != null)
			{
				var schedulesToStart = updatedState.ScheduledPrintsToSet
					.Where(s => !s.SchedulerID.HasValue || !Scheduler.IsTaskRunning(s.SchedulerID.Value));

				DoSchedulePrint(schedulesToStart);
			}

			if (updatedState.ScheduledPrintsToRemove != null)
			{
				foreach (var scheduledPrint in updatedState.ScheduledPrintsToRemove)
				{
					if (scheduledPrint.SchedulerID.HasValue)
					{
						Scheduler.End(scheduledPrint.SchedulerID.Value);
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
				if (doCommitError) CommitState(changes => changes.SetMessages(errorMessage));
				throw;
			}
			catch (Exception ex)
			{
				var mEx = new MachineException(errorMessage, ex);

				if (doCommitError) CommitState(changes => changes.SetMessages(errorMessage));
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
				if (doCommitError) CommitState(changes => changes.SetMessages(errorMessage));
				throw;
			}
			catch (Exception ex)
			{
				var mEx = new MachineException(errorMessage, ex);
				if (doCommitError) CommitState(changes => changes.SetMessages(errorMessage));
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
