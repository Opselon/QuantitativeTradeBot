using Nexus.Core.ValueObjects;

namespace Nexus.MarketIntelligence.DataSources
{
    #region Order Book Models

    /// <summary>
    /// Represents a single price level in the order book.
    /// </summary>
    public readonly struct OrderBookLevel
    {
        /// <summary>
        /// The price of this level.
        /// </summary>
        public Price Price { get; }

        /// <summary>
        /// The volume available at this price level.
        /// </summary>
        public Volume Volume { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBookLevel"/> struct.
        /// </summary>
        public OrderBookLevel(Price price, Volume volume)
        {
            Price = price;
            Volume = volume;
        }
    }

    /// <summary>
    /// Represents the full snapshot of the order book for a symbol.
    /// </summary>
    public sealed class OrderBookState
    {
        /// <summary>
        /// The symbol of the order book.
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// The UTC timestamp of the snapshot.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// The list of buy order levels (bids), sorted descending by price.
        /// </summary>
        public IReadOnlyList<OrderBookLevel> Bids { get; }

        /// <summary>
        /// The list of sell order levels (asks), sorted ascending by price.
        /// </summary>
        public IReadOnlyList<OrderBookLevel> Asks { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderBookState"/> class.
        /// </summary>
        public OrderBookState(Symbol symbol, DateTime timestampUtc, IReadOnlyList<OrderBookLevel> bids, IReadOnlyList<OrderBookLevel> asks)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            TimestampUtc = timestampUtc;
            Bids = bids ?? throw new ArgumentNullException(nameof(bids));
            Asks = asks ?? throw new ArgumentNullException(nameof(asks));
        }
    }

    #endregion

    #region Economic Calendar Models

    /// <summary>
    /// Represents the importance level of an economic event.
    /// </summary>
    public enum ImportanceLevel
    {
        /// <summary>Low impact.</summary>
        Low,
        /// <summary>Medium impact.</summary>
        Medium,
        /// <summary>High impact/market-moving event.</summary>
        High
    }

    /// <summary>
    /// Represents an event on the economic calendar.
    /// </summary>
    public sealed class EconomicEvent
    {
        /// <summary>The unique identifier of the event.</summary>
        public string EventId { get; }

        /// <summary>The headline title of the event.</summary>
        public string Title { get; }

        /// <summary>The country or region code (e.g., USD, EUR).</summary>
        public string Country { get; }

        /// <summary>The UTC timestamp of the release.</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>The level of market impact expected from this release.</summary>
        public ImportanceLevel Importance { get; }

        /// <summary>The previous reported value (if any).</summary>
        public string? PreviousValue { get; }

        /// <summary>The market forecast/consensus value (if any).</summary>
        public string? ForecastValue { get; }

        /// <summary>The actual actual published value (if any).</summary>
        public string? ActualValue { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="EconomicEvent"/>.
        /// </summary>
        public EconomicEvent(string eventId, string title, string country, DateTime timestampUtc, ImportanceLevel importance, string? previousValue = null, string? forecastValue = null, string? actualValue = null)
        {
            EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Country = country ?? throw new ArgumentNullException(nameof(country));
            TimestampUtc = timestampUtc;
            Importance = importance;
            PreviousValue = previousValue;
            ForecastValue = forecastValue;
            ActualValue = actualValue;
        }
    }

    #endregion

    #region News Events Models

    /// <summary>
    /// Represents a breaking news event from financial feeds.
    /// </summary>
    public sealed class NewsEvent
    {
        /// <summary>The unique identifier of the news item.</summary>
        public string NewsId { get; }

        /// <summary>The headline of the news article.</summary>
        public string Headline { get; }

        /// <summary>The source organization name.</summary>
        public string Source { get; }

        /// <summary>The UTC timestamp when published.</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>Evaluated sentiment score between -1.0 (extremely bearish) and 1.0 (extremely bullish).</summary>
        public double SentimentScore { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NewsEvent"/>.
        /// </summary>
        public NewsEvent(string newsId, string headline, string source, DateTime timestampUtc, double sentimentScore)
        {
            NewsId = newsId ?? throw new ArgumentNullException(nameof(newsId));
            Headline = headline ?? throw new ArgumentNullException(nameof(headline));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            TimestampUtc = timestampUtc;
            SentimentScore = Math.Clamp(sentimentScore, -1.0, 1.0);
        }
    }

    #endregion

    #region Broker Metadata Models

    /// <summary>
    /// Holds high-level metadata about the broker/account connection.
    /// </summary>
    public sealed class BrokerInfo
    {
        /// <summary>The name of the broker company.</summary>
        public string BrokerName { get; }

        /// <summary>The server endpoint/cluster being connected to.</summary>
        public string ServerName { get; }

        /// <summary>Maximum leverage allowed on the account (e.g. 500 for 1:500).</summary>
        public int MaxLeverage { get; }

        /// <summary>The primary account base currency (e.g., USD).</summary>
        public string BaseCurrency { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="BrokerInfo"/>.
        /// </summary>
        public BrokerInfo(string brokerName, string serverName, int maxLeverage, string baseCurrency)
        {
            BrokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
            ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            MaxLeverage = maxLeverage;
            BaseCurrency = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
        }
    }

    /// <summary>
    /// Details structural specifications of a tradeable asset.
    /// </summary>
    public sealed class SymbolSpecification
    {
        /// <summary>The instrument symbol representation.</summary>
        public Symbol Symbol { get; }

        /// <summary>The contract size/multiplier (e.g., 100,000 for standard FX lot).</summary>
        public double ContractSize { get; }

        /// <summary>The minimum trade size allowed (e.g. 0.01).</summary>
        public Volume MinVolume { get; }

        /// <summary>The maximum trade size allowed (e.g. 100.00).</summary>
        public Volume MaxVolume { get; }

        /// <summary>The volume stepping increment (e.g. 0.01).</summary>
        public double VolumeStep { get; }

        /// <summary>The point size (value of the smallest digit, e.g. 0.00001).</summary>
        public double PointSize { get; }

        /// <summary>The number of digits after the decimal (e.g. 5).</summary>
        public int Digits { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SymbolSpecification"/>.
        /// </summary>
        public SymbolSpecification(Symbol symbol, double contractSize, Volume minVolume, Volume maxVolume, double volumeStep, double pointSize, int digits)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            ContractSize = contractSize;
            MinVolume = minVolume;
            MaxVolume = maxVolume;
            VolumeStep = volumeStep;
            PointSize = pointSize;
            Digits = digits;
        }
    }

    #endregion
}
