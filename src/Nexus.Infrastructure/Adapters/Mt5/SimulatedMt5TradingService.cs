using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class SimulatedMt5TradingService : IMt5TradingService
    {
        private readonly SimulatedMt5TradeService _underlyingService;

        public SimulatedMt5TradingService(SimulatedMt5TradeService underlyingService)
        {
            _underlyingService = underlyingService ?? throw new ArgumentNullException(nameof(underlyingService));
        }

        public async Task<PlaceOrderResult> PlaceMarketOrderAsync(
            string symbol,
            BridgeOrderSide side,
            decimal volume,
            decimal? stopLoss,
            decimal? takeProfit,
            string? comment,
            string? clientCorrelationId,
            CancellationToken cancellationToken)
        {
            var request = new PlaceOrderRequest(symbol, side, volume, stopLoss, takeProfit, comment, clientCorrelationId);
            var response = await _underlyingService.PlaceOrderAsync(null!, request, cancellationToken);

            return new PlaceOrderResult(
                isSuccess: response.Success,
                ticket: response.Ticket,
                status: response.Status.ToString(),
                errorMessage: response.BrokerMessage,
                comment: response.Comment
            );
        }

        public async Task<ClosePositionResult> ClosePositionAsync(
            long positionTicket,
            string symbol,
            decimal? volume,
            CancellationToken cancellationToken)
        {
            var request = new ClosePositionRequest(positionTicket, symbol, volume);
            var response = await _underlyingService.ClosePositionAsync(null!, request, cancellationToken);

            return new ClosePositionResult(
                isSuccess: response.Success,
                ticket: response.Ticket,
                errorMessage: response.BrokerMessage
            );
        }

        public async Task<IReadOnlyList<OpenPositionDto>> GetOpenPositionsAsync(
            CancellationToken cancellationToken)
        {
            var positions = await _underlyingService.GetOpenPositionsAsync(null!, cancellationToken);
            var list = new List<OpenPositionDto>();
            foreach (var pos in positions)
            {
                list.Add(new OpenPositionDto(
                    ticket: pos.Ticket,
                    symbol: pos.Symbol,
                    side: pos.Side.ToString(),
                    volume: pos.Volume,
                    openPrice: pos.OpenPrice,
                    currentPrice: pos.CurrentPrice,
                    stopLoss: pos.StopLoss,
                    takeProfit: pos.TakeProfit,
                    profit: pos.Profit,
                    swap: pos.Swap,
                    magicNumber: pos.MagicNumber,
                    comment: pos.Comment,
                    openTime: pos.OpenTime
                ));
            }
            return list;
        }
    }
}
