using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Desktop.Models;

namespace Nexus.Desktop.Services
{
    public class Mt5OperatorService : IMt5OperatorService
    {
        private readonly IMt5TradingService _tradingService;

        public Mt5OperatorService(IMt5TradingService tradingService)
        {
            _tradingService = tradingService ?? throw new ArgumentNullException(nameof(tradingService));
        }

        public async Task<IReadOnlyList<DesktopPositionDto>> GetPositionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var originalPositions = await _tradingService.GetOpenPositionsAsync(cancellationToken);
                var dtos = new List<DesktopPositionDto>();
                foreach (var pos in originalPositions)
                {
                    dtos.Add(new DesktopPositionDto
                    {
                        Ticket = pos.Ticket,
                        Symbol = pos.Symbol,
                        Side = pos.Side,
                        Volume = pos.Volume,
                        OpenPrice = pos.OpenPrice,
                        CurrentPrice = pos.CurrentPrice,
                        StopLoss = pos.StopLoss,
                        TakeProfit = pos.TakeProfit,
                        Profit = pos.Profit,
                        Swap = pos.Swap,
                        Commission = 0.0m, // MT5 open positions doesn't expose commission in DTO, default to 0
                        OpenTime = pos.OpenTime,
                        Status = "Open"
                    });
                }
                return dtos;
            }
            catch (Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        public async Task<DesktopTradeResult> PlaceOrderAsync(
                string symbol,
                DesktopOrderSide side,
                decimal volume,
                decimal? stopLoss,
                decimal? takeProfit,
                string comment, // Forwarded custom comment
                CancellationToken cancellationToken)
        {
            try
            {
                var bridgeSide = side == DesktopOrderSide.Buy ? BridgeOrderSide.Buy : BridgeOrderSide.Sell;
                var result = await _tradingService.PlaceMarketOrderAsync(
                    symbol,
                    bridgeSide,
                    volume,
                    stopLoss,
                    takeProfit,
                    comment, // Applied to the order deal Comment property
                    clientCorrelationId: Guid.NewGuid().ToString(),
                    cancellationToken: cancellationToken
                );

                return new DesktopTradeResult
                {
                    IsSuccess = result.IsSuccess,
                    Ticket = result.Ticket,
                    ErrorMessage = result.ErrorMessage,
                    Message = result.Comment ?? result.Status
                };
            }
            catch (Exception ex)
            {
                throw TranslateException(ex);
            }
        }



        public async Task<DesktopTradeResult> ModifyPositionAsync(long ticket, string symbol, decimal sl, decimal tp, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _tradingService.ModifyPositionAsync(ticket, symbol, sl, tp, cancellationToken);
                return new DesktopTradeResult
                {
                    IsSuccess = result.IsSuccess,
                    Ticket = result.Ticket,
                    ErrorMessage = result.ErrorMessage,
                    Message = result.Status
                };
            }
            catch (Exception ex)
            {
                throw TranslateException(ex);
            }
        }



        public async Task<DesktopTradeResult> ClosePositionAsync(long ticket, string symbol, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _tradingService.ClosePositionAsync(ticket, symbol, volume: null, cancellationToken: cancellationToken);
                return new DesktopTradeResult
                {
                    IsSuccess = result.IsSuccess,
                    Ticket = result.Ticket,
                    ErrorMessage = result.ErrorMessage,
                    Message = result.IsSuccess ? "Closed successfully" : "Close failed"
                };
            }
            catch (Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        private Exception TranslateException(Exception ex)
        {
            if (ex is TimeoutException)
            {
                return new Exception("Bridge timeout", ex);
            }
            if (ex is System.Net.Sockets.SocketException)
            {
                return new Exception("Connection lost", ex);
            }
            if (ex is OperationCanceledException)
            {
                return new Exception("Operation cancelled", ex);
            }
            return new Exception("Unexpected error", ex);
        }
    }
}