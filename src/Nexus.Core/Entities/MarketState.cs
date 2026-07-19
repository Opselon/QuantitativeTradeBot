using Nexus.Core.Enums;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the platform's live internal understanding of the market at any given instant.
    /// Supports both primitive representation for database/adapter backward-compatibility and rich Value Objects.
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

        #region Value Object properties

        public Symbol SymbolObj => new Symbol(Symbol);

        public MarketRegime Regime => Enum.TryParse<MarketRegime>(MarketRegime, out var parsed) ? parsed : Enums.MarketRegime.Unknown;

        public Percentage VolatilityPct => new Percentage(Volatility);

        public Percentage ProbabilityPct => new Percentage(Probability);

        #endregion

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

        public MarketState(
            Symbol symbol,
            DateTime lastUpdatedUtc,
            Percentage volatility,
            double momentum,
            double liquidity,
            double priceStructure,
            Percentage probability,
            double risk,
            double currencyStrength,
            MarketRegime marketRegime)
            : this(
                  symbol?.Name ?? throw new ArgumentNullException(nameof(symbol)),
                  lastUpdatedUtc,
                  volatility.Value,
                  momentum,
                  liquidity,
                  priceStructure,
                  probability.Value,
                  risk,
                  currencyStrength,
                  marketRegime.ToString())
        {
        }

        public override string ToString()
        {
            return $"State for {Symbol} at {LastUpdatedUtc:HH:mm:ss}: Vol={Volatility:F2}, Mom={Momentum:F2}, Regime={MarketRegime}";
        }
    }
}
