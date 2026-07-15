using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a detailed, extended experience sample stored for offline replay and model self-learning validation.
    /// Captures the decision context, search metrics, action taken, and out-of-sample physical results.
    /// </summary>
    public sealed class ExperienceSample
    {
        public Guid Id { get; }
        public string Symbol { get; }
        public TimeframeInterval Timeframe { get; }
        public MarketState MarketStateSnapshot { get; }
        public float[] FeatureVector { get; }
        public TradeDecision Decision { get; }
        public double EntryPrice { get; }
        public double ExitPrice { get; set; }
        public double MaxDrawdown { get; set; }
        public double HoldingTimeMinutes { get; set; }
        public double OutcomeScore { get; set; } // expected value vs actual performance
        public string MistakeClassification { get; set; } // "None", "Overtrading", "BadRegimeFit", etc.
        public string MarketRegimeLabel { get; }
        public DateTime TimestampUtc { get; }

        public ExperienceSample(
            string symbol,
            TimeframeInterval timeframe,
            MarketState marketStateSnapshot,
            float[] featureVector,
            TradeDecision decision,
            double entryPrice,
            string marketRegimeLabel)
        {
            Id = Guid.NewGuid();
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Timeframe = timeframe;
            MarketStateSnapshot = marketStateSnapshot ?? throw new ArgumentNullException(nameof(marketStateSnapshot));
            FeatureVector = featureVector ?? Array.Empty<float>();
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
            EntryPrice = entryPrice;
            MarketRegimeLabel = marketRegimeLabel ?? "Unknown";
            TimestampUtc = DateTime.UtcNow;
            MistakeClassification = "None";
        }
    }
}
