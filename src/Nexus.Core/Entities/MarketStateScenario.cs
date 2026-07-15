using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a simulated, hypothetical future market state projection path (branch)
    /// generated during decision tree/search explorations.
    /// </summary>
    public sealed class MarketStateScenario
    {
        public string Symbol { get; }
        public DateTime ProjectedTimeUtc { get; }
        public double ProjectedPrice { get; }
        public double Probability { get; }
        public double Volatility { get; }
        public double DrawdownRisk { get; }
        public string PredictedRegime { get; }

        public MarketStateScenario(
            string symbol,
            DateTime projectedTimeUtc,
            double projectedPrice,
            double probability,
            double volatility,
            double drawdownRisk,
            string predictedRegime)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            ProjectedTimeUtc = projectedTimeUtc;
            ProjectedPrice = projectedPrice;
            Probability = Math.Clamp(probability, 0.0, 1.0);
            Volatility = volatility;
            DrawdownRisk = drawdownRisk;
            PredictedRegime = predictedRegime ?? "Unknown";
        }
    }
}
