using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class BridgeMessageEnvelope
    {
        [JsonPropertyName("messageType")]
        public string MessageType { get; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; }

        [JsonPropertyName("command")]
        public string Command { get; }

        [JsonPropertyName("payload")]
        public object? Payload { get; }

        [JsonPropertyName("error")]
        public BridgeError? Error { get; }

        [JsonPropertyName("version")]
        public string Version { get; }

        [JsonConstructor]
        public BridgeMessageEnvelope(
            string messageType,
            string requestId,
            string command,
            object? payload,
            BridgeError? error,
            string version = "1.0")
        {
            MessageType = messageType;
            RequestId = requestId;
            Command = command;
            Payload = payload;
            Error = error;
            Version = version;
        }

        public static BridgeMessageEnvelope CreateRequest(string requestId, string command, object? payload, string version = "1.0")
        {
            return new BridgeMessageEnvelope("Request", requestId, command, payload, null, version);
        }

        public static BridgeMessageEnvelope CreateResponse(string requestId, string command, object? payload, BridgeError? error, string version = "1.0")
        {
            return new BridgeMessageEnvelope("Response", requestId, command, payload, error, version);
        }
    }
}
