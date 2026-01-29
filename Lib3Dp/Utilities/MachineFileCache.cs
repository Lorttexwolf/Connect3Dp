using Lib3Dp.State;
using System.Collections.Concurrent;

namespace Lib3Dp.Utilities
{
	/// <summary>
	/// A utility class for caching <see cref="MachineFile">MachineFiles</see> for a certain <see cref="TimeSpan"/>, includes <see cref="MaxSizeBytes"/>, evicts by LRU, and computes the <see cref="HitRatio"/> for debugging.
	/// </summary>
	public sealed class MachineFileCache
	{
		private readonly ConcurrentDictionary<string, CacheEntry> Cache = new();

		private long _CacheHits, _CacheMisses, _CurrentSizeBytes;

		public long MaxSizeBytes { get; init; } = 256L * 1024 * 1024; // 256 MB

		public long CurrentSizeBytes => _CurrentSizeBytes;

		public long CacheHits => _CacheHits;
		public long CacheMisses => _CacheMisses;
		public double HitRatio => _CacheHits + _CacheMisses == 0 ? 0 : (double)_CacheHits / (_CacheHits + _CacheMisses);

		public async Task<bool> DownloadCached(MachineFile file, TimeSpan cacheDuration, Stream outStream)
		{
			var now = DateTimeOffset.UtcNow;

			// Cache hit
			if (Cache.TryGetValue(file.ID, out var entry) && entry.ExpiresAt > now)
			{
				Interlocked.Increment(ref _CacheHits);
				entry.LastAccessed = now;

				if (outStream.CanSeek) outStream.SetLength(0);

				await outStream.WriteAsync(entry.Data);
				await outStream.FlushAsync();
				return true;
			}

			Interlocked.Increment(ref _CacheMisses);

			// Cache miss or expired do download
			using var buffer = new MemoryStream();

			if (!await file.Download(buffer)) return false;

			var data = buffer.ToArray();
			var dataSize = data.LongLength;

			// Remove until under capacity
			while (_CurrentSizeBytes + dataSize > MaxSizeBytes && !Cache.IsEmpty)
			{
				var lru = Cache
					.OrderBy(kvp => kvp.Value.LastAccessed)
					.First();

				if (Cache.TryRemove(lru.Key, out var removed))
				{
					Interlocked.Add(ref _CurrentSizeBytes, -removed.Data.LongLength);
				}
			}

			Cache[file.ID] = new CacheEntry(data, now.Add(cacheDuration), now);

			Interlocked.Add(ref _CurrentSizeBytes, dataSize);

			if (outStream.CanSeek) outStream.SetLength(0);

			await outStream.WriteAsync(data);
			await outStream.FlushAsync();
			return true;
		}

		public void Clear(bool resetStats)
		{
			this.Cache.Clear();
			Interlocked.Exchange(ref _CurrentSizeBytes, 0);

			if (resetStats)
			{
				Interlocked.Exchange(ref _CacheHits, 0);
				Interlocked.Exchange(ref _CacheMisses, 0);

			}
			GC.Collect();
		}

		private sealed record CacheEntry(byte[] Data, DateTimeOffset ExpiresAt, DateTimeOffset LastAccessed)
		{
			public DateTimeOffset LastAccessed { get; set; } = LastAccessed;
		}
	}
}
