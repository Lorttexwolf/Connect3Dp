using System.Runtime.CompilerServices;

namespace Lib3Dp
{
	/// <summary>
	/// A utility object which prevents multiple operations at the same time.
	/// </summary>
	internal class MonoMachine
	{
		public bool IsMutating => Semaphore.CurrentCount > 0;

		protected SemaphoreSlim Semaphore = new(1, 1);

		private readonly System.Timers.Timer _watchdogTimer = new(250);

		public async Task<T> Mutate<T>(Func<Task<T>> mutateAction, [CallerMemberName] string callerName = "")
		{
			await Semaphore.WaitAsync();

			var result = await mutateAction();

			Semaphore.Release();

			return result;
		}

		public async Task Mutate(Func<Task> mutateAction, [CallerMemberName] string callerName = "")
		{
			await Semaphore.WaitAsync();

			await mutateAction();

			Semaphore.Release();
		}

		/// <summary>
		/// Executes a certain action, then waits for the predicate to return true.
		/// </summary>
		/// <returns>
		/// Returns whether or not the predicate has timed out.
		/// </returns>
		public async Task<bool> MutateUntil(Func<Task> mutateAction, Func<bool> predicate, TimeSpan timeout, [CallerMemberName] string callerName = "")
		{
			await Semaphore.WaitAsync();

			await mutateAction();

			_watchdogTimer.Interval = timeout.TotalMilliseconds;
			_watchdogTimer.Start();

			// Blocks until predicate returns true.

			while (!predicate.Invoke() && _watchdogTimer.Enabled)
			{

				await Task.Delay(TimeSpan.FromMilliseconds(250));
			}

			Semaphore.Release();

			return !_watchdogTimer.Enabled;
		}
	}
}
