using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class ClosePositionCommand
    {
        private readonly IMt5TradeService _tradeService;

        public ClosePositionCommand(IMt5TradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public async Task<ClosePositionResponse> ExecuteAsync(IMt5Session session, ClosePositionRequest request, CancellationToken cancellationToken = default)
        {
            return await _tradeService.ClosePositionAsync(session, request, cancellationToken);
        }
    }
}
