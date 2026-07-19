using Nexus.Core.Enums;
using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable timeframe for chart analysis, avoiding primitive obsession.
    /// </summary>
    public readonly struct Timeframe : IEquatable<Timeframe>, IComparable<Timeframe>
    {
        public TimeframeType Type { get; }

        public Timeframe(TimeframeType type)
        {
            if (!Enum.IsDefined(typeof(TimeframeType), type))
                throw new DomainException($"Invalid timeframe type: {type}");
            Type = type;
        }

        public Timeframe(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Timeframe name cannot be null or empty.");

            if (!Enum.TryParse(name, true, out TimeframeType parsedType))
                throw new DomainException($"Unknown timeframe name: {name}");

            Type = parsedType;
        }

        public TimeSpan Duration => Type switch
        {
            TimeframeType.M1 => TimeSpan.FromMinutes(1),
            TimeframeType.M5 => TimeSpan.FromMinutes(5),
            TimeframeType.M15 => TimeSpan.FromMinutes(15),
            TimeframeType.M30 => TimeSpan.FromMinutes(30),
            TimeframeType.H1 => TimeSpan.FromHours(1),
            TimeframeType.H4 => TimeSpan.FromHours(4),
            TimeframeType.D1 => TimeSpan.FromDays(1),
            _ => throw new NotImplementedException($"Duration for {Type} is not implemented.")
        };

        public bool Equals(Timeframe other) => Type == other.Type;

        public override bool Equals(object? obj) => obj is Timeframe other && Equals(other);

        public override int GetHashCode() => Type.GetHashCode();

        public int CompareTo(Timeframe other) => Type.CompareTo(other.Type);

        public override string ToString() => Type.ToString();

        public static bool operator ==(Timeframe left, Timeframe right) => left.Equals(right);
        public static bool operator !=(Timeframe left, Timeframe right) => !left.Equals(right);
        public static bool operator <(Timeframe left, Timeframe right) => left.CompareTo(right) < 0;
        public static bool operator <=(Timeframe left, Timeframe right) => left.CompareTo(right) <= 0;
        public static bool operator >(Timeframe left, Timeframe right) => left.CompareTo(right) > 0;
        public static bool operator >=(Timeframe left, Timeframe right) => left.CompareTo(right) >= 0;

        public static implicit operator TimeframeType(Timeframe timeframe) => timeframe.Type;
        public static implicit operator Timeframe(TimeframeType type) => new Timeframe(type);
    }
}
