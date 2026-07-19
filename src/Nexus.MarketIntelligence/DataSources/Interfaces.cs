using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.MarketIntelligence.DataSources
{
    /// <summary>
    /// Stream and fetch live pricing tick updates.
    /// </summary>
    public interface ITickStreamSource
    {
        /// <summary>
        /// Subscribes to real-time tick updates for a specific symbol.
        /// </summary>
        IAsyncEnumerable<Tick> StreamTicksAsync(Symbol symbol, CancellationToken ct = default);

        /// <summary>
        /// Retrieves the most recent tick for the given symbol.
        /// </summary>
        Task<Tick> GetLatestTickAsync(Symbol symbol, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for OHLC bar/candle history.
    /// </summary>
    public interface IOhlcBarSource
    {
        /// <summary>
        /// Retrieves historical candles for a specific symbol and timeframe.
        /// </summary>
        Task<IReadOnlyList<Candle>> GetHistoricalBarsAsync(Symbol symbol, Timeframe timeframe, int count, CancellationToken ct = default);

        /// <summary>
        /// Retrieves the latest complete candle for a symbol and timeframe.
        /// </summary>
        Task<Candle> GetLatestBarAsync(Symbol symbol, Timeframe timeframe, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for measuring market volume.
    /// </summary>
    public interface IVolumeSource
    {
        /// <summary>
        /// Retrieves the total volume traded for a symbol over a specific duration back from now.
        /// </summary>
        Task<double> GetTotalVolumeAsync(Symbol symbol, TimeSpan duration, CancellationToken ct = default);

        /// <summary>
        /// Retrieves average volume over a given number of periods.
        /// </summary>
        Task<double> GetAverageVolumeAsync(Symbol symbol, int periods, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for analyzing buy/sell spreads.
    /// </summary>
    public interface ISpreadSource
    {
        /// <summary>
        /// Gets the current spread in points/pips.
        /// </summary>
        Task<double> GetCurrentSpreadAsync(Symbol symbol, CancellationToken ct = default);

        /// <summary>
        /// Gets the average spread over a specified window.
        /// </summary>
        Task<double> GetAverageSpreadAsync(Symbol symbol, int periods, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for order book/depth of market (DOM) updates.
    /// </summary>
    public interface IOrderBookSource
    {
        /// <summary>
        /// Gets the current order book depth snapshot.
        /// </summary>
        Task<OrderBookState> GetOrderBookAsync(Symbol symbol, int depth = 10, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for tracking high, medium, and low impact economic data releases.
    /// </summary>
    public interface IEconomicCalendarSource
    {
        /// <summary>
        /// Retrieves upcoming scheduled calendar releases.
        /// </summary>
        Task<IReadOnlyList<EconomicEvent>> GetUpcomingEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for crawling and ingesting breaking financial news articles.
    /// </summary>
    public interface INewsEventSource
    {
        /// <summary>
        /// Retrieves latest news headlines and sentiment ratings.
        /// </summary>
        Task<IReadOnlyList<NewsEvent>> GetLatestNewsAsync(int limit = 50, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for detecting active global trading session intervals.
    /// </summary>
    public interface ITradingSessionSource
    {
        /// <summary>
        /// Retrieves the currently active trading session.
        /// </summary>
        Task<MarketSession> GetCurrentSessionAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks if a named session (e.g. "London", "New York") is currently active.
        /// </summary>
        Task<bool> IsSessionActiveAsync(string sessionName, DateTime timeUtc, CancellationToken ct = default);
    }

    /// <summary>
    /// Source for broker metadata and instrument configurations.
    /// </summary>
    public interface IBrokerMetadataSource
    {
        /// <summary>
        /// Gets current broker connection attributes.
        /// </summary>
        Task<BrokerInfo> GetBrokerInfoAsync(CancellationToken ct = default);

        /// <summary>
        /// Retrieves symbol contract sizes and volume stepping restrictions.
        /// </summary>
        Task<SymbolSpecification> GetSymbolSpecificationAsync(Symbol symbol, CancellationToken ct = default);
    }
}
