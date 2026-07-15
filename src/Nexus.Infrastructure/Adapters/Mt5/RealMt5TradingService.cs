using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RealMt5TradingService : IMt5TradingService
    {
        private readonly IMt5BridgeClient _bridgeClient;

        public RealMt5TradingService(IMt5BridgeClient bridgeClient)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
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
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return new PlaceOrderResult(false, 0, "Failed", "Symbol cannot be empty.", comment);
            }
            if (volume <= 0)
            {
                return new PlaceOrderResult(false, 0, "Failed", "Volume must be greater than zero.", comment);
            }

            string requestId = Guid.NewGuid().ToString();
            var request = new PlaceOrderRequest(symbol, side, volume, stopLoss, takeProfit, comment, clientCorrelationId);
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "PlaceOrder", request);

            Console.WriteLine($"[RealMt5TradingService] Sending PlaceOrder command. Request ID: {requestId}, Symbol: {symbol}, Side: {side}, Volume: {volume}, CorrelationId: {clientCorrelationId}");

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(envelopeRequest, cancellationToken);

                if (responseEnvelope.Error != null)
                {
                    return new PlaceOrderResult(
                        isSuccess: false,
                        ticket: 0,
                        status: "Failed",
                        errorMessage: $"MT5 Bridge Error [{responseEnvelope.Error.Code}]: {responseEnvelope.Error.Message}",
                        comment: comment);
                }

                if (responseEnvelope.Payload == null)
                {
                    return new PlaceOrderResult(
                        isSuccess: false,
                        ticket: 0,
                        status: "Failed",
                        errorMessage: "MT5 Bridge received an empty payload response.",
                        comment: comment);
                }

                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var placeOrderResponse = JsonSerializer.Deserialize<PlaceOrderResponse>(payloadJson);

                if (placeOrderResponse == null)
                {
                    return new PlaceOrderResult(
                        isSuccess: false,
                        ticket: 0,
                        status: "Failed",
                        errorMessage: "Failed to parse PlaceOrder response from bridge payload.",
                        comment: comment);
                }

                return new PlaceOrderResult(
                    isSuccess: placeOrderResponse.Success,
                    ticket: placeOrderResponse.Ticket,
                    status: placeOrderResponse.Status.ToString(),
                    errorMessage: placeOrderResponse.BrokerMessage,
                    comment: placeOrderResponse.Comment ?? comment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5TradingService] Error placing order: {ex.Message}");
                return new PlaceOrderResult(
                    isSuccess: false,
                    ticket: 0,
                    status: "Failed",
                    errorMessage: ex.Message,
                    comment: comment);
            }
        }

        public async Task<ClosePositionResult> ClosePositionAsync(
            long positionTicket,
            string symbol,
            decimal? volume,
            CancellationToken cancellationToken)
        {
            if (positionTicket <= 0)
            {
                return new ClosePositionResult(false, positionTicket, "Ticket must be greater than zero.");
            }
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return new ClosePositionResult(false, positionTicket, "Symbol cannot be empty.");
            }

            string requestId = Guid.NewGuid().ToString();
            var request = new ClosePositionRequest(positionTicket, symbol, volume);
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "ClosePosition", request);

            Console.WriteLine($"[RealMt5TradingService] Sending ClosePosition command. Request ID: {requestId}, Ticket: {positionTicket}, Symbol: {symbol}, Volume: {volume}");

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(envelopeRequest, cancellationToken);

                if (responseEnvelope.Error != null)
                {
                    return new ClosePositionResult(
                        isSuccess: false,
                        ticket: positionTicket,
                        errorMessage: $"MT5 Bridge Error [{responseEnvelope.Error.Code}]: {responseEnvelope.Error.Message}");
                }

                if (responseEnvelope.Payload == null)
                {
                    return new ClosePositionResult(
                        isSuccess: false,
                        ticket: positionTicket,
                        errorMessage: "MT5 Bridge received an empty payload response.");
                }

                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var closePositionResponse = JsonSerializer.Deserialize<ClosePositionResponse>(payloadJson);

                if (closePositionResponse == null)
                {
                    return new ClosePositionResult(
                        isSuccess: false,
                        ticket: positionTicket,
                        errorMessage: "Failed to parse ClosePosition response from bridge payload.");
                }

                return new ClosePositionResult(
                    isSuccess: closePositionResponse.Success,
                    ticket: closePositionResponse.Ticket,
                    errorMessage: closePositionResponse.BrokerMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5TradingService] Error closing position: {ex.Message}");
                return new ClosePositionResult(
                    isSuccess: false,
                    ticket: positionTicket,
                    errorMessage: ex.Message);
            }
        }
    public async Task<PlaceOrderResult> ModifyPositionAsync(
    long positionTicket,
    string symbol,
    decimal sl,
    decimal tp,
    CancellationToken cancellationToken)
        {
            string requestId = Guid.NewGuid().ToString();

            // Use an anonymous payload model to map exact contract keys cleanly
            var requestPayload = new
            {
                ticket = positionTicket,
                symbol = symbol,
                sl = sl,
                tp = tp
            };

            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "ModifyPosition", requestPayload);

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(envelopeRequest, cancellationToken);
                if (responseEnvelope.Error != null)
                {
                    return new PlaceOrderResult(false, positionTicket, "Failed", responseEnvelope.Error.Message, null);
                }
                return new PlaceOrderResult(true, positionTicket, "Success", "Modified successfully", null);
            }
            catch (Exception ex)
            {
                return new PlaceOrderResult(false, positionTicket, "Failed", ex.Message, null);
            }
        }


        public async Task<IReadOnlyList<OpenPositionDto>> GetOpenPositionsAsync(
            CancellationToken cancellationToken)
        {
            string requestId = Guid.NewGuid().ToString();
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "GetOpenPositions", null);

            Console.WriteLine($"[RealMt5TradingService] Sending GetOpenPositions command. Request ID: {requestId}");

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(envelopeRequest, cancellationToken);

                if (responseEnvelope.Error != null)
                {
                    throw new Exception($"MT5 Bridge Error [{responseEnvelope.Error.Code}]: {responseEnvelope.Error.Message}");
                }

                if (responseEnvelope.Payload == null)
                {
                    throw new Exception("MT5 Bridge received an empty payload response.");
                }

                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var openPositionsResponse = JsonSerializer.Deserialize<GetOpenPositionsResponse>(payloadJson);

                if (openPositionsResponse == null)
                {
                    throw new Exception("Failed to parse GetOpenPositions response from bridge payload.");
                }

                var resultList = new List<OpenPositionDto>();
                foreach (var pos in openPositionsResponse.Positions)
                {
                    resultList.Add(new OpenPositionDto(
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

                return resultList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5TradingService] Error retrieving open positions: {ex.Message}");
                throw;
            }
        }
    }
}
