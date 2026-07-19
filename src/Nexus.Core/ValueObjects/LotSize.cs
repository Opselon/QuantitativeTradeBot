namespace Nexus.Core.ValueObjects
{
    public readonly struct LotSize : IEquatable<LotSize>, IComparable<LotSize>
    {
        public double Value { get; }

        public LotSize(double value)
        {
            if (value < 0)
                throw new ArgumentException("Lot size cannot be negative.", nameof(value));
            Value = value;
        }

        public bool Equals(LotSize other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is LotSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public int CompareTo(LotSize other)
        {
            return Value.CompareTo(other.Value);
        }

        public override string ToString() => $"{Value:F2}";

        public static bool operator ==(LotSize left, LotSize right) => left.Equals(right);
        public static bool operator !=(LotSize left, LotSize right) => !left.Equals(right);
        public static bool operator <(LotSize left, LotSize right) => left.CompareTo(right) < 0;
        public static bool operator <=(LotSize left, LotSize right) => left.CompareTo(right) <= 0;
        public static bool operator >(LotSize left, LotSize right) => left.CompareTo(right) > 0;
        public static bool operator >=(LotSize left, LotSize right) => left.CompareTo(right) >= 0;

        public static explicit operator double(LotSize lotSize) => lotSize.Value;
        public static implicit operator LotSize(double value) => new LotSize(value);
    }
}
