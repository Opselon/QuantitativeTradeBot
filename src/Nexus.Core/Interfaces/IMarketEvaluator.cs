using System.Collections.Generic;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Analyzes raw chart data and structural characteristics to classify current market regimes and states.
    /// </summary>
    public interface IMarketEvaluator
    {
        /// <summary>
        /// Analyzes a stream of candles to determine the current live market state.
        /// </summary>
        /// <param name="symbol">The target symbol under evaluation.</param>
        /// <param name="timeframe">The evaluation timeframe.</param>
        /// <param name="candles">The collection of recent historical price bars.</param>
        /// <returns>A structured MarketState object representing current volatility, trend, and regime.</returns>
        MarketState Evaluate(Symbol symbol, Timeframe timeframe, IEnumerable<Candle> candles);
    }
}
