using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class PlaceOrderRequest
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; }

        [JsonPropertyName("side")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BridgeOrderSide Side { get; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; }

        [JsonPropertyName("stopLoss")]
        public decimal? StopLoss { get; }

        [JsonPropertyName("takeProfit")]
        public decimal? TakeProfit { get; }

        [JsonPropertyName("comment")]
        public string? Comment { get; }

        [JsonPropertyName("clientCorrelationId")]
        public string? ClientCorrelationId { get; }

        [JsonConstructor]
        public PlaceOrderRequest(
            string symbol,
            BridgeOrderSide side,
            decimal volume,
            decimal? stopLoss = null,
            decimal? takeProfit = null,
            string? comment = null,
            string? clientCorrelationId = null)
        {
            Symbol = symbol;
            Side = side;
            Volume = volume;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Comment = comment;
            ClientCorrelationId = clientCorrelationId;
        }
    }
}
