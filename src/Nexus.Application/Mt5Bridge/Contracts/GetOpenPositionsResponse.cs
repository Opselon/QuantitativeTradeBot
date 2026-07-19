using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class GetOpenPositionsResponse
    {
        [JsonPropertyName("positions")]
        public List<BridgePositionDto> Positions { get; }

        [JsonConstructor]
        public GetOpenPositionsResponse(List<BridgePositionDto> positions)
        {
            Positions = positions ?? new List<BridgePositionDto>();
        }
    }
}
