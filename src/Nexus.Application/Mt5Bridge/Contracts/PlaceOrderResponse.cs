using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class PlaceOrderResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; }

        [JsonPropertyName("ticket")]
        public long Ticket { get; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BridgeOrderExecutionStatus Status { get; }

        [JsonPropertyName("brokerMessage")]
        public string? BrokerMessage { get; }

        [JsonPropertyName("comment")]
        public string? Comment { get; }

        [JsonConstructor]
        public PlaceOrderResponse(
            bool success,
            long ticket,
            BridgeOrderExecutionStatus status,
            string? brokerMessage,
            string? comment = null)
        {
            Success = success;
            Ticket = ticket;
            Status = status;
            BrokerMessage = brokerMessage;
            Comment = comment;
        }
    }
}
