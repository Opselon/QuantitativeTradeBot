using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RealMt5BridgeAdapter : IMt5AccountService, IMt5TradeService
    {
        private readonly IMt5BridgeClient _bridgeClient;

        public RealMt5BridgeAdapter(IMt5BridgeClient bridgeClient)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
        }

        public async Task<AccountSnapshotDto> GetAccountSnapshotAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Generate unique requestId for correlation
            string requestId = Guid.NewGuid().ToString();
            var request = BridgeMessageEnvelope.CreateRequest(requestId, "GetAccountSnapshot", null);

            try
            {
                // Send command and await correlated response from the MQL5 EA Client
                var responseEnvelope = await _bridgeClient.SendAsync(request, cancellationToken);

                if (responseEnvelope.Error != null)
                {
                    throw new Exception($"MT5 Bridge Error [{responseEnvelope.Error.Code}]: {responseEnvelope.Error.Message}");
                }

                if (responseEnvelope.Payload == null)
                {
                    throw new Exception("MT5 Bridge received an empty payload response.");
                }

                // Safely convert payload to GetAccountSnapshotResponse DTO
                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var snapshotResponse = JsonSerializer.Deserialize<GetAccountSnapshotResponse>(payloadJson);

                if (snapshotResponse == null)
                {
                    throw new Exception("Failed to parse Account Snapshot from bridge response payload.");
                }

                return new AccountSnapshotDto
                {
                    AccountId = snapshotResponse.AccountId.ToString(),
                    BrokerServer = snapshotResponse.Broker,
                    Balance = snapshotResponse.Balance,
                    Equity = snapshotResponse.Equity,
                    Margin = snapshotResponse.Margin,
                    FreeMargin = snapshotResponse.FreeMargin,
                    Leverage = snapshotResponse.Leverage,
                    Currency = snapshotResponse.Currency,
                    AccountMode = snapshotResponse.Broker.Contains("Demo", StringComparison.OrdinalIgnoreCase) ? "Demo" : "Real",
                    TerminalStatus = snapshotResponse.ConnectionHealth
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5BridgeAdapter] Error fetching account snapshot: {ex.Message}");
                throw;
            }
        }

        public async Task<PlaceOrderResponse> PlaceOrderAsync(IMt5Session session, PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Validate fields on C# side first
            if (string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("Symbol cannot be empty.", nameof(request));
            if (request.Volume <= 0)
                throw new ArgumentException("Volume must be greater than zero.", nameof(request));

            string requestId = Guid.NewGuid().ToString();
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "PlaceOrder", request);

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
                var placeOrderResponse = JsonSerializer.Deserialize<PlaceOrderResponse>(payloadJson);

                if (placeOrderResponse == null)
                {
                    throw new Exception("Failed to parse PlaceOrder response from bridge payload.");
                }

                return placeOrderResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5BridgeAdapter] Error placing order: {ex.Message}");
                throw;
            }
        }

        public async Task<ClosePositionResponse> ClosePositionAsync(IMt5Session session, ClosePositionRequest request, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.Ticket <= 0)
                throw new ArgumentException("Ticket must be greater than zero.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Symbol))
                throw new ArgumentException("Symbol cannot be empty.", nameof(request));

            string requestId = Guid.NewGuid().ToString();
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "ClosePosition", request);

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
                var closePositionResponse = JsonSerializer.Deserialize<ClosePositionResponse>(payloadJson);

                if (closePositionResponse == null)
                {
                    throw new Exception("Failed to parse ClosePosition response from bridge payload.");
                }

                return closePositionResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5BridgeAdapter] Error closing position: {ex.Message}");
                throw;
            }
        }

        public async Task<IReadOnlyList<BridgePositionDto>> GetOpenPositionsAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            string requestId = Guid.NewGuid().ToString();
            var envelopeRequest = BridgeMessageEnvelope.CreateRequest(requestId, "GetOpenPositions", null);

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

                return openPositionsResponse.Positions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RealMt5BridgeAdapter] Error retrieving open positions: {ex.Message}");
                throw;
            }
        }
    }
}
