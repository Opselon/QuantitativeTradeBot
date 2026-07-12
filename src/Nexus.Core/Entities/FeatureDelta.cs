using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a discrete market event or delta that triggers incremental state updates rather than full recalculations.
    /// </summary>
    public readonly struct FeatureDelta
    {
        public string Symbol { get; }
        public DateTime Timestamp { get; }
        public double PriceChange { get; }
        public double VolumeDelta { get; }

        public FeatureDelta(string symbol, DateTime timestamp, double priceChange, double volumeDelta)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Timestamp = timestamp;
            PriceChange = priceChange;
            VolumeDelta = volumeDelta;
        }

        public override string ToString()
        {
            return $"{Symbol} Delta: PriceChange={PriceChange:F5}, VolChange={VolumeDelta:F1}";
        }
    }
}
