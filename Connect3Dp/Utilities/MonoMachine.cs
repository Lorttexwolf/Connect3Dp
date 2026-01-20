using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Utilities
{
    /// <summary>
    /// A utility object which prevents multiple operations at the same time.
    /// </summary>
    internal class MonoMachine
    {
        public bool IsMutating => this.Semaphore.CurrentCount > 0;

        protected SemaphoreSlim Semaphore = new(1, 1);

        private readonly System.Timers.Timer _watchdogTimer = new(250);

        public async Task<T> Mutate<T>(Func<Task<T>> mutateAction, [CallerMemberName] string callerName = "")
        {
            await this.Semaphore.WaitAsync();

            var result = await mutateAction();

            this.Semaphore.Release();

            return result;
        }

        public async Task Mutate(Func<Task> mutateAction, [CallerMemberName] string callerName = "")
        {
            await this.Semaphore.WaitAsync();

            await mutateAction();

            this.Semaphore.Release();
        }

        /// <summary>
        /// Executes a certain action, then waits for the predicate to return true.
        /// </summary>
        /// <returns>
        /// Returns whether or not the predicate has timed out.
        /// </returns>
        public async Task<bool> MutateUntil(Func<Task> mutateAction, Func<bool> predicate, TimeSpan timeout, [CallerMemberName] string callerName = "")
        {
            await this.Semaphore.WaitAsync();

            await mutateAction();

            _watchdogTimer.Interval = timeout.TotalMilliseconds;
            _watchdogTimer.Start();

            // Blocks until predicate returns true.

            while (!predicate.Invoke() && _watchdogTimer.Enabled)
            {

                await Task.Delay(TimeSpan.FromMilliseconds(250));
            }

            this.Semaphore.Release();

            return !_watchdogTimer.Enabled;
        }
    }
}
