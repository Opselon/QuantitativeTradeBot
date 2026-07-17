using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.AI.Enums;
using Nexus.Core.AI.Interfaces;
using Nexus.Core.Entities;

namespace Nexus.Application.AI.Features
{
    /// <summary>
    /// Average True Range (ATR) Extractor.
    /// Normalizes market volatility into a 0 to 1 bounded output.
    /// </summary>
    public class AtrFeatureExtractor : IFeatureExtractor
    {
        public string Name => "Normalized_ATR_14";
        public string Version => "1.0.0";
        public FeatureType Type => FeatureType.Volatility;
        public int OutputDimension => 1;
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();

        private readonly int _period;

        public AtrFeatureExtractor(int period = 14)
        {
            _period = period;
        }

        public double[] Extract(MarketState state, IReadOnlyList<Candle> recentCandles, IReadOnlyList<Tick> recentTicks)
        {
            if (recentCandles == null || recentCandles.Count <= _period)
                return new double[] { 0.0 };

            var trValues = new List<double>();

            for (int i = 1; i <= _period; i++)
            {
                var current = recentCandles[recentCandles.Count - i];
                var prev = recentCandles[recentCandles.Count - i - 1];

                double highLow = current.High.Value - current.Low.Value;
                double highClose = Math.Abs(current.High.Value - prev.Close.Value);
                double lowClose = Math.Abs(current.Low.Value - prev.Close.Value);

                double tr = Math.Max(highLow, Math.Max(highClose, lowClose));
                trValues.Add(tr);
            }

            double atr = trValues.Average();

            // Normalize assuming ATR typically doesn't exceed 5x the spread for a given interval
            // This prevents gradient explosion in the neural net
            double normalizedAtr = Math.Clamp(atr * 1000.0, 0.0, 1.0);

            return new[] { normalizedAtr };
        }
    }
}