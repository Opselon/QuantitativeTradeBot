using System;

namespace Nexus.MarketIntelligence.DataSources
{
    /// <summary>
    /// Evaluates natural language sentiment of financial headlines using deterministic heuristics.
    /// </summary>
    public sealed class NewsSentimentEngine : INewsSentimentEngine
    {
        /// <summary>
        /// Analyzes natural language sentiment of financial headlines using deterministic heuristics.
        /// </summary>
        public double AnalyzeSentiment(string headline)
        {
            if (string.IsNullOrWhiteSpace(headline)) return 0.0;

            var text = headline.ToLowerInvariant();
            double score = 0.0;

            if (text.Contains("bullish") || text.Contains("surge") || text.Contains("growth") || text.Contains("hawkish") || text.Contains("positive") || text.Contains("gain") || text.Contains("increase") || text.Contains("rally"))
                score += 0.5;

            if (text.Contains("bearish") || text.Contains("drop") || text.Contains("plunge") || text.Contains("dovish") || text.Contains("negative") || text.Contains("loss") || text.Contains("decrease") || text.Contains("recession") || text.Contains("decline"))
                score -= 0.5;

            return Math.Clamp(score, -1.0, 1.0);
        }
    }
}
