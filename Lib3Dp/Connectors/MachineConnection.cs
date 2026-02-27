using Lib3Dp.Configuration;
using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.Extensions;
using Lib3Dp.Files;
using Lib3Dp.Plugins.OME;
using Lib3Dp.Scheduling;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Lib3Dp.MonoMachine;

namespace Lib3Dp.Connectors
{
	/// <summary>
	/// Base configuration of a <see cref="MachineConnection"/> required to instantiate a connection.
	/// </summary>
	public record MachineConnectionConfiguration(
		string? Nickname,
		string ID,
		string Brand,
		string Model
	);

	public abstract partial class MachineConnection
	{
		private static readonly CronScheduler Scheduler;

		private readonly Logger Logger;

		private readonly MachineState _State;

		public readonly MonoMachine Mono;

		protected readonly IMachineFileStore FileStore;

		/// <summary>
		/// The unique identifier for this machine connection.
		/// Only assigned during construction or a configuration load/update via
		/// <see cref="MachineIDWithConfigurationWithDiscrimination"/>.
		/// </summary>
		public string ID { get; private set; }

		public event Action<MachineConnection, MachineStateChanges>? OnChanges;

		public IMachineState State => _State;

		static MachineConnection()
		{
			Scheduler = new(TimeSpan.FromMinutes(1));
		}

		protected MachineConnection(IMachineFileStore fileStore, MachineConnectionConfiguration configuration)
		{
			FileStore = fileStore;
			Mono = new MonoMachine();
			ID = configuration.ID;

			_State = new MachineState
			{
				Nickname = configuration.Nickname,
				Brand = configuration.Brand,
				Model = configuration.Model
			};

			Logger = Logger.OfCategory($"Machine {ID}");
		}

		public void AddNotification(MachineMessage message)
		{
			this.CommitState(new MachineStateUpdate().SetNotifications(message));
		}

		public async Task<Stream> DownloadFile(MachineFileHandle fileHandle)
		{
			if (this.FileStore.Contains(fileHandle))
			{
				try
				{
					var fromStore = await this.FileStore.Read(fileHandle);

					return fromStore;
				}
				catch (IOException)
				{
					// Attempt to download from local before throwing.
				}
			}

			try
			{
				Stream storeAt;
				bool isTempStream = false;

				try
				{
					storeAt = await this.FileStore.Stream(fileHandle);
				}
				catch (Exception storeStreamEx)
				{
					Logger.Error(storeStreamEx, $"Failed to StoreStream() {fileHandle} into FileStore");

					storeAt = FileUtils.CreateTempFileStream();
					isTempStream = true;
				}

				await this.DownloadLocalFile(fileHandle, storeAt);

				// Add to file store.

				if (isTempStream)
				{
					try
					{
						await this.FileStore.Store(fileHandle, storeAt);
					}
					catch (IOException storeEx)
					{
						Logger.Error(storeEx, $"Failed to write {fileHandle} into FileStore");
					}
				}

				return storeAt;
			}
			catch (IOException)
			{
				throw;
			}
		}

		/// <summary>
		/// Returns a readable stream of the downloaded <paramref name="fileHandle"/> or <see cref="IOException"/> if not found.
		/// </summary>
		protected abstract Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream);

		internal void PreprocessState(MachineStateUpdate updatedState, out IEnumerable<string> issues)
		{
			var foundIssues = new List<string>();
			issues = foundIssues;

			// TODO RULE: Total time on Job cannot be zero minutes!

			// TODO RULE: CurrentJob must only exist while status is Printing, Paused, or Failed.

			// RULE: With the MU Heating feature, a Heating Constraint must be applied before advertising this functionality.

			// Note: Notification merging (preserving IssuedAt for existing notifications while updating
			// LastSeenAt) is handled automatically by the source-generated AppendUpdate — it calls
			// AppendUpdate on existing dict entries instead of replacing them, so init-only fields
			// like IssuedAt are preserved and only explicitly-set fields (LastSeenAt) are updated.

			// Auto-resolve Messages

			foreach (var (message, _) in _State.Notifications)
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
				else if (message.AutoResolve.WhenConnected.HasValue && message.AutoResolve.WhenConnected.Value && _State.Status is not MachineStatus.Disconnected)
				{
					doResolve = true;
				}

				if (doResolve) updatedState.RemoveNotifications(message);
			}

