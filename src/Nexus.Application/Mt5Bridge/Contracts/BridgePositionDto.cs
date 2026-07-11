using System;
using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class BridgePositionDto
    {
        [JsonPropertyName("ticket")]
        public long Ticket { get; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; }

        [JsonPropertyName("side")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BridgePositionSide Side { get; }

        [JsonPropertyName("volume")]
        public decimal Volume { get; }

        [JsonPropertyName("openPrice")]
        public decimal OpenPrice { get; }

        [JsonPropertyName("currentPrice")]
        public decimal CurrentPrice { get; }

        [JsonPropertyName("stopLoss")]
        public decimal StopLoss { get; }

        [JsonPropertyName("takeProfit")]
        public decimal TakeProfit { get; }

        [JsonPropertyName("profit")]
        public decimal Profit { get; }

        [JsonPropertyName("swap")]
        public decimal Swap { get; }

        [JsonPropertyName("magicNumber")]
        public long MagicNumber { get; }

        [JsonPropertyName("comment")]
        public string Comment { get; }

        [JsonPropertyName("openTime")]
        public DateTime OpenTime { get; }

        [JsonConstructor]
        public BridgePositionDto(
            long ticket,
            string symbol,
            BridgePositionSide side,
            decimal volume,
            decimal openPrice,
            decimal currentPrice,
            decimal stopLoss,
            decimal takeProfit,
            decimal profit,
            decimal swap,
            long magicNumber,
            string comment,
            DateTime openTime)
        {
            Ticket = ticket;
            Symbol = symbol;
            Side = side;
            Volume = volume;
            OpenPrice = openPrice;
            CurrentPrice = currentPrice;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Profit = profit;
            Swap = swap;
            MagicNumber = magicNumber;
            Comment = comment ?? string.Empty;
            OpenTime = openTime;
        }
    }
}
