using System;

namespace Nexus.Core.ValueObjects
{
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency = "USD")
        {
            Amount = amount;
            Currency = currency?.ToUpperInvariant() ?? "USD";
        }

        public static Money Zero(string currency = "USD") => new Money(0m, currency);

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal multiplier)
        {
            return new Money(Amount * multiplier, Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}");
        }

        public bool Equals(Money other)
        {
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj)
        {
            return obj is Money other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public int CompareTo(Money other)
        {
            EnsureSameCurrency(other);
            return Amount.CompareTo(other.Amount);
        }

        public override string ToString() => $"{Amount:F4} {Currency}";

        public static Money operator +(Money left, Money right) => left.Add(right);
        public static Money operator -(Money left, Money right) => left.Subtract(right);
        public static Money operator *(Money left, decimal multiplier) => left.Multiply(multiplier);
        public static Money operator *(decimal multiplier, Money right) => right.Multiply(multiplier);

        public static bool operator ==(Money left, Money right) => left.Equals(right);
        public static bool operator !=(Money left, Money right) => !left.Equals(right);
        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    }
}
