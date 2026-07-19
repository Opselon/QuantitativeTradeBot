using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Service contract for generating and comparing competing market hypotheses.
    /// </summary>
    public interface IMarketHypothesisEngine
    {
        List<MarketHypothesis> GenerateHypotheses(MarketState marketState, double neuralBuyConfidence, double neuralSellConfidence);
    }

    public sealed class MarketHypothesisEngine : IMarketHypothesisEngine
    {
        public List<MarketHypothesis> GenerateHypotheses(MarketState marketState, double neuralBuyConfidence, double neuralSellConfidence)
        {
            if (marketState == null)
            {
                throw new ArgumentNullException(nameof(marketState));
            }

            var hypotheses = new List<MarketHypothesis>();

            // 1. Trend Continuation Hypothesis
            double continuationProb = 0.4;
            if (marketState.MarketRegime.Contains("Trend") || marketState.MarketRegime.Contains("Continuation"))
            {
                continuationProb += 0.2;
            }
            continuationProb += Math.Max(neuralBuyConfidence, neuralSellConfidence) * 0.2;
            continuationProb = Math.Clamp(continuationProb, 0.1, 0.9);

            double continuationReward = 30.0 + (marketState.Momentum * 10.0);
            double continuationRisk = 15.0;
            double continuationConf = 0.5 + (Math.Max(neuralBuyConfidence, neuralSellConfidence) * 0.4);
            hypotheses.Add(new MarketHypothesis("Trend Continuation", continuationProb, continuationReward, continuationRisk, continuationConf));

            // 2. Trend Reversal Hypothesis
            double reversalProb = 0.3;
            if (marketState.MarketRegime.Contains("Range") || marketState.MarketRegime.Contains("Reversal") || marketState.Volatility > 0.6)
            {
                reversalProb += 0.2;
            }
            reversalProb = Math.Clamp(reversalProb, 0.1, 0.8);

            double reversalReward = 45.0;
            double reversalRisk = 20.0;
            double reversalConf = 0.4 + (marketState.Volatility * 0.3);
            hypotheses.Add(new MarketHypothesis("Trend Reversal", reversalProb, reversalReward, reversalRisk, reversalConf));

            // 3. Sideways / Range-Bound Hypothesis
            double sidewaysProb = 1.0 - continuationProb - reversalProb;
            sidewaysProb = Math.Clamp(sidewaysProb, 0.05, 0.7);

            double sidewaysReward = 15.0;
            double sidewaysRisk = 10.0;
            double sidewaysConf = 0.6 - (marketState.Volatility * 0.4);
            hypotheses.Add(new MarketHypothesis("Sideways / Mean Reversion", sidewaysProb, sidewaysReward, sidewaysRisk, sidewaysConf));

            return hypotheses;
        }
    }
}
