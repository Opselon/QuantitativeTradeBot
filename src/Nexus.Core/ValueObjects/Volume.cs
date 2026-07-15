using System;
using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable, thread-safe trading or market transaction volume.
    /// </summary>
    public readonly struct Volume : IEquatable<Volume>, IComparable<Volume>
    {
        public double Value { get; }

        public Volume(double value)
        {
            if (value < 0)
                throw new InvalidVolumeException($"Volume cannot be negative. Value was: {value}");
            Value = value;
        }

        public bool Equals(Volume other) => Value.Equals(other.Value);

        public override bool Equals(object? obj) => obj is Volume other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Volume other) => Value.CompareTo(other.Value);

        public override string ToString() => Value.ToString("F4");

        public static Volume Add(Volume left, Volume right) => new Volume(left.Value + right.Value);
        public static Volume Subtract(Volume left, Volume right) => new Volume(left.Value - right.Value);

        public static Volume operator +(Volume left, Volume right) => Add(left, right);
        public static Volume operator -(Volume left, Volume right) => Subtract(left, right);
        public static Volume operator *(Volume left, double scalar) => new Volume(left.Value * scalar);
        public static Volume operator /(Volume left, double scalar) => new Volume(left.Value / scalar);

        public static bool operator ==(Volume left, Volume right) => left.Equals(right);
        public static bool operator !=(Volume left, Volume right) => !left.Equals(right);
        public static bool operator <(Volume left, Volume right) => left.CompareTo(right) < 0;
        public static bool operator <=(Volume left, Volume right) => left.CompareTo(right) <= 0;
        public static bool operator >(Volume left, Volume right) => left.CompareTo(right) > 0;
        public static bool operator >=(Volume left, Volume right) => left.CompareTo(right) >= 0;

        public static implicit operator double(Volume volume) => volume.Value;
        public static implicit operator Volume(double value) => new Volume(value);
    }
}
