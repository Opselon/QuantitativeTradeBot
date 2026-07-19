using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using System.Collections.Concurrent;

namespace Nexus.MarketIntelligence.Aggregation
{
    /// <summary>
    /// Thread-safe tick aggregator that compiles high-frequency streaming tick updates into standard candles.
    /// Supports multi-timeframe candle construction.
    /// </summary>
    public sealed class TickAggregator
    {
        private readonly ConcurrentDictionary<string, ActiveCandleState> _activeStates =
            new ConcurrentDictionary<string, ActiveCandleState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Event fired when a candle period completes and is finalized.
        /// </summary>
        public event EventHandler<Candle>? CandleCompleted;

        /// <summary>
        /// Ingests a new tick and updates the active building candle.
        /// Returns the completed candle if the tick rolls over the timeframe boundary, otherwise null.
        /// </summary>
        public Candle? IngestTick(Tick tick, Timeframe timeframe)
        {
            if (tick.Symbol == null)
                throw new ArgumentException("Tick symbol cannot be null.", nameof(tick));

            string key = $"{tick.Symbol.Name}_{timeframe.Type}";

            // Fetch or create the active state for this symbol/timeframe combo
            var state = _activeStates.GetOrAdd(key, _ => new ActiveCandleState(tick.Symbol, timeframe));

            Candle? completedCandle = null;

            lock (state)
            {
                DateTime roundedTime = RoundTimestamp(tick.Time, timeframe.Duration);

                if (state.CurrentCandle == null)
                {
                    // Initialize the first candle for this series
                    var price = new Price(tick.Bid);
                    var volume = new Volume(1.0); // Tick count or volume increment
                    state.CurrentCandle = new Candle(state.Symbol, state.Timeframe, roundedTime, price, price, price, price, volume);
                }
                else if (roundedTime > state.CurrentCandle.Timestamp)
                {
                    // Roll over! The incoming tick belongs to a new timeframe candle interval.
                    completedCandle = state.CurrentCandle;

                    // Initialize the new active candle
                    var price = new Price(tick.Bid);
                    var volume = new Volume(1.0);
                    state.CurrentCandle = new Candle(state.Symbol, state.Timeframe, roundedTime, price, price, price, price, volume);

                    // Raise event
                    OnCandleCompleted(completedCandle);
                }
                else
                {
                    // Update active candle with the price and volume increment
                    var price = new Price(tick.Bid);
                    var volume = new Volume(1.0);
                    state.CurrentCandle.Update(price, volume);
                }
            }

            return completedCandle;
        }

        /// <summary>
        /// Forcefully closes and flushes the currently building candle for a symbol and timeframe, returning it.
        /// </summary>
        public Candle? Flush(Symbol symbol, Timeframe timeframe)
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));

            string key = $"{symbol.Name}_{timeframe.Type}";
            if (_activeStates.TryRemove(key, out var state))
            {
                lock (state)
                {
                    return state.CurrentCandle;
                }
            }
            return null;
        }

        #region Helper Methods

        private static DateTime RoundTimestamp(DateTime dt, TimeSpan interval)
        {
            long ticks = dt.Ticks / interval.Ticks;
            return new DateTime(ticks * interval.Ticks, DateTimeKind.Utc);
        }

        private void OnCandleCompleted(Candle candle)
        {
            CandleCompleted?.Invoke(this, candle);
        }

        #endregion

        #region Inner Active State Representation

        private sealed class ActiveCandleState
        {
            public Symbol Symbol { get; }
            public Timeframe Timeframe { get; }
            public Candle? CurrentCandle { get; set; }

            public ActiveCandleState(Symbol symbol, Timeframe timeframe)
            {
                Symbol = symbol;
                Timeframe = timeframe;
            }
        }

        #endregion
    }
}
