using System;
using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable percentage value (e.g., 5.0 for 5%).
    /// </summary>
    public readonly struct Percentage : IEquatable<Percentage>, IComparable<Percentage>
    {
        public double Value { get; }

        public Percentage(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidPercentageException("Percentage value must be a valid, finite number.");
            Value = value;
        }

        public double AsFraction => Value / 100.0;

        public double Of(double total) => total * AsFraction;

        public decimal Of(decimal total) => total * (decimal)AsFraction;

        public bool Equals(Percentage other) => Value.Equals(other.Value);

        public override bool Equals(object? obj) => obj is Percentage other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Percentage other) => Value.CompareTo(other.Value);

        public override string ToString() => $"{Value:F2}%";

        public static Percentage operator +(Percentage left, Percentage right) => new Percentage(left.Value + right.Value);
        public static Percentage operator -(Percentage left, Percentage right) => new Percentage(left.Value - right.Value);

        public static bool operator ==(Percentage left, Percentage right) => left.Equals(right);
        public static bool operator !=(Percentage left, Percentage right) => !left.Equals(right);
        public static bool operator <(Percentage left, Percentage right) => left.CompareTo(right) < 0;
        public static bool operator <=(Percentage left, Percentage right) => left.CompareTo(right) <= 0;
        public static bool operator >(Percentage left, Percentage right) => left.CompareTo(right) > 0;
        public static bool operator >=(Percentage left, Percentage right) => left.CompareTo(right) >= 0;

        public static implicit operator double(Percentage percentage) => percentage.Value;
        public static implicit operator Percentage(double value) => new Percentage(value);
    }
}
