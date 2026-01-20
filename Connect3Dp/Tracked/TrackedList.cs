using System.Diagnostics;

namespace Connect3Dp.Tracked
{
    public sealed class TrackedList<T>(Func<IList<T>> getter) : Tracked<TrackedList<T>.Changes>
    {
        private readonly Func<IList<T>> _Getter = getter;
        private IList<T> _LastValue = Clone(getter());

        public override bool HasChanged => !ListEquals(_LastValue, _Getter());

        public record Changes(
            IReadOnlyList<T> Inserted,
            IReadOnlyList<T> Removed,
            IList<T> PreviousSnapshot
        );

        protected override Changes Use_Internal()
        {
            var current = _Getter();

            var inserted = new List<T>();
            var removed = new List<T>();

            // basic list diff (not LCS — simple and predictable)
            int i = 0;
            while (i < current.Count && i < _LastValue.Count)
            {
                if (!EqualityComparer<T>.Default.Equals(current[i], _LastValue[i]))
                {
                    removed.Add(_LastValue[i]);
                    inserted.Add(current[i]);
                }
                i++;
            }

            // remaining additions
            for (int j = i; j < current.Count; j++)
                inserted.Add(current[j]);

            // remaining removals
            for (int j = i; j < _LastValue.Count; j++)
                removed.Add(_LastValue[j]);

            var prev = _LastValue;

            return new Changes(inserted, removed, prev);
        }

        private static IList<T> Clone(IList<T> src) => [..src];

        private static bool ListEquals(IList<T> a, IList<T> b)
        {
            if (b == null && a == null) return true;
            if (b == null || a == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(a[i], b[i]))
                {
                    return false;
                }
                else
                {
                    
                }
            }
            return true;
        }

        public override void View()
        {
            _LastValue = Clone(_Getter());
        }
    }
}
