using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable, thread-safe trading market session (e.g., London, New York, Asian).
    /// </summary>
    public sealed class MarketSession : IEquatable<MarketSession>
    {
        public string Name { get; }
        public TimeSpan StartTimeUtc { get; }
        public TimeSpan EndTimeUtc { get; }

        public MarketSession(string name, TimeSpan startTimeUtc, TimeSpan endTimeUtc)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Market session name cannot be null or empty.");

            if (startTimeUtc < TimeSpan.Zero || startTimeUtc >= TimeSpan.FromDays(1))
                throw new DomainException("Start time must be within a single day (00:00 to 23:59).");

            if (endTimeUtc < TimeSpan.Zero || endTimeUtc >= TimeSpan.FromDays(1))
                throw new DomainException("End time must be within a single day (00:00 to 23:59).");

            Name = name;
            StartTimeUtc = startTimeUtc;
            EndTimeUtc = endTimeUtc;
        }

        /// <summary>
        /// Checks if a given UTC date-time falls within this market session.
        /// </summary>
        public bool IsActive(DateTime utcTime)
        {
            TimeSpan timeOfDay = utcTime.TimeOfDay;

            if (StartTimeUtc <= EndTimeUtc)
            {
                // Standard intraday session (e.g., 08:00 to 16:00)
                return timeOfDay >= StartTimeUtc && timeOfDay <= EndTimeUtc;
            }
            else
            {
                // Overnight session (e.g., 22:00 to 06:00)
                return timeOfDay >= StartTimeUtc || timeOfDay <= EndTimeUtc;
            }
        }

        public bool Equals(MarketSession? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                   StartTimeUtc == other.StartTimeUtc &&
                   EndTimeUtc == other.EndTimeUtc;
        }

        public override bool Equals(object? obj) => Equals(obj as MarketSession);

        public override int GetHashCode() => HashCode.Combine(Name.ToUpperInvariant(), StartTimeUtc, EndTimeUtc);

        public override string ToString() => $"{Name} ({StartTimeUtc:hh\\:mm} - {EndTimeUtc:hh\\:mm} UTC)";
    }
}
