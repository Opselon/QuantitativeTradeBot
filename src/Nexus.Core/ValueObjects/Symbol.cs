namespace Nexus.Core.ValueObjects
{
    public sealed class Symbol : IEquatable<Symbol>
    {
        public string Name { get; }
        public int Digits { get; }
        public double PointSize { get; }
        public double TickSize { get; }
        public double MinLot { get; }
        public double MaxLot { get; }
        public double LotStep { get; }

        public Symbol(string name, int digits = 5, double tickSize = 0.00001, double minLot = 0.01, double maxLot = 100.0, double lotStep = 0.01)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Symbol name cannot be null or empty.", nameof(name));

            Name = name.ToUpperInvariant();
            Digits = digits;
            PointSize = Math.Pow(10, -digits);
            TickSize = tickSize;
            MinLot = minLot;
            MaxLot = maxLot;
            LotStep = lotStep;
        }

        public double NormalizePrice(double price)
        {
            return Math.Round(price, Digits);
        }

        public double NormalizeLot(double lot)
        {
            if (lot < MinLot) return MinLot;
            if (lot > MaxLot) return MaxLot;

            decimal lotDec = (decimal)lot;
            decimal minLotDec = (decimal)MinLot;
            decimal lotStepDec = (decimal)LotStep;

            decimal steps = Math.Round((lotDec - minLotDec) / lotStepDec, MidpointRounding.AwayFromZero);
            decimal normalized = minLotDec + (steps * lotStepDec);

            return (double)Math.Round(normalized, 2); // Standard lot granularity
        }

        public bool Equals(Symbol? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Symbol);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString() => Name;

        public static bool operator ==(Symbol? left, Symbol? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Symbol? left, Symbol? right) => !(left == right);
    }
}
