using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class ClosePositionRequest
    {
        [JsonPropertyName("ticket")]
        public long Ticket { get; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; }

        [JsonPropertyName("volume")]
        public decimal? Volume { get; }

        [JsonConstructor]
        public ClosePositionRequest(long ticket, string symbol, decimal? volume = null)
        {
            Ticket = ticket;
            Symbol = symbol;
            Volume = volume;
        }
    }
}
