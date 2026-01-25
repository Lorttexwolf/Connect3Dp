using PartialSourceGen;

namespace Connect3Dp.State
{

    [Partial]
    public readonly struct HeatingSettings : IEquatable<HeatingSettings>
    {
        public double TempC { get; init; }
        public bool? DoSpin { get; init; }

        public TimeSpan Duration { get; init; }

        public readonly bool Equals(HeatingSettings other)
        {
            return TempC.Equals(other.TempC) && Nullable.Equals(DoSpin, other.DoSpin) && Duration.Equals(other.Duration);
        }

        public override readonly bool Equals(object? obj) => obj is HeatingSettings other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(TempC, DoSpin, Duration);

        public readonly bool InRange(HeatingConstraints constraints)
        {
            return TempC >= constraints.MinTempC && TempC <= constraints.MaxTempC;
        }

        public static bool operator ==(HeatingSettings left, HeatingSettings right) => left.Equals(right);

        public static bool operator !=(HeatingSettings left, HeatingSettings right) => !left.Equals(right);
    }

}
