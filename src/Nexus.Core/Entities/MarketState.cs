using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the platform's live internal understanding of the market at any given instant.
    /// </summary>
    public class MarketState
    {
        public string Symbol { get; }
        public DateTime LastUpdatedUtc { get; }
        public double Volatility { get; }
        public double Momentum { get; }
        public double Liquidity { get; }
        public double PriceStructure { get; }
        public double Probability { get; }
        public double Risk { get; }
        public double CurrencyStrength { get; }
        public string MarketRegime { get; }

        public MarketState(
            string symbol,
            DateTime lastUpdatedUtc,
            double volatility,
            double momentum,
            double liquidity,
            double priceStructure,
            double probability,
            double risk,
            double currencyStrength,
            string marketRegime)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            LastUpdatedUtc = lastUpdatedUtc;
            Volatility = volatility;
            Momentum = momentum;
            Liquidity = liquidity;
            PriceStructure = priceStructure;
            Probability = probability;
            Risk = risk;
            CurrencyStrength = currencyStrength;
            MarketRegime = marketRegime ?? "Unknown";
        }

        public override string ToString()
        {
            return $"State for {Symbol} at {LastUpdatedUtc:HH:mm:ss}: Vol={Volatility:F2}, Mom={Momentum:F2}, Regime={MarketRegime}";
        }
    }
}
