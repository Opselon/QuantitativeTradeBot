using Nexus.Core.AI.Enums;
using Nexus.Core.Entities;

namespace Nexus.Core.AI.Interfaces
{
    /// <summary>
    /// Defines an isolated, reusable feature engineering module.
    /// Never generates indicators inside the trainer.
    /// </summary>
    public interface IFeatureExtractor
    {
        string Name { get; }
        string Version { get; }
        FeatureType Type { get; }
        int OutputDimension { get; }
        IReadOnlyList<string> Dependencies { get; }

        /// <summary>
        /// Extracts numerical features from the operational market state.
        /// </summary>
        double[] Extract(MarketState state, IReadOnlyList<Candle> recentCandles, IReadOnlyList<Tick> recentTicks);
    }
}