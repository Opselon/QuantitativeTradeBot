namespace Nexus.MarketIntelligence.DataSources
{
    /// <summary>
    /// Service engine interface for evaluating natural language sentiment of financial news.
    /// </summary>
    public interface INewsSentimentEngine
    {
        /// <summary>
        /// Analyzes a headline and returns a score between -1.0 (extremely bearish) and 1.0 (extremely bullish).
        /// </summary>
        double AnalyzeSentiment(string headline);
    }
}
