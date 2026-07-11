using System;

namespace Nexus.Application.Pipeline
{
    public class OrderIntentFactory
    {
        public OrderIntent CreateIntent(TradeSignal signal)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));

            return new OrderIntent(
                Guid.NewGuid(),
                signal.StrategyId,
                signal.SymbolName,
                signal.Direction,
                signal.Type,
                signal.Volume,
                signal.Price,
                signal.StopLoss,
                signal.TakeProfit
            );
        }
    }
}
