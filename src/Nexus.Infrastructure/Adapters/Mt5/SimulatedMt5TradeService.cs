using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedMt5TradeService : IMt5TradeService
    {
        private readonly ConcurrentDictionary<long, BridgePositionDto> _positions = new();
        private long _ticketCounter = 80000;

        public SimulatedMt5TradeService()
        {
            // Seed a default position for pleasant UI exploration right from the start
            var ticket = Interlocked.Increment(ref _ticketCounter);
            _positions.TryAdd(ticket, new BridgePositionDto(
                ticket: ticket,
                symbol: "EURUSD",
                side: BridgePositionSide.Buy,
                volume: 0.10m,
                openPrice: 1.08500m,
                currentPrice: 1.08550m,
                stopLoss: 1.08000m,
                takeProfit: 1.10000m,
                profit: 5.00m,
                swap: 0.00m,
                magicNumber: 123456,
                comment: "Simulated Start",
                openTime: DateTime.UtcNow.AddHours(-1)
            ));
        }

        public Task<PlaceOrderResponse> PlaceOrderAsync(IMt5Session session, PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var ticket = Interlocked.Increment(ref _ticketCounter);
            var side = request.Side == BridgeOrderSide.Buy ? BridgePositionSide.Buy : BridgePositionSide.Sell;

            var openPrice = 1.08500m; // simple mock open price
            var currentPrice = openPrice;

            var position = new BridgePositionDto(
                ticket: ticket,
                symbol: request.Symbol,
                side: side,
                volume: request.Volume,
                openPrice: openPrice,
                currentPrice: currentPrice,
                stopLoss: request.StopLoss ?? 0m,
                takeProfit: request.TakeProfit ?? 0m,
                profit: 0m,
                swap: 0m,
                magicNumber: 0,
                comment: request.Comment ?? "Simulated Order",
                openTime: DateTime.UtcNow
            );

            _positions.TryAdd(ticket, position);

            var response = new PlaceOrderResponse(
                success: true,
                ticket: ticket,
                status: BridgeOrderExecutionStatus.Executed,
                brokerMessage: "Request executed successfully inside Simulator.",
                comment: request.Comment
            );

            return Task.FromResult(response);
        }

        public Task<ClosePositionResponse> ClosePositionAsync(IMt5Session session, ClosePositionRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            bool removed = _positions.TryRemove(request.Ticket, out _);

            var response = new ClosePositionResponse(
                success: removed,
                ticket: request.Ticket,
                brokerMessage: removed
                    ? "Position fully closed inside Simulator."
                    : $"Position {request.Ticket} not found in Simulator."
            );

            return Task.FromResult(response);
        }

        public Task<IReadOnlyList<BridgePositionDto>> GetOpenPositionsAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BridgePositionDto> list = _positions.Values.ToList();
            return Task.FromResult(list);
        }
    }
}
