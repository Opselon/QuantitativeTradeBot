using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class BridgeError
    {
        [JsonPropertyName("code")]
        public string Code { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        [JsonPropertyName("details")]
        public string? Details { get; }

        [JsonConstructor]
        public BridgeError(string code, string message, string? details = null)
        {
            Code = code;
            Message = message;
            Details = details;
        }
    }
}
