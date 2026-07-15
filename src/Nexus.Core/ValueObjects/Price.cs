using System;
using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable, thread-safe financial instrument price.
    /// Prevents primitive obsession and ensures mathematical consistency.
    /// </summary>
    public readonly struct Price : IEquatable<Price>, IComparable<Price>
    {
        public double Value { get; }

        public Price(double value)
        {
            if (value <= 0)
                throw new InvalidPriceException($"Price must be strictly positive. Value was: {value}");
            Value = value;
        }

        public bool Equals(Price other) => Value.Equals(other.Value);

        public override bool Equals(object? obj) => obj is Price other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Price other) => Value.CompareTo(other.Value);

        public override string ToString() => Value.ToString("F5");

        public static Price Add(Price left, Price right) => new Price(left.Value + right.Value);
        public static Price Subtract(Price left, Price right) => new Price(left.Value - right.Value);

        public static Price operator +(Price left, Price right) => Add(left, right);
        public static Price operator -(Price left, Price right) => Subtract(left, right);
        public static Price operator *(Price left, double scalar) => new Price(left.Value * scalar);
        public static Price operator /(Price left, double scalar) => new Price(left.Value / scalar);

        public static bool operator ==(Price left, Price right) => left.Equals(right);
        public static bool operator !=(Price left, Price right) => !left.Equals(right);
        public static bool operator <(Price left, Price right) => left.CompareTo(right) < 0;
        public static bool operator <=(Price left, Price right) => left.CompareTo(right) <= 0;
        public static bool operator >(Price left, Price right) => left.CompareTo(right) > 0;
        public static bool operator >=(Price left, Price right) => left.CompareTo(right) >= 0;

        public static implicit operator double(Price price) => price.Value;
        public static implicit operator Price(double value) => new Price(value);
    }
}
