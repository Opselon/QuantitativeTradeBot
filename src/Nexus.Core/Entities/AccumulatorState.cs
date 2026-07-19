namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the running internal feature matrices, counters, and metrics maintained by the accumulator engine.
    /// </summary>
    public class AccumulatorState
    {
        public string Symbol { get; }
        public DateTime LastUpdatedUtc { get; private set; }
        public int TickCount { get; private set; }

        // Cumulative state for incrementally computing metrics
        public double SumPrices { get; private set; }
        public double SumSquaredPrices { get; private set; }
        public double LastPrice { get; private set; }
        public double HighPrice { get; private set; }
        public double LowPrice { get; private set; }

        public AccumulatorState(string symbol)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            HighPrice = double.MinValue;
            LowPrice = double.MaxValue;
        }

        public double CalculateMean() => TickCount == 0 ? 0 : SumPrices / TickCount;

        public double CalculateVariance()
        {
            if (TickCount <= 1) return 0;
            double mean = CalculateMean();
            double variance = (SumSquaredPrices / TickCount) - (mean * mean);
            return variance < 0 ? 0 : variance;
        }

        public double CalculateStandardDeviation() => Math.Sqrt(CalculateVariance());

        public void ApplyDelta(FeatureDelta delta)
        {
            LastUpdatedUtc = delta.Timestamp;
            TickCount++;
            SumPrices += delta.PriceChange;
            SumSquaredPrices += (delta.PriceChange * delta.PriceChange);
            LastPrice += delta.PriceChange;

            if (LastPrice > HighPrice) HighPrice = LastPrice;
            if (LastPrice < LowPrice) LowPrice = LastPrice;
        }
    }
}