			// Check if any removed local jobs have associated scheduled prints

			if (updatedState.LocalJobsToRemove != null)
			{
				foreach (var removedJob in updatedState.LocalJobsToRemove)
				{
					var orphanedScheduledPrints = _State.ScheduledPrints
						.Where(sp => sp.LocalJob.File.Equals(removedJob.File))
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
		}

		protected void CommitState(MachineStateUpdate updatedState, [CallerMemberName] string callerName = "")
		{
			// Preprocess / Validate

			PreprocessState(updatedState, out var issuesFound);

			if (issuesFound.Any())
			{
				Logger.Trace($"{callerName}() produced state issues, which were ignored: {string.Join(", ", issuesFound)}");
			}

			// Apply Update	

			updatedState.AppendUpdate(this._State, out var changes);

			// Append to Print History

			var justEnded = changes.StatusHasChanged && State.Status is MachineStatus.Printed or MachineStatus.Canceled && changes.StatusPrevious is MachineStatus.Printing;

			if (justEnded && State.Job is not null)
			{
				updatedState.SetJobHistory(new HistoricPrintJob(
					State.Job.Name, 
					State.Status is MachineStatus.Printed, 
					DateTime.Now, 
					State.Job.TotalTime - State.Job.RemainingTime,
					State.Job.Thumbnail,
					State.Job.File));
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

			// Invoke OnChange

			if (changes.HasChanged) this.OnChanges?.Invoke(this, changes);

		}

		protected void CommitState(Action<MachineStateUpdate> updatedStateFunc)
		{
			var update = new MachineStateUpdate();
			updatedStateFunc?.Invoke(update);
			this.CommitState(update);
		}

		public abstract object GetConfiguration();

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

	#region Connect and Disconnect
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> Connect(CancellationToken cancellationToken = default)
		{
			MutationValuedResult<MachineOperationResult> mutateResult = await this.Mono.MutateUntil(
				Connect_Internal,
				() => this.State.Status is not MachineStatus.Disconnected,
				TimeSpan.FromSeconds(15));

			return mutateResult.IntoOperationResult(MachineMessages.FailedToConnect.Title, autoResolve: MachineMessages.FailedToConnect.AutoResolve);
		}

		protected abstract Task<MachineOperationResult> Connect_Internal();

		public async Task<MachineOperationResult> ConnectIfDisconnected(CancellationToken cancellationToken = default)
		{
			if (this.State.Status is not MachineStatus.Disconnected) return MachineOperationResult.Ok;

			return await this.Connect(cancellationToken);
		}

		public abstract Task Disconnect();
	}
	#endregion

	#region Print Local File
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> PrintLocal(LocalPrintJob localPrint, PrintOptions options)
		{
			if (State.OpIfNotCapable(MachineCapabilities.StartLocalJob, out var uncapableResult)) return uncapableResult.Value;

			if (State.Status != MachineStatus.Idle)
			{
				return MachineOperationResult.Fail(Constants.MachineMessages.FailedToStartLocalPrint.Title, $"Cannot start print: Machine is not {nameof(MachineStatus.Idle)}. Current status: {State.Status}");
			}

			var mutateResult = await this.Mono.MutateUntil(
				() => PrintLocal_Internal(localPrint, options),
				() => this.State.Status is MachineStatus.Printing,
				TimeSpan.FromSeconds(60));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToStartLocalPrint.Title, autoResolve: Constants.MachineMessages.FailedToStartLocalPrint.AutoResolve);
		}

