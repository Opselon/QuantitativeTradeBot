using System;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Contract for domain-level operations and state synchronization of open/closed trading positions.
    /// </summary>
    public interface IPositionManager
    {
        /// <summary>
        /// Synchronizes the latest price tick with an active open position, recalculating floating profit/loss.
        /// </summary>
        /// <param name="position">The open position to update.</param>
        /// <param name="currentPrice">The new market bid/ask price.</param>
        void UpdatePosition(Position position, Price currentPrice);

        /// <summary>
        /// Adjusts risk bounds (Stop Loss / Take Profit) of an open position.
        /// </summary>
        /// <param name="position">The target position.</param>
        /// <param name="stopLoss">The optional new Stop Loss price.</param>
        /// <param name="takeProfit">The optional new Take Profit price.</param>
        void ModifyPositionSlTp(Position position, Price? stopLoss, Price? takeProfit);
    }
}
