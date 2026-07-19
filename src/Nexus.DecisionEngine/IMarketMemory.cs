namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Abstraction for retrieving historical market situations, pattern frequencies, and past outcomes.
    /// </summary>
    public interface IMarketMemory
    {
        Task<double> GetSimilarSituationsSuccessRateAsync(string symbol, double[] currentFeatures, CancellationToken ct);
        Task<int> GetPatternFrequencyAsync(string symbol, string patternName, CancellationToken ct);
    }
}
