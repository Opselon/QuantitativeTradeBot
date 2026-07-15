using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a unified snapshot of the C++ quantitative state, the AI's neural prediction, 
    /// and the actual trade execution outcome, persisted as training data for future models.
    /// </summary>
    public sealed class ExperienceRecord
    {
        public Guid Id { get; }
        public string Symbol { get; }
        public DateTime TimestampUtc { get; }

        // C++ Quant Features (Continuous 64-element feature state)
        public float[] MarketVectorFeatures { get; }

        // AI Model Metrics
        public string ModelVersion { get; }
        public double BuyConfidence { get; }
        public double SellConfidence { get; }
        public double RiskScore { get; }
        public string MarketRegime { get; }

        // Execution Outcome Details
        public string ExecutedAction { get; }
        public double RealizedPips { get; set; }
        public bool IsCompleted { get; set; }

        public ExperienceRecord(
            string symbol,
            float[] marketVectorFeatures,
            string modelVersion,
            double buyConfidence,
            double sellConfidence,
            double riskScore,
            string marketRegime,
            string executedAction)
        {
            Id = Guid.NewGuid();
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            TimestampUtc = DateTime.UtcNow;
            MarketVectorFeatures = marketVectorFeatures ?? new float[64];
            ModelVersion = modelVersion ?? "1.0.0";
            BuyConfidence = buyConfidence;
            SellConfidence = sellConfidence;
            RiskScore = riskScore;
            MarketRegime = marketRegime ?? "Unknown";
            ExecutedAction = executedAction ?? "WAIT";
            IsCompleted = false;
        }
    }
}