using Nexus.Core.Entities;

namespace Nexus.Application.Pipeline
{
    public record ExecutionRequest(
        Guid RequestId,
        string AccountId,
        string SymbolName,
        OrderDirection Direction,
        OrderType Type,
        double Volume,
        double Price,
        double? StopLoss = null,
        double? TakeProfit = null,
        string ClientOrderId = ""
    );
}
