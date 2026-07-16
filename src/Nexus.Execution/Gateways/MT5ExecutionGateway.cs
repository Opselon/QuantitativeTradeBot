using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Execution.Domain;

namespace Nexus.Execution.Gateways
{
    public class MT5ExecutionGateway : IExecutionGateway
    {
        private readonly IMt5TradingService _mt5TradingService;

        public MT5ExecutionGateway(IMt5TradingService mt5TradingService)
        {
            _mt5TradingService = mt5TradingService ?? throw new ArgumentNullException(nameof(mt5TradingService));
        }

        public async Task<ExecutionResult> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var stopwatch = Stopwatch.StartNew();

            var side = request.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase)
                ? BridgeOrderSide.Buy
                : BridgeOrderSide.Sell;

            request.TransitionTo(Enums.ExecutionState.Submitted);

            try
            {
                var result = await _mt5TradingService.PlaceMarketOrderAsync(
                    symbol: request.Symbol,
                    side: side,
                    volume: (decimal)request.Volume,
                    stopLoss: request.StopLoss.HasValue ? (decimal)request.StopLoss.Value : null,
                    takeProfit: request.TakeProfit.HasValue ? (decimal)request.TakeProfit.Value : null,
                    comment: request.Reason,
                    clientCorrelationId: request.Id.ToString(),
                    cancellationToken: cancellationToken
                );

                stopwatch.Stop();
                var latency = stopwatch.Elapsed.TotalMilliseconds;

                if (result.IsSuccess)
                {
                    request.TransitionTo(Enums.ExecutionState.Filled);
                    return ExecutionResult.Succeeded(result.Ticket.ToString(), latency);
                }
                else
                {
                    request.TransitionTo(Enums.ExecutionState.Rejected);
                    return ExecutionResult.Failed(result.ErrorMessage ?? "Order was rejected by MT5 broker.", latency);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                request.TransitionTo(Enums.ExecutionState.Rejected);
                return ExecutionResult.Failed($"MT5 execution error: {ex.Message}", stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<ExecutionResult> ClosePositionAsync(string ticketId, double? volume = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            var stopwatch = Stopwatch.StartNew();

            if (!long.TryParse(ticketId, out var ticketLong))
            {
                stopwatch.Stop();
                return ExecutionResult.Failed($"Invalid MT5 ticket format: {ticketId}", stopwatch.Elapsed.TotalMilliseconds);
            }

            try
            {
                // We'll need the symbol. Since symbol is not passed directly, we can retrieve open positions first to find the symbol
                string symbol = string.Empty;
                var openPositions = await _mt5TradingService.GetOpenPositionsAsync(cancellationToken);
                foreach (var pos in openPositions)
                {
                    if (pos.Ticket == ticketLong)
                    {
                        symbol = pos.Symbol;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(symbol))
                {
                    // Fallback to EURUSD or similar
                    symbol = "EURUSD";
                }

                var result = await _mt5TradingService.ClosePositionAsync(
                    positionTicket: ticketLong,
                    symbol: symbol,
                    volume: volume.HasValue ? (decimal)volume.Value : null,
                    cancellationToken: cancellationToken
                );

                stopwatch.Stop();
                var latency = stopwatch.Elapsed.TotalMilliseconds;

                if (result.IsSuccess)
                {
                    return ExecutionResult.Succeeded(ticketId, latency);
                }
                else
                {
                    return ExecutionResult.Failed(result.ErrorMessage ?? "Close position rejected by MT5 broker.", latency);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ExecutionResult.Failed($"MT5 close position error: {ex.Message}", stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<ExecutionResult> ModifyPositionAsync(string ticketId, double? sl = null, double? tp = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            var stopwatch = Stopwatch.StartNew();

            if (!long.TryParse(ticketId, out var ticketLong))
            {
                stopwatch.Stop();
                return ExecutionResult.Failed($"Invalid MT5 ticket format: {ticketId}", stopwatch.Elapsed.TotalMilliseconds);
            }

            try
            {
                string symbol = "EURUSD";
                var openPositions = await _mt5TradingService.GetOpenPositionsAsync(cancellationToken);
                foreach (var pos in openPositions)
                {
                    if (pos.Ticket == ticketLong)
                    {
                        symbol = pos.Symbol;
                        break;
                    }
                }

                var result = await _mt5TradingService.ModifyPositionAsync(
                    positionTicket: ticketLong,
                    symbol: symbol,
                    sl: sl.HasValue ? (decimal)sl.Value : 0m,
                    tp: tp.HasValue ? (decimal)tp.Value : 0m,
                    cancellationToken: cancellationToken
                );

                stopwatch.Stop();
                var latency = stopwatch.Elapsed.TotalMilliseconds;

                if (result.IsSuccess)
                {
                    return ExecutionResult.Succeeded(ticketId, latency);
                }
                else
                {
                    return ExecutionResult.Failed(result.ErrorMessage ?? "Modify position rejected by MT5 broker.", latency);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ExecutionResult.Failed($"MT5 modify position error: {ex.Message}", stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public async Task<IReadOnlyList<PositionSnapshot>> GetPositionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var openPositions = await _mt5TradingService.GetOpenPositionsAsync(cancellationToken);
                var list = new List<PositionSnapshot>();

                foreach (var pos in openPositions)
                {
                    var snapshot = new PositionSnapshot(
                        ticketId: pos.Ticket.ToString(),
                        symbol: pos.Symbol,
                        direction: pos.Side,
                        volume: (double)pos.Volume,
                        entryPrice: (double)pos.OpenPrice,
                        currentPrice: (double)pos.CurrentPrice,
                        stopLoss: pos.StopLoss == 0m ? null : (double?)pos.StopLoss,
                        takeProfit: pos.TakeProfit == 0m ? null : (double?)pos.TakeProfit,
                        unrealizedPnl: pos.Profit,
                        riskExposure: (double)pos.Volume * (double)pos.OpenPrice,
                        status: "OPEN"
                    );
                    list.Add(snapshot);
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MT5ExecutionGateway] GetPositionsAsync Error: {ex.Message}");
                return Array.Empty<PositionSnapshot>();
            }
        }
    }
}
