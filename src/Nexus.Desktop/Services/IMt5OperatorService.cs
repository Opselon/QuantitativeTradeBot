using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Desktop.Models;

namespace Nexus.Desktop.Services
{
    public interface IMt5OperatorService
    {
        Task<IReadOnlyList<DesktopPositionDto>> GetPositionsAsync(CancellationToken cancellationToken);
        Task<DesktopTradeResult> PlaceOrderAsync(string symbol, DesktopOrderSide side, decimal volume, CancellationToken cancellationToken);
        Task<DesktopTradeResult> ClosePositionAsync(long ticket, string symbol, CancellationToken cancellationToken);
    }
}
