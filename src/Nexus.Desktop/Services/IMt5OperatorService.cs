using Nexus.Desktop.Models;

namespace Nexus.Desktop.Services
{
    public interface IMt5OperatorService
    {
        Task<IReadOnlyList<DesktopPositionDto>> GetPositionsAsync(CancellationToken cancellationToken);
        Task<DesktopTradeResult> ModifyPositionAsync(long ticket, string symbol, decimal sl, decimal tp, CancellationToken cancellationToken);
        Task<DesktopTradeResult> PlaceOrderAsync(
            string symbol,
            DesktopOrderSide side,
            decimal volume,
            decimal? stopLoss,
            decimal? takeProfit,
            string comment, // Added custom comment support
            CancellationToken cancellationToken);

        Task<DesktopTradeResult> ClosePositionAsync(long ticket, string symbol, CancellationToken cancellationToken);
    }
}