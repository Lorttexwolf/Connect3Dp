namespace Connect3Dp.Tracked
{
    public sealed class TrackedDictionary<TKey, TValue>(Func<IDictionary<TKey, TValue>> getter) : Tracked<TrackedDictionary<TKey, TValue>.Changes>
    {
        private readonly Func<IDictionary<TKey, TValue>> _Getter = getter;
        private IDictionary<TKey, TValue> _LastValue = Clone(getter());

        public override bool HasChanged => !DictionaryEquals(_LastValue, _Getter());

        public record Changes(
            IReadOnlyDictionary<TKey, TValue> Inserted,
            IReadOnlyDictionary<TKey, TValue> Removed
        );

        protected override Changes Use_Internal()
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
