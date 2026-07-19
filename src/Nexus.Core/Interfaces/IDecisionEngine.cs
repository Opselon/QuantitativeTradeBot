using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for the quantitative decision intelligence layer.
    /// Combines neural evaluation, live market state, and pre-trade risk statuses to yield executable trade decisions.
    /// </summary>
    public interface IDecisionEngine
    {
        TradeDecision Evaluate(EvaluationResult evaluation, MarketState market, RiskState risk);
    }
}
