using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Execution.Domain;
using Nexus.Execution.Enums;
using Nexus.Execution.Gateways;

namespace Nexus.Execution.Management
{
    public class PositionManager
    {
        private readonly ConcurrentDictionary<string, PositionSnapshot> _openPositions = new();
        private readonly ConcurrentBag<PositionSnapshot> _closedPositions = new();
        private readonly ConcurrentDictionary<Guid, OrderRequest> _pendingOrders = new();
        private readonly IExecutionGateway _executionGateway;

        public PositionManager(IExecutionGateway executionGateway)
        {
            _executionGateway = executionGateway ?? throw new ArgumentNullException(nameof(executionGateway));
        }

        #region Thread-safe In-memory Tracking Lists

        public IReadOnlyList<PositionSnapshot> OpenPositions => _openPositions.Values.ToList();
        public IReadOnlyList<PositionSnapshot> ClosedPositions => _closedPositions.ToList();
        public IReadOnlyList<OrderRequest> PendingOrders => _pendingOrders.Values.ToList();

        #endregion

        #region Core Position Management APIs

        public void TrackOrder(OrderRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _pendingOrders[request.Id] = request;
        }

        public void UntrackOrder(Guid orderId)
        {
            _pendingOrders.TryRemove(orderId, out _);
        }

        public void TrackPosition(PositionSnapshot position)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            _openPositions[position.TicketId] = position;
        }

        /// <summary>
        /// Modifies stops (Stop Loss and Take Profit) for a specific active position.
        /// </summary>
        public async Task<ExecutionResult> HandleStopModificationAsync(
            string ticketId,
            double? sl,
            double? tp,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            // Call gateway to update the actual broker position
            var result = await _executionGateway.ModifyPositionAsync(ticketId, sl, tp, cancellationToken);

            if (result.Success)
            {
                if (_openPositions.TryGetValue(ticketId, out var position))
                {
                    position.StopLoss = sl;
                    position.TakeProfit = tp;
                }
            }

            return result;
        }

        /// <summary>
        /// Support partial close of a position. Updates the volume of the position in memory,
        /// or completely closes it if the remaining volume falls to zero.
        /// </summary>
        public async Task<ExecutionResult> HandlePartialCloseAsync(
            string ticketId,
            double closeVolume,
            double closePrice,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));
            if (closeVolume <= 0)
                throw new ArgumentException("Close volume must be greater than zero.", nameof(closeVolume));

            if (!_openPositions.TryGetValue(ticketId, out var position))
            {
                return ExecutionResult.Failed($"Position with ticket {ticketId} not found in open positions tracker.", 0);
            }

            if (closeVolume > position.Volume)
            {
                return ExecutionResult.Failed($"Close volume {closeVolume} exceeds current position volume {position.Volume}.", 0);
            }

            // Call gateway to close the requested volume
            var result = await _executionGateway.ClosePositionAsync(ticketId, closeVolume, cancellationToken);

            if (result.Success)
            {
                if (Math.Abs(position.Volume - closeVolume) < 0.0001)
                {
                    // Full close
                    if (_openPositions.TryRemove(ticketId, out var closedPos))
                    {
                        closedPos.Status = "CLOSED";
                        closedPos.CurrentPrice = closePrice;
                        closedPos.UnrealizedPnl = RecalculatePnl(closedPos.Symbol, closedPos.Direction, closedPos.Volume, closedPos.EntryPrice, closePrice);
                        _closedPositions.Add(closedPos);
                    }
                }
                else
                {
                    // Partial close: update the volume in-memory
                    var updatedVolume = position.Volume - closeVolume;
                    var newPos = new PositionSnapshot(
                        ticketId: position.TicketId,
                        symbol: position.Symbol,
                        direction: position.Direction,
                        volume: updatedVolume,
                        entryPrice: position.EntryPrice,
                        currentPrice: closePrice,
                        stopLoss: position.StopLoss,
                        takeProfit: position.TakeProfit,
                        unrealizedPnl: RecalculatePnl(position.Symbol, position.Direction, updatedVolume, position.EntryPrice, closePrice),
                        riskExposure: updatedVolume * position.EntryPrice,
                        status: "OPEN"
                    );
                    _openPositions[ticketId] = newPos;

                    // Also add the closed portion to closed positions list as an audit record
                    var closedPortion = new PositionSnapshot(
                        ticketId: position.TicketId + "_PARTIAL",
                        symbol: position.Symbol,
                        direction: position.Direction,
                        volume: closeVolume,
                        entryPrice: position.EntryPrice,
                        currentPrice: closePrice,
                        stopLoss: position.StopLoss,
                        takeProfit: position.TakeProfit,
                        unrealizedPnl: RecalculatePnl(position.Symbol, position.Direction, closeVolume, position.EntryPrice, closePrice),
                        riskExposure: closeVolume * position.EntryPrice,
                        status: "CLOSED"
                    );
                    _closedPositions.Add(closedPortion);
                }
            }

            return result;
        }

        /// <summary>
        /// Synchronizes the local tracking collection with the gateway's actual active positions.
        /// Moves closed positions to the closed collection and adds new ones.
        /// </summary>
        public async Task SynchronizePositionsAsync(CancellationToken cancellationToken = default)
        {
            var activeSnapshots = await _executionGateway.GetPositionsAsync(cancellationToken);
            var activeTickets = new HashSet<string>(activeSnapshots.Select(s => s.TicketId));

            // 1. Move open positions that are no longer active to closed list
            foreach (var ticketId in _openPositions.Keys.ToList())
            {
                if (!activeTickets.Contains(ticketId))
                {
                    if (_openPositions.TryRemove(ticketId, out var position))
                    {
                        position.Status = "CLOSED";
                        _closedPositions.Add(position);
                    }
                }
            }

            // 2. Add or update open positions with the latest gateway state
            foreach (var snapshot in activeSnapshots)
            {
                _openPositions[snapshot.TicketId] = snapshot;
            }
        }

        #endregion

        #region Helper Math

        private static decimal RecalculatePnl(string symbol, string direction, double volume, double entryPrice, double currentPrice)
        {
            double multiplier = symbol.ToUpperInvariant().Contains("XAU") || symbol.ToUpperInvariant().Contains("GOLD") ? 100.0 : 100000.0;
            double diff = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase)
                ? (currentPrice - entryPrice)
                : (entryPrice - currentPrice);
            return (decimal)Math.Round(diff * volume * multiplier, 4);
        }

        #endregion
    }
}
