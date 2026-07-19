using Nexus.Core.AI.Enums;
using Nexus.Core.AI.Interfaces;
using Nexus.Core.Entities;

namespace Nexus.Application.AI.Features
{
    /// <summary>
    /// Computes Log-Return Momentum over a specific window.
    /// Bounded typically between -1 and 1.
    /// </summary>
    public class MomentumFeatureExtractor : IFeatureExtractor
    {
        public string Name => "LogReturn_Momentum_20";
        public string Version => "1.0.0";
        public FeatureType Type => FeatureType.Momentum;
        public int OutputDimension => 1;
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();

        private readonly int _lookback;

        public MomentumFeatureExtractor(int lookback = 20)
        {
            _lookback = lookback;
        }

        public double[] Extract(MarketState state, IReadOnlyList<Candle> recentCandles, IReadOnlyList<Tick> recentTicks)
        {
            if (recentCandles == null || recentCandles.Count <= _lookback)
                return new double[] { 0.0 };

            double currentPrice = recentCandles[^1].Close.Value;
            double oldPrice = recentCandles[recentCandles.Count - _lookback - 1].Close.Value;

            if (oldPrice <= 0) return new double[] { 0.0 };

            double logReturn = Math.Log(currentPrice / oldPrice);

            // Normalize (Standardize using empirical observation of max moves)
            double normalized = Math.Clamp(logReturn * 50.0, -1.0, 1.0);

            return new[] { normalized };
        }
    }
}