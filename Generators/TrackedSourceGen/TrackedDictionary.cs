using System;
using System.Collections.Generic;

namespace TrackedSourceGen
{
	public sealed class TrackedDictionary<TKey, TValue> : Tracked<TrackedDictionary<TKey, TValue>.Changes>
	{
		private readonly Func<IDictionary<TKey, TValue>> _Getter;
		private IDictionary<TKey, TValue> _LastValue;

		public TrackedDictionary(Func<IDictionary<TKey, TValue>> getter)
		{
			_Getter = getter;
			_LastValue = Clone(getter());
		}

		public override bool HasChanged => !DictionaryEquals(_LastValue, _Getter());

		public class Changes
		{
			public IReadOnlyDictionary<TKey, TValue> Inserted { get; }
			public IReadOnlyDictionary<TKey, TValue> Removed { get; }

			public Changes(IReadOnlyDictionary<TKey, TValue> inserted, IReadOnlyDictionary<TKey, TValue> removed)
			{
				this.Inserted = inserted;
				this.Removed = removed;
			}
		}

		protected override Changes LastValue()
		{
			var current = _Getter();

			var inserted = new Dictionary<TKey, TValue>();
			var removed = new Dictionary<TKey, TValue>();

			// detect added/updated
			foreach (var kv in current)
			{
				if (!_LastValue.TryGetValue(kv.Key, out var oldVal))
				{
					inserted[kv.Key] = kv.Value;
				}
				else if (!EqualityComparer<TValue>.Default.Equals(oldVal, kv.Value))
				{
					inserted[kv.Key] = kv.Value;
				}
			}

			// detect removed
			foreach (var kv in _LastValue)
			{
				if (!current.ContainsKey(kv.Key))
					removed[kv.Key] = kv.Value;
			}

			return new Changes(inserted, removed);
		}

		private static IDictionary<TKey, TValue> Clone(IDictionary<TKey, TValue> src)
			=> new Dictionary<TKey, TValue>(src);

		private static bool DictionaryEquals(IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
		{
			if (a.Count != b.Count) return false;
			foreach (var kv in a)
			{
				if (!b.TryGetValue(kv.Key, out var val)) return false;
				if (!EqualityComparer<TValue>.Default.Equals(kv.Value, val)) return false;
			}
			return true;
		}

		public override void View()
		{
			_LastValue = Clone(_Getter());
		}
	}
}
