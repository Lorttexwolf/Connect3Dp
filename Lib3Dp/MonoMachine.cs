using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lib3Dp
{
	/// <summary>
	/// A utility object which limits a machine to one operation at once.
	/// </summary>
	public class MonoMachine
	{
		protected SemaphoreSlim Semaphore = new(1, 1);

		private readonly Stopwatch Stopwatch = new();

		public bool IsMutating => Semaphore.CurrentCount == 0;

		/// <summary>
		/// Executes a certain <paramref name="invokeAction"/> then blocks until <paramref name="predicate"/> up to the <paramref name="timeout"/>.
		/// </summary>
		public async Task<MutationResult> MutateUntil(Func<Task> invokeAction, Func<bool> predicate, TimeSpan timeout, [CallerMemberName] string callerName = "")
		{
			await Semaphore.WaitAsync();

			try
			{
				await invokeAction();
			}
			catch (Exception ex)
			{
				return new MutationResult(TimeSpan.Zero, false, ex);
			}

			Stopwatch.Restart();

			// Blocks until predicate returns true.

			while (!predicate.Invoke())
			{
				if (Stopwatch.Elapsed <= timeout)
				{
					await Task.Delay(TimeSpan.FromMilliseconds(250));
				}
				else
				{
					return new MutationResult(timeout, true, null);
				}
			}

			Stopwatch.Stop();

			var results = new MutationResult(Stopwatch.Elapsed, false, null);

			Semaphore.Release();

			return results;
		}

		public readonly record struct MutationResult(TimeSpan TimeSpent, bool TimedOut, Exception? InvokeException)
		{
			public bool IsSuccess => InvokeException == null && !TimedOut;
		}
	}
}
