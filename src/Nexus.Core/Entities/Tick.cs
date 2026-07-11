using System;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    public readonly struct Tick : IEquatable<Tick>
    {
        public Symbol Symbol { get; }
        public DateTime Time { get; }
        public double Bid { get; }
        public double Ask { get; }
        public double Spread => Ask - Bid;

        public Tick(Symbol symbol, DateTime time, double bid, double ask)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Time = time;
            Bid = bid;
            Ask = ask;
        }

        public bool Equals(Tick other)
        {
            return Symbol.Equals(other.Symbol) && Time.Equals(other.Time) && Bid.Equals(other.Bid) && Ask.Equals(other.Ask);
        }

        public override bool Equals(object? obj)
        {
            return obj is Tick other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol, Time, Bid, Ask);
        }

        public override string ToString() => $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] {Symbol}: Bid={Bid:F5} Ask={Ask:F5} Spread={Spread:F5}";

        public static bool operator ==(Tick left, Tick right) => left.Equals(right);
        public static bool operator !=(Tick left, Tick right) => !left.Equals(right);
    }
}
