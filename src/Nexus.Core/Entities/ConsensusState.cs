namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the high-level consolidated consensus state derived from analyzing separate timeframes.
    /// Higher timeframes dictate direction/bias, while lower timeframes specify entry and exit timing.
    /// </summary>
    public sealed class ConsensusState
    {
        public TrendDirection DominantBias { get; }
        public double BiasStrength { get; } // 0.0 to 1.0
        public bool EntryTriggered { get; }
        public double OverallConfidence { get; } // 0.0 to 1.0
        public string ConsensusSummary { get; }
        public DateTime GeneratedAtUtc { get; }

        public List<MultiTimeframeSignal> Signals { get; }

        public ConsensusState(
            TrendDirection dominantBias,
            double biasStrength,
            bool entryTriggered,
            double overallConfidence,
            string consensusSummary,
            List<MultiTimeframeSignal> signals,
            DateTime generatedAtUtc)
        {
            DominantBias = dominantBias;
            BiasStrength = Math.Clamp(biasStrength, 0.0, 1.0);
            EntryTriggered = entryTriggered;
            OverallConfidence = Math.Clamp(overallConfidence, 0.0, 1.0);
            ConsensusSummary = consensusSummary ?? string.Empty;
            Signals = signals ?? new List<MultiTimeframeSignal>();
            GeneratedAtUtc = generatedAtUtc;
        }
    }
}
