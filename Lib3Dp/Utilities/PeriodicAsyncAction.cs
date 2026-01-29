namespace Lib3Dp.Utilities
{
	public sealed class PeriodicAsyncAction : IAsyncDisposable
	{
		private readonly PeriodicTimer _timer;
		private readonly Func<Task> _action;
		private readonly CancellationTokenSource _cts = new();
		private readonly Task _loopTask;

		public PeriodicAsyncAction(TimeSpan interval, Func<Task> action)
		{
			_action = action;
			_timer = new PeriodicTimer(interval);
			_loopTask = RunAsync();
		}

		private async Task RunAsync()
		{
			try
			{
				while (await _timer.WaitForNextTickAsync(_cts.Token))
				{
					await _action();
				}
			}
			catch (OperationCanceledException)
			{
				// expected on dispose
			}
		}

		public async ValueTask DisposeAsync()
		{
			_cts.Cancel();
			_timer.Dispose();
			await _loopTask;
			_cts.Dispose();
		}
	}
}
