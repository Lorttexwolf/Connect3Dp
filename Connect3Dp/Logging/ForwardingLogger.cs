using Microsoft.Extensions.Logging;

namespace Connect3Dp.Logging
{
	public sealed class ForwardingLogger(string category, BufferedLoggerProvider provider) : ILogger
	{
		public record Entry(LogLevel Level, string Category, string Message, Exception? Exception, DateTimeOffset Time);

		private sealed class ScopeNode(object? value, ScopeNode? parent)
		{
			public readonly object? Value = value;
			public readonly ScopeNode? Parent = parent;
		}

		private static readonly AsyncLocal<ScopeNode?> _scopeHead = new();

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel)) return;
			provider.AddEntry(new Entry(logLevel, category, formatter(state, exception), exception, DateTimeOffset.UtcNow));
		}

		public IDisposable BeginScope<TState>(TState state) where TState : notnull
		{
			var previous = _scopeHead.Value;
			_scopeHead.Value = new ScopeNode(state, previous);
			return new Scope(() => _scopeHead.Value = previous);
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		private sealed class Scope(Action restore) : IDisposable
		{
			private bool _disposed;
			public void Dispose()
			{
				if (_disposed) return;
				_disposed = true;
				restore();
			}
		}
	}
}
