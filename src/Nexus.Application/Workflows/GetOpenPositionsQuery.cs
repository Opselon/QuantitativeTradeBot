using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class GetOpenPositionsQuery
    {
        private readonly IMt5TradeService _tradeService;

        public GetOpenPositionsQuery(IMt5TradeService tradeService)
        {
            _tradeService = tradeService;
        }

        public async Task<IReadOnlyList<BridgePositionDto>> ExecuteAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            return await _tradeService.GetOpenPositionsAsync(session, cancellationToken);
        }
    }
}
