using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Connect3Dp.Logging
{
	public sealed class BufferedLoggerProvider : ILoggerProvider
	{
		private readonly ForwardingLogger.Entry[] _buffer;
		private readonly int _capacity;
		private int _head;
		private int _count;
		private readonly Lock _lock = new();
		private readonly ConcurrentDictionary<string, ForwardingLogger> _loggers = new();
		private readonly ConcurrentBag<BufferedLoggerChannel> _subscribers = new();

		public BufferedLoggerProvider(int capacity = 500)
		{
			_capacity = capacity;
			_buffer = new ForwardingLogger.Entry[capacity];
		}

		public IEnumerable<ForwardingLogger.Entry> Entries
		{
			get
			{
				int start, count;
				lock (_lock)
				{
					start = _count < _capacity ? 0 : _head;
					count = _count;
				}

				for (int i = 0; i < count; i++) yield return _buffer[(start + i) % _capacity];
			}
		}

		public BufferedLoggerChannel Subscribe(int channelCapacity = 100)
		{
			var sub = new BufferedLoggerChannel(channelCapacity, () => _subscribers.TryTake(out _));
			_subscribers.Add(sub);
			return sub;
		}

		public ILogger CreateLogger(string categoryName) =>
			_loggers.GetOrAdd(categoryName, name => new ForwardingLogger(name, this));

		internal void AddEntry(ForwardingLogger.Entry entry)
		{
			lock (_lock)
			{
				_buffer[_head] = entry;
				_head = (_head + 1) % _capacity;
				if (_count < _capacity) _count++;
			}

			foreach (var subscriber in _subscribers)
				subscriber.Write(entry);
		}

		public void Dispose()
		{
			foreach (var subscriber in _subscribers)
				subscriber.Dispose();
			_loggers.Clear();
		}
	}

	public sealed class BufferedLoggerChannel : IDisposable
	{
		private readonly Channel<ForwardingLogger.Entry> _channel;
		private readonly Action _unsubscribe;

		public ChannelReader<ForwardingLogger.Entry> Reader => _channel.Reader;

		internal BufferedLoggerChannel(int capacity, Action unsubscribe)
		{
			_unsubscribe = unsubscribe;
			_channel = Channel.CreateBounded<ForwardingLogger.Entry>(new BoundedChannelOptions(capacity)
			{
				FullMode = BoundedChannelFullMode.DropOldest,
				SingleReader = true,
				SingleWriter = true
			});
		}

		internal void Write(ForwardingLogger.Entry entry) => _channel.Writer.TryWrite(entry);

		public void Dispose()
		{
			_unsubscribe();
			_channel.Writer.Complete();
		}
	}
}
