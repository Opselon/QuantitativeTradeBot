using System;
using Nexus.Core.Entities;

namespace Nexus.Application.Pipeline
{
    public record TradeSignal(
        string StrategyId,
        string SymbolName,
        OrderDirection Direction,
        OrderType Type,
        double Volume,
        double Price,
        double? StopLoss = null,
        double? TakeProfit = null
    );
}
