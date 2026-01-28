using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Connect3Dp.Tracked
{
    public abstract class Tracked<R>
    {
        public abstract bool HasChanged { get; }

        /// <summary>
        /// Returns the last-seen value. If markAsSeen=true, updates the last seen value to the current value.
        /// </summary>
        public R Use(bool markAsSeen)
        {
            R val = this.LastValue();
            if (markAsSeen) View();
            return val;
        }

        protected abstract R LastValue();

        public bool TryUse(bool markAsSeen, [NotNullWhen(true)] out R? lastValue)
        {
            lastValue = Use(markAsSeen);
            return HasChanged;
        }

        public abstract void View();
    }

    public sealed class TrackedStruct<T>(Func<T> getter) : Tracked<T> where T : struct
    {
        private readonly Func<T> _Getter = getter;
        private T _LastValue = getter();

        public override bool HasChanged => !EqualityComparer<T>.Default.Equals(_LastValue, _Getter());

        protected override T LastValue()
        {
            return _LastValue;
        }

        public override void View()
        {
            _LastValue = _Getter();
        }
    }

    public sealed class TrackedValue<T>(Func<T?> getter) : Tracked<T> where T : class, ICloneable
    {
        private readonly Func<T?> _Getter = getter;
        private T? _LastValue = getter();

        public override bool HasChanged => !EqualityComparer<T>.Default.Equals(_LastValue, _Getter());

        protected override T LastValue()
        {
            return _LastValue;
        }

        public override void View()
        {
            var gotValue = _Getter();

            if (gotValue != null)
            {
                _LastValue = (T?)gotValue.Clone();
            }
            else
            {
                _LastValue = null;
            }

        }
    }
}
