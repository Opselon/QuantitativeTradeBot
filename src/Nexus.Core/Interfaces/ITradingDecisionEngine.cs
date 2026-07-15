using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Evaluates current intelligence parameters to form high-confidence execution decisions.
    /// </summary>
    public interface ITradingDecisionEngine
    {
        /// <summary>
        /// Generates the ultimate actionable trade decision for the platform execution pipeline.
        /// </summary>
        /// <param name="marketState">The classified condition of the market.</param>
        /// <param name="account">The current account balance and equity details.</param>
        /// <param name="riskState">The system-wide pre-trade risk limitations and metrics.</param>
        /// <returns>A TradeDecision with specific direction, volume, and analytical rationale.</returns>
        TradeDecision GenerateDecision(MarketState marketState, Account account, RiskState riskState);
    }
}
