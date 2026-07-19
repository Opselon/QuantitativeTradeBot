namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a high-performance, zero-allocation, fixed-layout normalized numerical vector
    /// optimized for native processing and neural model inference.
    /// </summary>
    public readonly struct MarketVector : IEquatable<MarketVector>
    {
        public double PriceStructure { get; }
        public double TrendState { get; }
        public double Momentum { get; }
        public double Volatility { get; }
        public double VolumePressure { get; }
        public double Liquidity { get; }
        public double UsdStrength { get; }
        public double SessionState { get; }
        public double MarketRegime { get; }
        public double RiskState { get; }

        public MarketVector(
            double priceStructure,
            double trendState,
            double momentum,
            double volatility,
            double volumePressure,
            double liquidity,
            double usdStrength,
            double sessionState,
            double marketRegime,
            double riskState)
        {
            PriceStructure = priceStructure;
            TrendState = trendState;
            Momentum = momentum;
            Volatility = volatility;
            VolumePressure = volumePressure;
            Liquidity = liquidity;
            UsdStrength = usdStrength;
            SessionState = sessionState;
            MarketRegime = marketRegime;
            RiskState = riskState;
        }

        /// <summary>
        /// Converts the fixed-layout vector into a float array optimized for ONNX neural model input.
        /// </summary>
        public float[] ToFloatArray()
        {
            return new float[]
            {
                (float)PriceStructure,
                (float)TrendState,
                (float)Momentum,
                (float)Volatility,
                (float)VolumePressure,
                (float)Liquidity,
                (float)UsdStrength,
                (float)SessionState,
                (float)MarketRegime,
                (float)RiskState
            };
        }

        public bool Equals(MarketVector other)
        {
            return PriceStructure.Equals(other.PriceStructure) &&
                   TrendState.Equals(other.TrendState) &&
                   Momentum.Equals(other.Momentum) &&
                   Volatility.Equals(other.Volatility) &&
                   VolumePressure.Equals(other.VolumePressure) &&
                   Liquidity.Equals(other.Liquidity) &&
                   UsdStrength.Equals(other.UsdStrength) &&
                   SessionState.Equals(other.SessionState) &&
                   MarketRegime.Equals(other.MarketRegime) &&
                   RiskState.Equals(other.RiskState);
        }

        public override bool Equals(object? obj)
        {
            return obj is MarketVector other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(PriceStructure);
            hash.Add(TrendState);
            hash.Add(Momentum);
            hash.Add(Volatility);
            hash.Add(VolumePressure);
            hash.Add(Liquidity);
            hash.Add(UsdStrength);
            hash.Add(SessionState);
            hash.Add(MarketRegime);
            hash.Add(RiskState);
            return hash.ToHashCode();
        }

        public static bool operator ==(MarketVector left, MarketVector right) => left.Equals(right);
        public static bool operator !=(MarketVector left, MarketVector right) => !left.Equals(right);

        public override string ToString()
        {
            return $"[Struct={PriceStructure:F2}, Trend={TrendState:F2}, Mom={Momentum:F2}, Vol={Volatility:F2}, UsdStr={UsdStrength:F2}, Regime={MarketRegime:F2}]";
        }
    }
}
