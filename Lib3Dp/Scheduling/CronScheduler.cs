using Cronos;
using Lib3Dp.Utilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace Lib3Dp.Scheduling
{
	/// <summary>
	/// Minimal CRON scheduler for executing actions on a schedule.
	/// </summary>
	internal class CronScheduler : IDisposable
	{
		private static readonly Logger Logger = Logger.OfCategory(nameof(CronScheduler));

		private readonly Timer Timer;
		private readonly List<IScheduledTask> Tasks = [];
		private readonly object Lock = new();
		private bool IsDisposed;

		public bool IsRunning => !IsDisposed;

		public CronScheduler(TimeSpan checkInterval)
		{
			Timer = new Timer(CheckScheduledTasks, null, TimeSpan.Zero, checkInterval);
		}

		public Guid Schedule(CronExpression expression, TimeZoneInfo timeZone, Action action)
		{
			return Schedule<object?>(expression, timeZone, _ => action(), null);
		}
		public Guid Schedule<TMetadata>(CronExpression expression, TimeZoneInfo timeZone, Action<TMetadata> action, TMetadata metadata)
		{
			var task = new ScheduledTask<TMetadata>(Guid.NewGuid(), expression, timeZone, action, metadata);

			lock (Lock)
			{
				Tasks.Add(task);
				var next = task.Expression.GetNextOccurrence(DateTimeOffset.UtcNow, task.TimeZone);
				task.NextRun = next;
			}

			return task.Id;
		}

		public Guid ScheduleAsync(CronExpression expression, TimeZoneInfo timeZone, Func<Task> action)
		{
			return ScheduleAsync<object?>(expression, timeZone, _ => action(), null);
		}
		public Guid ScheduleAsync<TMetadata>(CronExpression expression, TimeZoneInfo timeZone, Func<TMetadata, Task> action, TMetadata metadata, [CallerMemberName] string callerName = "")
		{
			var task = new AsyncScheduledTask<TMetadata>(Guid.NewGuid(), expression, timeZone, action, metadata);

			lock (Lock)
			{
				Tasks.Add(task);
				var next = task.Expression.GetNextOccurrence(DateTimeOffset.UtcNow, task.TimeZone);
				task.NextRun = next;
			}

			Logger.Trace($"Scheduled action called from {callerName}() to run next at {task.NextRun!.Value}");

			return task.Id;
		}

		public bool End(Guid taskId)
		{
			lock (Lock)
			{
				var task = Tasks.FirstOrDefault(t => t.Id == taskId);

				if (task == null) return false;

				Tasks.Remove(task);

				return true;
			}
		}

		public bool IsTaskRunning(Guid taskId)
		{
			lock (Lock)
			{
				var task = Tasks.FirstOrDefault(t => t.Id == taskId);

				return task != null && this.IsRunning;
			}
		}

		private void CheckScheduledTasks(object? state)
		{
			if (IsDisposed) return;

			var now = DateTimeOffset.UtcNow;

			List<IScheduledTask> tasksToRun = new();

			// Collect due tasks and pre-schedule their next run while holding the lock to avoid double-scheduling.
			lock (Lock)
			{
				foreach (var t in Tasks)
				{
					if (t.NextRun.HasValue && t.NextRun.Value <= now)
					{
						tasksToRun.Add(t);
						var next = t.Expression.GetNextOccurrence(now, t.TimeZone);
						t.NextRun = next?.ToUniversalTime();
					}
				}
			}

			// Execute outside the lock
			foreach (var task in tasksToRun)
			{
				Task.Run(() =>
				{
					try
					{
						Logger.Trace($"Invoking scheduled event GUID {task.Id}");

						task.Execute();
					}
					catch (Exception ex)
					{
						Logger.Error(ex, $"An exception occurred in scheduled event GUID {task.Id}.");
					}
				});
			}
		}

		public void Dispose()
		{
			if (IsDisposed) return;

			GC.SuppressFinalize(this);

			IsDisposed = true;
			Timer?.Dispose();

			lock (Lock)
			{
				Tasks.Clear();
			}
		}

		#region Task Classes

		private interface IScheduledTask
		{
			Guid Id { get; }
			CronExpression Expression { get; }
			TimeZoneInfo TimeZone { get; set; }
			DateTimeOffset? NextRun { get; set; }
			void Execute();
		}

		private class ScheduledTask<TMetadata>(Guid id, CronExpression expression, TimeZoneInfo timeZone, Action<TMetadata> action, TMetadata metadata) : IScheduledTask
		{
			public Guid Id { get; } = id;
			public CronExpression Expression { get; } = expression;
			public DateTimeOffset? NextRun { get; set; }
			public TimeZoneInfo TimeZone { get; set; } = timeZone;

			private readonly Action<TMetadata> Action = action;
			private readonly TMetadata Metadata = metadata;

			public void Execute() => Action(Metadata);
		}

		private class AsyncScheduledTask<TMetadata>(Guid id, CronExpression expression, TimeZoneInfo timeZone, Func<TMetadata, Task> action, TMetadata metadata) : IScheduledTask
		{
			public Guid Id { get; } = id;
			public CronExpression Expression { get; } = expression;
			public DateTimeOffset? NextRun { get; set; }
			public TimeZoneInfo TimeZone { get; set; } = timeZone;

			private readonly Func<TMetadata, Task> Action = action;
			private readonly TMetadata Metadata = metadata;

			public void Execute()
			{
				// Start the async work without blocking the caller thread. Log exceptions if the task faults.
				var _ = Action(Metadata).ContinueWith(t =>
				{
					if (t.IsFaulted)
					{
						Logger.Error(t.Exception!, $"An exception occurred in scheduled async event GUID {Id}.");
					}
				}, TaskScheduler.Default);
			}
		}

		#endregion
	}
}
