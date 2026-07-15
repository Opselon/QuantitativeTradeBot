using System;
using Nexus.Core.Exceptions;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents standard OHLCV price bar data for a financial instrument over a specified timeframe.
    /// Performs self-validation to guarantee structural correctness of the price boundaries.
    /// </summary>
    public sealed class Candle
    {
        public Symbol Symbol { get; }
        public Timeframe Timeframe { get; }
        public DateTime Timestamp { get; }
        public Price Open { get; private set; }
        public Price High { get; private set; }
        public Price Low { get; private set; }
        public Price Close { get; private set; }
        public Volume Volume { get; private set; }

        public Candle(Symbol symbol, Timeframe timeframe, DateTime timestamp, Price open, Price high, Price low, Price close, Volume volume)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Timeframe = timeframe;
            Timestamp = timestamp;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;

            Validate();
        }

        /// <summary>
        /// Validates that the candle price ranges satisfy standard structural constraints.
        /// </summary>
        private void Validate()
        {
            if (High < Low)
                throw new InvalidPriceException($"Candle High ({High}) cannot be less than Low ({Low}).");

            if (Open < Low)
                throw new InvalidPriceException($"Candle Open ({Open}) cannot be less than Low ({Low}).");

            if (Close < Low)
                throw new InvalidPriceException($"Candle Close ({Close}) cannot be less than Low ({Low}).");

            if (High < Open)
                throw new InvalidPriceException($"Candle High ({High}) cannot be less than Open ({Open}).");

            if (High < Close)
                throw new InvalidPriceException($"Candle High ({High}) cannot be less than Close ({Close}).");
        }

        /// <summary>
        /// Update the candle with a new price point, modifying High, Low, Close, and accumulating Volume.
        /// </summary>
        public void Update(Price price, Volume volumeIncrement)
        {
            if (price > High) High = price;
            if (price < Low) Low = price;
            Close = price;
            Volume += volumeIncrement;
            Validate();
        }

        public override string ToString() =>
            $"{Symbol} ({Timeframe}) {Timestamp:yyyy-MM-dd HH:mm:ss} UTC | O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}";
    }
}
