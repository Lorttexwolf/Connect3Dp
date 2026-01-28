using System.Diagnostics;

namespace Connect3Dp.Tracked
{
    public sealed class TrackedCollection<T>(Func<ICollection<T>> getter) : Tracked<TrackedCollection<T>.Changes>
    {
        private readonly Func<ICollection<T>> _Getter = getter;
        private ICollection<T> _LastValue = Clone(getter());

        public override bool HasChanged => !CollectionEquals(_LastValue, _Getter());

        public record Changes(IReadOnlyCollection<T> Inserted, IReadOnlyCollection<T> Removed, IReadOnlyCollection<T> Previous);

        protected override Changes LastValue()
        {
            var current = _Getter();

            var inserted = new List<T>();
            var removed = new List<T>();

            // ---- ORDERED (List) DIFF ----
            if (current is IList<T> curList && _LastValue is IList<T> lastList)
            {
                int i = 0;
                while (i < curList.Count && i < lastList.Count)
                {
                    if (!EqualityComparer<T>.Default.Equals(curList[i], lastList[i]))
                    {
                        removed.Add(lastList[i]);
                        inserted.Add(curList[i]);
                    }
                    i++;
                }

                for (int j = i; j < curList.Count; j++)
                    inserted.Add(curList[j]);

                for (int j = i; j < lastList.Count; j++)
                    removed.Add(lastList[j]);
            }
            // ---- UNORDERED (Set) DIFF ----
            else if (current is ISet<T> curSet && _LastValue is ISet<T> lastSet)
            {
                foreach (var item in curSet)
                    if (!lastSet.Contains(item))
                        inserted.Add(item);

                foreach (var item in lastSet)
                    if (!curSet.Contains(item))
                        removed.Add(item);
            }
            // ---- FALLBACK (Enumeration-based) ----
            else
            {
                foreach (var item in current)
                    if (!_LastValue.Contains(item))
                        inserted.Add(item);

                foreach (var item in _LastValue)
                    if (!current.Contains(item))
                        removed.Add(item);
            }

            var prev = _LastValue;
            return new Changes(inserted, removed, (IReadOnlyCollection<T>)prev);
        }

        private static ICollection<T> Clone(ICollection<T> src) => src switch
        {
            IList<T> => new List<T>(src),
            ISet<T> => new HashSet<T>(src),
            _ => [..src]
        };

        private static bool CollectionEquals(ICollection<T> a, ICollection<T> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            if (a is ISet<T> setA && b is ISet<T> setB) return setA.SetEquals(setB);

            return a.SequenceEqual(b);
        }

        public override void View()
        {
            _LastValue = Clone(_Getter());
        }
    }
}
