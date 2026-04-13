using System.Collections.Generic;

namespace Lib3Dp.Utilities
{
	internal static class CollectionUtils
	{
		public static bool AreDictionariesEqual<TKey, TValue>(Dictionary<TKey, TValue>? a, Dictionary<TKey, TValue>? b)
		{
			if (ReferenceEquals(a, b)) return true;
			if (a is null && b is null) return true;
			if (a is null || b is null) return false;
			if (a.Count != b.Count) return false;

			var valueComparer = EqualityComparer<TValue>.Default;
			foreach (var kv in a)
			{
				if (!b.TryGetValue(kv.Key, out var otherVal)) return false;
				if (!valueComparer.Equals(kv.Value, otherVal)) return false;
			}

			return true;
		}
	}
}
