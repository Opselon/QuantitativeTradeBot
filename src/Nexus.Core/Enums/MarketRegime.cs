namespace Nexus.Core.Enums
{
    /// <summary>
    /// Represents the structural regime classification of the current market state.
    /// </summary>
    public enum MarketRegime
    {
        TrendingBullish,
        TrendingBearish,
        MeanReverting,
        HighVolatility,
        LowVolatility,
        Unknown
    }
}
