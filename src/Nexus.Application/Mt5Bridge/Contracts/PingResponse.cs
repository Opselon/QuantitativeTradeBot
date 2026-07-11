using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class PingResponse
    {
        [JsonPropertyName("serverTime")]
        public string ServerTime { get; }

        [JsonPropertyName("terminalTime")]
        public string TerminalTime { get; }

        [JsonPropertyName("status")]
        public string Status { get; }

        [JsonConstructor]
        public PingResponse(string serverTime, string terminalTime, string status)
        {
            ServerTime = serverTime;
            TerminalTime = terminalTime;
            Status = status;
        }
    }
}
