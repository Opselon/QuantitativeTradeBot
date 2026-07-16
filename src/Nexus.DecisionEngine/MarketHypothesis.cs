namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Represents a single competing market hypothesis with its probability, expected reward, risk, and confidence.
    /// </summary>
    public sealed class MarketHypothesis
    {
        public string Name { get; }
        public double Probability { get; }      // 0.0 to 1.0
        public double ExpectedRewardPips { get; }
        public double RiskPips { get; }
        public double Confidence { get; }       // 0.0 to 1.0

        public MarketHypothesis(string name, double probability, double expectedRewardPips, double riskPips, double confidence)
        {
            Name = name ?? string.Empty;
            Probability = probability;
            ExpectedRewardPips = expectedRewardPips;
            RiskPips = riskPips;
            Confidence = confidence;
        }

        public double GetUtility()
        {
            // Expected Utility calculation
            return (Probability * ExpectedRewardPips) - ((1.0 - Probability) * RiskPips);
        }
    }
}
