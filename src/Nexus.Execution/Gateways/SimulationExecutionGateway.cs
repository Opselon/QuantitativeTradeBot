using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Execution.Domain;
using Nexus.Execution.Enums;

namespace Nexus.Execution.Gateways
{
    public class SimulationExecutionGateway : IExecutionGateway
    {
        private readonly ConcurrentDictionary<string, PositionSnapshot> _positions = new();
        private long _ticketCounter = 200000;

        public void TrackPosition(PositionSnapshot position)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            _positions[position.TicketId] = position;
        }

        public Task<ExecutionResult> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var stopwatch = Stopwatch.StartNew();

            // Simulate slight latency to be production ready and realistic (~5-15ms)
            Thread.Sleep(10);

            var ticketId = "SIM_" + Interlocked.Increment(ref _ticketCounter);
            request.TransitionTo(ExecutionState.Submitted);

            // Create open position snapshot
            var isBuy = request.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase);
            var direction = isBuy ? "Buy" : "Sell";
            var snapshot = new PositionSnapshot(
                ticketId: ticketId,
                symbol: request.Symbol,
                direction: direction,
                volume: request.Volume,
                entryPrice: request.Entry,
                currentPrice: request.Entry,
                stopLoss: request.StopLoss,
                takeProfit: request.TakeProfit,
                unrealizedPnl: 0m,
                riskExposure: request.Volume * request.Entry,
                status: "OPEN"
            );

            _positions.TryAdd(ticketId, snapshot);
            request.TransitionTo(ExecutionState.Filled);

            stopwatch.Stop();
            var latency = stopwatch.Elapsed.TotalMilliseconds;

            return Task.FromResult(ExecutionResult.Succeeded(ticketId, latency));
        }

        public Task<ExecutionResult> ClosePositionAsync(string ticketId, double? volume = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            var stopwatch = Stopwatch.StartNew();

            if (_positions.TryRemove(ticketId, out var position))
            {
                position.Status = "CLOSED";
                stopwatch.Stop();
                return Task.FromResult(ExecutionResult.Succeeded(ticketId, stopwatch.Elapsed.TotalMilliseconds));
            }

            stopwatch.Stop();
            return Task.FromResult(ExecutionResult.Failed($"Simulated position with ticket {ticketId} not found.", stopwatch.Elapsed.TotalMilliseconds));
        }

        public Task<ExecutionResult> ModifyPositionAsync(string ticketId, double? sl = null, double? tp = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            var stopwatch = Stopwatch.StartNew();

            if (_positions.TryGetValue(ticketId, out var position))
            {
                position.StopLoss = sl;
                position.TakeProfit = tp;
                stopwatch.Stop();
                return Task.FromResult(ExecutionResult.Succeeded(ticketId, stopwatch.Elapsed.TotalMilliseconds));
            }

            stopwatch.Stop();
            return Task.FromResult(ExecutionResult.Failed($"Simulated position with ticket {ticketId} not found.", stopwatch.Elapsed.TotalMilliseconds));
        }

        public Task<IReadOnlyList<PositionSnapshot>> GetPositionsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PositionSnapshot> active = _positions.Values.ToList();
            return Task.FromResult(active);
        }
    }
}
