using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class ClosePositionResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; }

        [JsonPropertyName("ticket")]
        public long Ticket { get; }

        [JsonPropertyName("brokerMessage")]
        public string? BrokerMessage { get; }

        [JsonConstructor]
        public ClosePositionResponse(bool success, long ticket, string? brokerMessage)
        {
            Success = success;
            Ticket = ticket;
            BrokerMessage = brokerMessage;
        }
    }
}
