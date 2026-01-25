using Connect3Dp.Utilities;
using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Connect3Dp.Scheduling
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

        public Guid Schedule(CronExpression expression, Action action)
        {
            return Schedule<object?>(expression, _ => action(), null);
        }
        public Guid Schedule<TMetadata>(CronExpression expression, Action<TMetadata> action, TMetadata metadata)
        {
            var task = new ScheduledTask<TMetadata>(Guid.NewGuid(), expression, action, metadata);

            lock (Lock)
            {
                Tasks.Add(task);
            }

            return task.Id;
        }

        public Guid ScheduleAsync(CronExpression expression, Func<Task> action)
        {
            return ScheduleAsync<object?>(expression, _ => action(), null);
        }
        public Guid ScheduleAsync<TMetadata>(CronExpression expression, Func<TMetadata, Task> action, TMetadata metadata)
        {
            var task = new AsyncScheduledTask<TMetadata>(Guid.NewGuid(), expression, action, metadata);

            lock (Lock)
            {
                Tasks.Add(task);
            }

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
            var task = Tasks.FirstOrDefault(t => t.Id == taskId);

            return task != null && this.IsRunning;
        }

        private void CheckScheduledTasks(object? state)
        {
            if (IsDisposed) return;

            var now = DateTime.Now;

            IEnumerable<IScheduledTask> tasksToRun;

            lock (Lock)
            {
                tasksToRun = Tasks.Where(t => t.NextRun.HasValue && t.NextRun.Value <= now);
            }

            foreach (var task in tasksToRun)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        Logger.Trace($"Invoking scheduled event GUID {task.Id}");

                        task.Execute();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"An exception occurred in scheduled event GUID {task.Id}.\n{ex}");
                    }
                    finally
                    {
                        lock (Lock)
                        {
                            task.NextRun = task.Expression.GetNextOccurrence(now, TimeZoneInfo.Local);
                        }
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
            DateTime? NextRun { get; set; }
            void Execute();
        }

        private class ScheduledTask<TMetadata>(Guid id, CronExpression expression, Action<TMetadata> action, TMetadata metadata) : IScheduledTask
        {
            public Guid Id { get; } = id;
            public CronExpression Expression { get; } = expression;
            public DateTime? NextRun { get; set; } = expression.GetNextOccurrence(DateTime.Now, TimeZoneInfo.Local);

            private readonly Action<TMetadata> Action = action;
            private readonly TMetadata Metadata = metadata;

            public void Execute() => Action(Metadata);
        }

        private class AsyncScheduledTask<TMetadata>(Guid id, CronExpression expression, Func<TMetadata, Task> action, TMetadata metadata) : IScheduledTask
        {
            public Guid Id { get; } = id;
            public CronExpression Expression { get; } = expression;
            public DateTime? NextRun { get; set; } = expression.GetNextOccurrence(DateTime.Now, TimeZoneInfo.Local);

            private readonly Func<TMetadata, Task> Action = action;
            private readonly TMetadata Metadata = metadata;

            public void Execute() => Action(Metadata).GetAwaiter().GetResult();
        }

        #endregion
    }
}