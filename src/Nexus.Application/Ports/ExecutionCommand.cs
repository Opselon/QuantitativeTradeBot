using System;
using Nexus.Core.Entities;

namespace Nexus.Application.Ports
{
    public record ExecutionCommand(
        Guid CommandId,
        string AccountId,
        string Symbol,
        OrderDirection Direction,
        OrderType Type,
        double Volume,
        double Price,
        double? StopLoss = null,
        double? TakeProfit = null,
        string ClientOrderId = ""
    );
}
