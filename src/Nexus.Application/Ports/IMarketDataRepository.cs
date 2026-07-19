using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Ports
{
    public interface IMarketDataRepository
    {

        /// <summary>
        /// Retrieves historical OHLCV candles for a given symbol and timeframe.
        /// Used by the Dataset Generator for offline ML training.
        /// </summary>
        Task<IReadOnlyList<Candle>> GetCandlesAsync(
            string symbol,
            string timeframe,
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default);

        ValueTask AppendTickAsync(Tick tick, CancellationToken cancellationToken = default);
        Task AppendTicksAsync(IReadOnlyCollection<Tick> ticks, CancellationToken cancellationToken = default);
        Task AppendBarsAsync(IReadOnlyCollection<Bar> bars, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Tick> StreamTicksAsync(Symbol symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Bar>> GetBarsAsync(Symbol symbol, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    }
}
