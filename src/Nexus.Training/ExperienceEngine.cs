using System;
using Nexus.Core.Entities;

namespace Nexus.Training
{
    /// <summary>
    /// Converts every live or simulated market decision into a rich structured ExperienceSample ready for replay training.
    /// </summary>
    public sealed class ExperienceEngine
    {
        /// <summary>
        /// Translates raw decision details and market metrics into a learning experience snapshot.
        /// </summary>
        public ExperienceSample CreateExperience(
            string symbol,
            TimeframeInterval timeframe,
            MarketState marketState,
            float[] featureVector,
            TradeDecision decision,
            double entryPrice,
            double confidence,
            string reasoningMetadata,
            double risk,
            double reward)
        {
            if (marketState == null) throw new ArgumentNullException(nameof(marketState));
            if (decision == null) throw new ArgumentNullException(nameof(decision));

            var sample = new ExperienceSample(
                symbol,
                timeframe,
                marketState,
                featureVector,
                decision,
                entryPrice,
                marketState.MarketRegime
            )
            {
                Confidence = Math.Clamp(confidence, 0.0, 1.0),
                ReasoningMetadata = reasoningMetadata ?? "Automated quantitative decision",
                Risk = Math.Max(0.0, risk),
                Reward = Math.Max(0.0, reward)
            };

            return sample;
        }

        /// <summary>
        /// Updates a previously collected experience sample with physical market results after trade conclusion.
        /// </summary>
        public void FinalizeExperienceOutcome(
            ExperienceSample sample,
            double exitPrice,
            double maxDrawdown,
            double holdingTimeMinutes,
            double resultProfitLoss,
            string mistakeClassification)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));

            sample.ExitPrice = exitPrice;
            sample.MaxDrawdown = Math.Max(0.0, maxDrawdown);
            sample.HoldingTimeMinutes = Math.Max(0.0, holdingTimeMinutes);
            sample.Result = resultProfitLoss;
            sample.MistakeClassification = string.IsNullOrWhiteSpace(mistakeClassification) ? "None" : mistakeClassification;
        }
    }
}
