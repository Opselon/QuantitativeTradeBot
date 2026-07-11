using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class PlaceOrderCommand
    {
        private readonly IMt5TradeService _tradeService;

        public PlaceOrderCommand(IMt5TradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public async Task<PlaceOrderResponse> ExecuteAsync(IMt5Session session, PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            return await _tradeService.PlaceOrderAsync(session, request, cancellationToken);
        }
    }
}
