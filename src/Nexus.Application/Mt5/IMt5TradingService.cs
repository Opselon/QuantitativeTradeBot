using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;

namespace Nexus.Application.Mt5
{
    public interface IMt5TradingService
    {
        Task<PlaceOrderResult> PlaceMarketOrderAsync(
            string symbol,
            BridgeOrderSide side,
            decimal volume,
            decimal? stopLoss,
            decimal? takeProfit,
            string? comment,
            string? clientCorrelationId,
            CancellationToken cancellationToken);

        Task<ClosePositionResult> ClosePositionAsync(
            long positionTicket,
            string symbol,
            decimal? volume,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<OpenPositionDto>> GetOpenPositionsAsync(
            CancellationToken cancellationToken);
    }
}
