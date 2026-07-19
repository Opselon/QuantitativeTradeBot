using Nexus.Core.Exceptions;

namespace Nexus.Core.ValueObjects
{
    /// <summary>
    /// Represents a validated, immutable, thread-safe monetary or capital risk amount.
    /// </summary>
    public readonly struct RiskAmount : IEquatable<RiskAmount>, IComparable<RiskAmount>
    {
        public Money Amount { get; }

        public RiskAmount(Money amount)
        {
            if (amount.Amount < 0)
                throw new InvalidRiskException("Risk amount cannot be negative.");
            Amount = amount;
        }

        public RiskAmount(decimal value, string currency = "USD")
            : this(new Money(value, currency))
        {
        }

        public bool Equals(RiskAmount other) => Amount.Equals(other.Amount);

        public override bool Equals(object? obj) => obj is RiskAmount other && Equals(other);

        public override int GetHashCode() => Amount.GetHashCode();

        public int CompareTo(RiskAmount other) => Amount.CompareTo(other.Amount);

        public override string ToString() => $"Risk: {Amount}";

        public static RiskAmount operator +(RiskAmount left, RiskAmount right) => new RiskAmount(left.Amount + right.Amount);
        public static RiskAmount operator -(RiskAmount left, RiskAmount right) => new RiskAmount(left.Amount - right.Amount);

        public static bool operator ==(RiskAmount left, RiskAmount right) => left.Equals(right);
        public static bool operator !=(RiskAmount left, RiskAmount right) => !left.Equals(right);
        public static bool operator <(RiskAmount left, RiskAmount right) => left.CompareTo(right) < 0;
        public static bool operator <=(RiskAmount left, RiskAmount right) => left.CompareTo(right) <= 0;
        public static bool operator >(RiskAmount left, RiskAmount right) => left.CompareTo(right) > 0;
        public static bool operator >=(RiskAmount left, RiskAmount right) => left.CompareTo(right) >= 0;

        public static implicit operator Money(RiskAmount risk) => risk.Amount;
        public static implicit operator RiskAmount(Money money) => new RiskAmount(money);
    }
}
