using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for a Stockfish-inspired tree-based scenario search and evaluation engine.
    /// Simulates possible action paths (BUY, SELL, WAIT) and ranks them using probabilistic expectations.
    /// </summary>
    public interface IScenarioSearchEngine
    {
        /// <summary>
        /// Searches and evaluates the best possible decision path under current market and risk states.
        /// </summary>
        Task<ScenarioSearchNode> SearchBestActionAsync(
            MarketState currentState,
            RiskState riskState,
            double neuralBuyConfidence,
            double neuralSellConfidence,
            CancellationToken ct);
    }
}
