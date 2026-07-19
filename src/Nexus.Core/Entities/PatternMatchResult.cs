namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a matching historical market pattern retrieved from pattern memory.
    /// </summary>
    public class PatternMatchResult
    {
        public MarketVector Vector { get; }
        public string Conditions { get; }
        public string Outcome { get; }
        public double Performance { get; }
        public double Similarity { get; }

        public PatternMatchResult(MarketVector vector, string conditions, string outcome, double performance, double similarity)
        {
            Vector = vector;
            Conditions = conditions;
            Outcome = outcome;
            Performance = performance;
            Similarity = similarity;
        }

        public override string ToString()
        {
            return $"Similarity={Similarity:P1}, Outcome={Outcome}, Performance={Performance:F1}";
        }
    }
}