		protected virtual Task PrintLocal_Internal(LocalPrintJob localPrint, PrintOptions options)
		{
			throw new NotImplementedException($"{nameof(PrintLocal_Internal)} has not been implemented on the Connector");
		}
	}
	#endregion

	#region Modifying Filament
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> ChangeMaterial(SpoolLocation location, Material material)
		{
			if (!this._State.MaterialUnits.TryGetValue(location.MUID, out var mu))
			{
				return MachineOperationResult.Fail(MachineMessages.MUDoesNotExist(location.MUID));
			}

			if (mu.IfNotCapable(MUCapabilities.ModifyTray, out var unableToModifyTrayOpRes)) return unableToModifyTrayOpRes.Value;

			var changeMaterialOp = await Mono.MutateUntil(
				() => Invoke_ChangeMaterial(location, material),
				() => this._State.MaterialUnits.GetValueOrDefault(location.MUID)?.Trays.GetValueOrDefault(location.Slot).Material == material,
				TimeSpan.FromSeconds(15));

			return changeMaterialOp.IntoOperationResult("Change Material");
		}

		protected virtual Task<MachineOperationResult> Invoke_ChangeMaterial(SpoolLocation location, Material material)
		{
			throw new NotImplementedException($"{nameof(Invoke_ChangeMaterial)} has not been implemented on the Connector");
		}
	}
	#endregion

	#region Pause, Resume, Stop
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> Pause()
		{
			if (State.OpIfNotCapable(MachineCapabilities.Control, out var uncapableResult)) return uncapableResult.Value;

			var mutateResult = await this.Mono.MutateUntil(
				() => Pause_Internal(),
				() => this.State.Status is MachineStatus.Paused,
				TimeSpan.FromSeconds(20));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToPause.Title, autoResolve: Constants.MachineMessages.FailedToPause.AutoResolve);
		}
		protected virtual Task Pause_Internal()
		{
			throw new NotImplementedException($"{nameof(Pause_Internal)} has not been implemented on the Connector");
		}

		public async Task<MachineOperationResult> Resume()
		{
			if (State.OpIfNotCapable(MachineCapabilities.Control, out var uncapableResult)) return uncapableResult.Value;

			var mutateResult = await this.Mono.MutateUntil(
				() => Resume_Internal(),
				() => this.State.Status is not MachineStatus.Paused,
				TimeSpan.FromSeconds(20));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToResume.Title, autoResolve: Constants.MachineMessages.FailedToResume.AutoResolve);
		}
		protected virtual Task Resume_Internal()
		{
			throw new NotImplementedException($"{nameof(Resume_Internal)} has not been implemented on the Connector");
		}

		public async Task<MachineOperationResult> Stop()
		{
			if (State.OpIfNotCapable(MachineCapabilities.Control, out var uncapableResult)) return uncapableResult.Value;

			var mutateResult = await this.Mono.MutateUntil(
				() => Stop_Internal(),
				() => this.State.Status is MachineStatus.Canceled,
				TimeSpan.FromSeconds(15));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToStop.Title, autoResolve: Constants.MachineMessages.FailedToStop.AutoResolve);
		}
		protected virtual Task Stop_Internal()
		{
			throw new NotImplementedException($"{nameof(Stop_Internal)} has not been implemented on the Connector");
		}
	}
	#endregion

	#region Mark as Idle
	public abstract partial class MachineConnection
	{
		/// <summary>
		/// Manually marks a machine as <see cref="MachineStatus.Idle"/> 
		/// if <see cref="MachineStatus.Printed"/> or <see cref="MachineStatus.Canceled"/>.
		/// </summary>
		public async Task<MachineOperationResult> MarkAsIdle()
		{
			// Pre-checks

			if (this.State.Status is MachineStatus.Idle) return MachineOperationResult.Ok;

			if (!(this.State.Status is MachineStatus.Printed or MachineStatus.Canceled))
			{
				return MachineOperationResult.Fail(
					Constants.MachineMessages.FailedToClearBed.Title,
					$"Machine must be {nameof(MachineStatus.Printed)} or {nameof(MachineStatus.Canceled)}.");
			}

			// Action to Perform

			var mutateAction = await this.Mono.MutateUntil(
				ClearBed_Internal,
				() => this.State.Status is MachineStatus.Idle,
				TimeSpan.FromSeconds(30));

			return mutateAction.IntoOperationResult(Constants.MachineMessages.FailedToClearBed.Title, autoResolve: Constants.MachineMessages.FailedToClearBed.AutoResolve);
		}
		protected virtual Task ClearBed_Internal()
		{
			throw new NotImplementedException($"{nameof(ClearBed_Internal)} has not been implemented on the Connector");
		}
	}
	#endregion

	#region Air Duct
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> ChangeAirDuct(MachineAirDuctMode mode)
		{
			if (State.OpIfNotCapable(MachineCapabilities.AirDuct, out var uncapableResult)) return uncapableResult.Value;

			var mutateResult = await this.Mono.MutateUntil(
				() => ChangeAirDuctMode_Internal(mode),
				() => this.State.AirDuctMode == mode,
				TimeSpan.FromSeconds(10));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToChangeAirDuct.Title, autoResolve: Constants.MachineMessages.FailedToChangeAirDuct.AutoResolve);
		}
		protected virtual Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
		{
			throw new NotImplementedException($"{nameof(ChangeAirDuctMode_Internal)} has not been implemented on the Connector");
		}

		}
	#endregion

	#region Material Unit Start & End Heating
	public abstract partial class MachineConnection
	{
		public async Task<MachineOperationResult> BeginMUHeating(string unitID, HeatingSettings settings)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) return MachineOperationResult.Fail(Constants.MachineMessages.FailedToBeginMUHeating.Title, $"Unit of ID {unitID} does not exist!");

			// Check for AMS Heating Capability 

			if (unit.IfNotCapable(MUCapabilities.Heating, out var uncapableResult)) return uncapableResult.Value;

			// Check for DoSpin Capability 

			if (settings.DoSpin.HasValue && unit.IfNotCapable(MUCapabilities.Heating_CanSpin, out uncapableResult)) return uncapableResult.Value;

			if (!unit.HeatingConstraints.HasValue || !settings.IsInRange(unit.HeatingConstraints.Value))
			{
				return MachineOperationResult.Fail(Constants.MachineMessages.FailedToBeginMUHeating.Title, $"Temp must be in Range {unit.HeatingConstraints!.Value}");
			}

			var mutateResult = await this.Mono.MutateUntil(
				() => BeginMUHeating_Internal(unitID, settings),
				() => _State.MaterialUnits.Values.Any(item => item.ID.Equals(unitID) && item.HeatingJob != null),
				TimeSpan.FromSeconds(30));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToBeginMUHeating.Title, autoResolve: Constants.MachineMessages.FailedToBeginMUHeating.AutoResolve);
		}
		protected virtual Task BeginMUHeating_Internal(string unitID, HeatingSettings settings)
		{
			throw new NotImplementedException($"{nameof(BeginMUHeating_Internal)} has not been implemented on the Connector");
		}

		public async Task<MachineOperationResult> EndMUHeating(string unitID)
		{
			var unit = _State.MaterialUnits.GetValueOrDefault(unitID);

			if (unit == null) return MachineOperationResult.Fail(Constants.MachineMessages.FailedToEndMUHeating.Title, $"Unit of ID {unitID} does not exist!");

			if (unit.HeatingJob == null) return MachineOperationResult.Ok;

			// Check for AMS Heating Capability 

			if (unit.IfNotCapable(MUCapabilities.Heating, out var uncapableResult)) return uncapableResult.Value;

			var mutateResult = await this.Mono.MutateUntil(
				() => EndMaterialUnitHeating_Internal(unitID),
				() => _State.MaterialUnits.Values.Any(item => item.ID.Equals(unitID) && !item.HeatingJob.HasValue),
				TimeSpan.FromSeconds(30));

			return mutateResult.IntoOperationResult(Constants.MachineMessages.FailedToEndMUHeating.Title, autoResolve: Constants.MachineMessages.FailedToEndMUHeating.AutoResolve);
		}
		protected virtual Task EndMaterialUnitHeating_Internal(string unitID)
		{
			throw new NotImplementedException($"{nameof(EndMaterialUnitHeating_Internal)} has not been implemented on the Connector");
		}
	}
	#endregion

	#region Schedule Material Unit Heating
	public abstract partial class MachineConnection
	{
		/// <summary>
		/// Schedules a material unit heating/drying job to be executed based on a CRON expression.
		/// </summary>
		public MachineOperationResult ScheduleMUHeating(string unitID, HeatingSchedule schedule)
		{
			if (!_State.MaterialUnits.TryGetValue(unitID, out var unit))
			{
				return MachineOperationResult.Fail(MachineMessages.MUDoesNotExist(unitID));
			}

			// Check for AMS Heating Capability 

			if (unit.IfNotCapable(MUCapabilities.Heating, out var uncapableResult)) return uncapableResult.Value;

			CommitState(changes => changes.UpdateMaterialUnits(unitID, mu => mu.SetHeatingSchedule(schedule)));

			return MachineOperationResult.Ok;
		}

		/// <summary>
		/// Cancels a scheduled material unit heating/drying job.
		/// </summary>
		public MachineOperationResult CancelScheduledMUHeating(string unitID, HeatingSchedule schedule)
		{
			if (!_State.MaterialUnits.TryGetValue(unitID, out var unit))
			{
				return MachineOperationResult.Fail(MachineMessages.MUDoesNotExist(unitID));
			}

			CommitState(changes => changes.UpdateMaterialUnits(unitID, mu => mu.RemoveHeatingSchedule(schedule)));

			return MachineOperationResult.Ok;
		}

		/// <summary>
		/// Cancels all scheduled heating jobs for a material unit.
		/// </summary>
		public MachineOperationResult CancelAllScheduledMUHeating(string unitID)
		{
			if (!_State.MaterialUnits.TryGetValue(unitID, out var unit))
			{
				return MachineOperationResult.Fail(MachineMessages.MUDoesNotExist(unitID));
			}

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

			return MachineOperationResult.Ok;
		}
	}
	#endregion

	#region Scheduled Tasks
	public abstract partial class MachineConnection
	{
		/// <summary>
		/// Schedules a print job to be executed based on a CRON expression.
		/// </summary>
		/// <param name="scheduledPrint">The scheduled print configuration.</param>
		public MachineOperationResult SchedulePrint(ScheduledPrint scheduledPrint)
		{
			if (State.OpIfNotCapable(MachineCapabilities.StartLocalJob, out var uncapableResult)) return uncapableResult.Value;

			CommitState(changes => changes.SetScheduledPrints(scheduledPrint));

			return MachineOperationResult.Ok;
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
					schedule.SchedulerID = Scheduler.ScheduleAsync(schedule.Timing, TimeZoneInfo.Local, async (meta) =>
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
					scheduledPrint.SchedulerID = Scheduler.ScheduleAsync(scheduledPrint.Timing, TimeZoneInfo.Local, async (meta) =>
					{
						var jobName = meta.LocalJob.Name;

						if (!_State.LocalJobs.Any(lj => lj.File.Equals(meta.LocalJob.File)))
						{
							CommitState(changes => changes
								.SetNotifications(Constants.MachineMessages.ScheduledPrintSkipped(jobName, "Local file no longer exists on the machine"))
								.RemoveScheduledPrints(meta));

							return;
						}

						if (_State.Status is MachineStatus.Disconnected || _State.Status is not MachineStatus.Idle)
						{
							var reason = _State.Status is MachineStatus.Disconnected ? "Not Connected" : "Not Ready";

							CommitState(changes => changes
								.SetNotifications(Constants.MachineMessages.ScheduledPrintSkipped(jobName, reason)));

							return;
						}

						var originalOptions = meta.Options;
						var required = meta.LocalJob.MaterialsToPrint;

						try
						{
							var opResult = await this.PrintLocal(meta.LocalJob, meta.Options);

							if (!opResult.Success) CommitState(changes => changes
								.SetNotifications(Constants.MachineMessages.ScheduledPrintFailed(jobName, opResult.Reasoning.HasValue ? opResult.Reasoning.Value.Body : "Unknown Error")));
						}
						catch (Exception ex)
						{
							Logger.Error(ex, $"Scheduled Print '{jobName}' Failed");
							CommitState(changes => changes
								.SetNotifications(Constants.MachineMessages.ScheduledPrintFailed(jobName, ex.Message)));
						}

					}, scheduledPrint);
				}
			}
		}
	}
	#endregion

	#region Oven Media Engine Streaming
	public abstract partial class MachineConnection
	{
		internal virtual bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
		{
			passURL = null;
			return false;
		}
	}
	#endregion
}
