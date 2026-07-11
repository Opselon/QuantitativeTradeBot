using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;

namespace Nexus.Application.Ports
{
    public interface IMt5TradeService
    {
        Task<PlaceOrderResponse> PlaceOrderAsync(IMt5Session session, PlaceOrderRequest request, CancellationToken cancellationToken = default);
        Task<ClosePositionResponse> ClosePositionAsync(IMt5Session session, ClosePositionRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BridgePositionDto>> GetOpenPositionsAsync(IMt5Session session, CancellationToken cancellationToken = default);
    }
}
