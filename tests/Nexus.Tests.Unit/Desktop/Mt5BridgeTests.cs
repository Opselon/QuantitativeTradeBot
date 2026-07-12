using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Nexus.Application.Mt5;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using Nexus.Infrastructure.Adapters.Mt5;

namespace Nexus.Tests.Unit.Desktop
{
    public class Mt5BridgeTests
    {
        [Fact]
        public void MessageEnvelope_Serialization_RoundTrip_PreservesAllFields()
        {
            // Arrange
            var originalResponse = new GetAccountSnapshotResponse(
                accountId: 998234,
                broker: "ICMarkets-Live-US",
                currency: "EUR",
                balance: 24500.50m,
                equity: 24650.75m,
                margin: 400.00m,
                freeMargin: 24250.75m,
                leverage: 200,
                connectionHealth: "Healthy"
            );

            var envelope = BridgeMessageEnvelope.CreateResponse(
                requestId: "uuid-1234-test",
                command: "GetAccountSnapshot",
                payload: originalResponse,
                error: null
            );

            // Act
            var json = JsonSerializer.Serialize(envelope);
            var deserializedEnvelope = JsonSerializer.Deserialize<BridgeMessageEnvelope>(json);

            // Assert
            Assert.NotNull(deserializedEnvelope);
            Assert.Equal("Response", deserializedEnvelope.MessageType);
            Assert.Equal("uuid-1234-test", deserializedEnvelope.RequestId);
            Assert.Equal("GetAccountSnapshot", deserializedEnvelope.Command);
            Assert.Equal("1.0", deserializedEnvelope.Version);
            Assert.Null(deserializedEnvelope.Error);

            // Deserialize the payload back
            var payloadJson = JsonSerializer.Serialize(deserializedEnvelope.Payload);
            var deserializedResponse = JsonSerializer.Deserialize<GetAccountSnapshotResponse>(payloadJson);

            Assert.NotNull(deserializedResponse);
            Assert.Equal(998234, deserializedResponse.AccountId);
            Assert.Equal("ICMarkets-Live-US", deserializedResponse.Broker);
            Assert.Equal("EUR", deserializedResponse.Currency);
            Assert.Equal(24500.50m, deserializedResponse.Balance);
            Assert.Equal(24650.75m, deserializedResponse.Equity);
            Assert.Equal(400.00m, deserializedResponse.Margin);
            Assert.Equal(24250.75m, deserializedResponse.FreeMargin);
            Assert.Equal(200, deserializedResponse.Leverage);
            Assert.Equal("Healthy", deserializedResponse.ConnectionHealth);
        }

        [Fact]
        public async Task RealMt5BridgeAdapter_MapsGetAccountSnapshotResponse_ToAccountSnapshotDto_Correctly()
        {
            // Arrange
            var responsePayload = new GetAccountSnapshotResponse(
                accountId: 7820491,
                broker: "ICMarkets-Demo",
                currency: "USD",
                balance: 50000.00m,
                equity: 50245.50m,
                margin: 1200.00m,
                freeMargin: 49045.50m,
                leverage: 500,
                connectionHealth: "Healthy"
            );

            var envelope = BridgeMessageEnvelope.CreateResponse(
                requestId: "some-guid",
                command: "GetAccountSnapshot",
                payload: responsePayload,
                error: null
            );

            var mockBridgeClient = new StubBridgeClient(envelope);
            var adapter = new RealMt5BridgeAdapter(mockBridgeClient);
            var mockSession = new StubSession();

            // Act
            var snapshot = await adapter.GetAccountSnapshotAsync(mockSession, CancellationToken.None);

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("7820491", snapshot.AccountId);
            Assert.Equal("ICMarkets-Demo", snapshot.BrokerServer);
            Assert.Equal(50000.00m, snapshot.Balance);
            Assert.Equal(50245.50m, snapshot.Equity);
            Assert.Equal(1200.00m, snapshot.Margin);
            Assert.Equal(49045.50m, snapshot.FreeMargin);
            Assert.Equal(500, snapshot.Leverage);
            Assert.Equal("USD", snapshot.Currency);
            Assert.Equal("Demo", snapshot.AccountMode);
            Assert.Equal("Healthy", snapshot.TerminalStatus);
        }

        [Fact]
        public async Task RoutingMt5ConnectionService_DelegatesCorrectly_BasedOnActiveMt5Mode()
        {
            // Arrange
            var mockConfig = new StubAppConfigurationService();
            // Start with Simulated, then switch to Real
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Simulated" };

            var simulatedService = new SimulatedMt5ConnectionService();

            var stubBridgeClient = new StubBridgeClient(null);
            var stubAccountService = new StubAccountService();
            var realService = new RealMt5BridgeConnectionService(stubBridgeClient, stubAccountService);

            var routingService = new RoutingMt5ConnectionService(
                mockConfig,
                simulatedService,
                realService
            );

            var profile = new ConnectionProfileDto
            {
                ProfileName = "TestProfile",
                BrokerServer = "ICMarkets-Demo",
                LoginAccountId = "7820491",
                Password = "password123"
            };

            // Act & Assert 1: Simulated Mode (First Call)
            var resultSimulated = await routingService.TestConnectionAsync(profile, CancellationToken.None);
            Assert.True(resultSimulated.IsSuccess);
            Assert.Equal("7820491", resultSimulated.AccountSnapshot?.AccountId);

            // Switch config to Real mode
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Real", TimeoutSeconds = 1 };

            // Tell the real-service account stub to fail to prove that the real service was routed and executed!
            stubAccountService.ThrowException = true;

            // Act & Assert 2: Real Mode (Second Call)
            var resultReal = await routingService.TestConnectionAsync(profile, CancellationToken.None);
            Assert.False(resultReal.IsSuccess);
            Assert.Contains("Intentional Test Exception", resultReal.ErrorMessage);
        }

        [Fact]
        public void PlaceOrderRequest_Serialization_RoundTrip()
        {
            var originalRequest = new PlaceOrderRequest("EURUSD", BridgeOrderSide.Buy, 0.50m, 1.08000m, 1.10000m, "TestComment", "correlation-id");
            var envelope = BridgeMessageEnvelope.CreateRequest("req-123", "PlaceOrder", originalRequest);

            var json = JsonSerializer.Serialize(envelope);
            var deserializedEnvelope = JsonSerializer.Deserialize<BridgeMessageEnvelope>(json);

            Assert.NotNull(deserializedEnvelope);
            Assert.Equal("Request", deserializedEnvelope.MessageType);
            Assert.Equal("req-123", deserializedEnvelope.RequestId);

            var payloadJson = JsonSerializer.Serialize(deserializedEnvelope.Payload);
            var deserializedRequest = JsonSerializer.Deserialize<PlaceOrderRequest>(payloadJson);

            Assert.NotNull(deserializedRequest);
            Assert.Equal("EURUSD", deserializedRequest.Symbol);
            Assert.Equal(BridgeOrderSide.Buy, deserializedRequest.Side);
            Assert.Equal(0.50m, deserializedRequest.Volume);
            Assert.Equal(1.08000m, deserializedRequest.StopLoss);
            Assert.Equal(1.10000m, deserializedRequest.TakeProfit);
            Assert.Equal("TestComment", deserializedRequest.Comment);
            Assert.Equal("correlation-id", deserializedRequest.ClientCorrelationId);
        }

        [Fact]
        public void PlaceOrderResponse_Serialization_RoundTrip()
        {
            var originalResponse = new PlaceOrderResponse(true, 123456, BridgeOrderExecutionStatus.Executed, "Success", "comment-text");
            var envelope = BridgeMessageEnvelope.CreateResponse("req-123", "PlaceOrder", originalResponse, null);

            var json = JsonSerializer.Serialize(envelope);
            var deserializedEnvelope = JsonSerializer.Deserialize<BridgeMessageEnvelope>(json);

            Assert.NotNull(deserializedEnvelope);

            var payloadJson = JsonSerializer.Serialize(deserializedEnvelope.Payload);
            var deserializedResponse = JsonSerializer.Deserialize<PlaceOrderResponse>(payloadJson);

            Assert.NotNull(deserializedResponse);
            Assert.True(deserializedResponse.Success);
            Assert.Equal(123456, deserializedResponse.Ticket);
            Assert.Equal(BridgeOrderExecutionStatus.Executed, deserializedResponse.Status);
            Assert.Equal("Success", deserializedResponse.BrokerMessage);
            Assert.Equal("comment-text", deserializedResponse.Comment);
        }

        [Fact]
        public void ClosePositionRequestAndResponse_Serialization_RoundTrip()
        {
            var originalRequest = new ClosePositionRequest(99882, "EURUSD", 0.10m);
            var reqJson = JsonSerializer.Serialize(originalRequest);
            var deserializedRequest = JsonSerializer.Deserialize<ClosePositionRequest>(reqJson);

            Assert.NotNull(deserializedRequest);
            Assert.Equal(99882, deserializedRequest.Ticket);
            Assert.Equal("EURUSD", deserializedRequest.Symbol);
            Assert.Equal(0.10m, deserializedRequest.Volume);

            var originalResponse = new ClosePositionResponse(true, 99882, "Closed successfully");
            var resJson = JsonSerializer.Serialize(originalResponse);
            var deserializedResponse = JsonSerializer.Deserialize<ClosePositionResponse>(resJson);

            Assert.NotNull(deserializedResponse);
            Assert.True(deserializedResponse.Success);
            Assert.Equal(99882, deserializedResponse.Ticket);
            Assert.Equal("Closed successfully", deserializedResponse.BrokerMessage);
        }

        [Fact]
        public void GetOpenPositionsResponse_Serialization_RoundTrip()
        {
            var position = new BridgePositionDto(
                ticket: 54321,
                symbol: "GBPUSD",
                side: BridgePositionSide.Sell,
                volume: 0.25m,
                openPrice: 1.25000m,
                currentPrice: 1.24800m,
                stopLoss: 1.26000m,
                takeProfit: 1.22000m,
                profit: 50.00m,
                swap: -1.20m,
                magicNumber: 987654,
                comment: "EA-Position",
                openTime: new DateTime(2025, 5, 20, 12, 0, 0, DateTimeKind.Utc)
            );

            var response = new GetOpenPositionsResponse(new List<BridgePositionDto> { position });
            var json = JsonSerializer.Serialize(response);
            var deserializedResponse = JsonSerializer.Deserialize<GetOpenPositionsResponse>(json);

            Assert.NotNull(deserializedResponse);
            Assert.Single(deserializedResponse.Positions);

            var pos = deserializedResponse.Positions[0];
            Assert.Equal(54321, pos.Ticket);
            Assert.Equal("GBPUSD", pos.Symbol);
            Assert.Equal(BridgePositionSide.Sell, pos.Side);
            Assert.Equal(0.25m, pos.Volume);
            Assert.Equal(1.25000m, pos.OpenPrice);
            Assert.Equal(1.24800m, pos.CurrentPrice);
            Assert.Equal(1.26000m, pos.StopLoss);
            Assert.Equal(1.22000m, pos.TakeProfit);
            Assert.Equal(50.00m, pos.Profit);
            Assert.Equal(-1.20m, pos.Swap);
            Assert.Equal(987654, pos.MagicNumber);
            Assert.Equal("EA-Position", pos.Comment);
            Assert.Equal(new DateTime(2025, 5, 20, 12, 0, 0, DateTimeKind.Utc), pos.OpenTime.ToUniversalTime());
        }

        [Fact]
        public async Task RoutingMt5TradeService_DelegatesCorrectly_BasedOnActiveMt5Mode()
        {
            var mockConfig = new StubAppConfigurationService();
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Simulated" };

            var simulatedService = new SimulatedMt5TradeService();
            var stubBridgeClient = new StubBridgeClient(null);
            var realService = new RealMt5BridgeAdapter(stubBridgeClient);

            var routingService = new RoutingMt5TradeService(
                mockConfig,
                simulatedService,
                realService
            );

            // Act & Assert 1: Simulated Mode (Delegates to SimulatedMt5TradeService)
            var session = new StubSession();
            var positions = await routingService.GetOpenPositionsAsync(session, CancellationToken.None);
            Assert.NotNull(positions);
            // Should have 1 seeded position
            Assert.Single(positions);
            Assert.Equal("EURUSD", positions[0].Symbol);

            // Switch to Real Mode
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Real" };

            // Act & Assert 2: Real Mode (Will throw since bridge client stub is not configured with response)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await routingService.GetOpenPositionsAsync(session, CancellationToken.None);
            });
        }

        [Fact]
        public async Task RealMt5TradingService_PlaceMarketOrder_MapsResultCorrectly()
        {
            // Arrange
            var responsePayload = new PlaceOrderResponse(
                success: true,
                ticket: 98765,
                status: BridgeOrderExecutionStatus.Executed,
                brokerMessage: "Trade Executed successfully",
                comment: "TestComment"
            );

            var envelope = BridgeMessageEnvelope.CreateResponse(
                requestId: "some-guid-order",
                command: "PlaceOrder",
                payload: responsePayload,
                error: null
            );

            var mockBridgeClient = new StubBridgeClient(envelope);
            var tradingService = new RealMt5TradingService(mockBridgeClient);

            // Act
            var result = await tradingService.PlaceMarketOrderAsync(
                symbol: "EURUSD",
                side: BridgeOrderSide.Buy,
                volume: 1.50m,
                stopLoss: 1.08000m,
                takeProfit: 1.10000m,
                comment: "TestComment",
                clientCorrelationId: "correlation-123",
                cancellationToken: CancellationToken.None
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(98765, result.Ticket);
            Assert.Equal("Executed", result.Status);
            Assert.Equal("Trade Executed successfully", result.ErrorMessage);
            Assert.Equal("TestComment", result.Comment);
        }

        [Fact]
        public async Task RealMt5TradingService_ClosePosition_MapsResultCorrectly()
        {
            // Arrange
            var responsePayload = new ClosePositionResponse(
                success: true,
                ticket: 54321,
                brokerMessage: "Position Closed"
            );

            var envelope = BridgeMessageEnvelope.CreateResponse(
                requestId: "some-guid-close",
                command: "ClosePosition",
                payload: responsePayload,
                error: null
            );

            var mockBridgeClient = new StubBridgeClient(envelope);
            var tradingService = new RealMt5TradingService(mockBridgeClient);

            // Act
            var result = await tradingService.ClosePositionAsync(
                positionTicket: 54321,
                symbol: "EURUSD",
                volume: null,
                cancellationToken: CancellationToken.None
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(54321, result.Ticket);
            Assert.Equal("Position Closed", result.ErrorMessage);
        }

        [Fact]
        public async Task RealMt5TradingService_GetOpenPositions_MapsResultCorrectly()
        {
            // Arrange
            var positionDto = new BridgePositionDto(
                ticket: 112233,
                symbol: "USDJPY",
                side: BridgePositionSide.Sell,
                volume: 0.10m,
                openPrice: 155.50m,
                currentPrice: 155.20m,
                stopLoss: 156.00m,
                takeProfit: 154.00m,
                profit: 300.00m,
                swap: 0.00m,
                magicNumber: 777,
                comment: "IndicatorSell",
                openTime: new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc)
            );

            var responsePayload = new GetOpenPositionsResponse(new List<BridgePositionDto> { positionDto });

            var envelope = BridgeMessageEnvelope.CreateResponse(
                requestId: "some-guid-positions",
                command: "GetOpenPositions",
                payload: responsePayload,
                error: null
            );

            var mockBridgeClient = new StubBridgeClient(envelope);
            var tradingService = new RealMt5TradingService(mockBridgeClient);

            // Act
            var positions = await tradingService.GetOpenPositionsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(positions);
            Assert.Single(positions);
            var pos = positions[0];
            Assert.Equal(112233, pos.Ticket);
            Assert.Equal("USDJPY", pos.Symbol);
            Assert.Equal("Sell", pos.Side);
            Assert.Equal(0.10m, pos.Volume);
            Assert.Equal(155.50m, pos.OpenPrice);
            Assert.Equal(155.20m, pos.CurrentPrice);
            Assert.Equal(156.00m, pos.StopLoss);
            Assert.Equal(154.00m, pos.TakeProfit);
            Assert.Equal(300.00m, pos.Profit);
            Assert.Equal(0.00m, pos.Swap);
            Assert.Equal(777, pos.MagicNumber);
            Assert.Equal("IndicatorSell", pos.Comment);
            Assert.Equal(new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc), pos.OpenTime);
        }

        [Fact]
        public async Task RoutingMt5TradingService_DelegatesCorrectly_BasedOnActiveMt5Mode()
        {
            // Arrange
            var mockConfig = new StubAppConfigurationService();
            // Default to Simulated mode first
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Simulated" };

            var simulatedTradeService = new SimulatedMt5TradeService();
            var simulatedTradingService = new SimulatedMt5TradingService(simulatedTradeService);

            var stubBridgeClient = new StubBridgeClient(null);
            var realTradingService = new RealMt5TradingService(stubBridgeClient);

            var routingTradingService = new RoutingMt5TradingService(
                mockConfig,
                simulatedTradingService,
                realTradingService
            );

            // Act & Assert 1: Simulated Mode
            var positions = await routingTradingService.GetOpenPositionsAsync(CancellationToken.None);
            Assert.NotNull(positions);
            Assert.Single(positions); // Default seeded simulated position is 1
            Assert.Equal("EURUSD", positions[0].Symbol);

            // Switch config to Real mode
            mockConfig.SettingsToReturn = new AppSettings { Mt5Mode = "Real" };

            // Act & Assert 2: Real Mode
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await routingTradingService.GetOpenPositionsAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task RealMt5TradingService_MapsBridgeError_ToPlaceOrderResultCorrectly()
        {
            // Arrange
            var envelope = new BridgeMessageEnvelope(
                messageType: "Response",
                requestId: "some-guid-err",
                command: "PlaceOrder",
                payload: null,
                error: new BridgeError("TRADE_REJECTED", "No money")
            );

            var mockBridgeClient = new StubBridgeClient(envelope);
            var tradingService = new RealMt5TradingService(mockBridgeClient);

            // Act
            var result = await tradingService.PlaceMarketOrderAsync(
                symbol: "EURUSD",
                side: BridgeOrderSide.Buy,
                volume: 1.0m,
                stopLoss: null,
                takeProfit: null,
                comment: "comment",
                clientCorrelationId: null,
                cancellationToken: CancellationToken.None
            );

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("TRADE_REJECTED", result.ErrorMessage);
        }

        // --- STUB CLASSES ---

        private class StubBridgeClient : IMt5BridgeClient
        {
            private readonly BridgeMessageEnvelope? _responseToReturn;

            public StubBridgeClient(BridgeMessageEnvelope? responseToReturn)
            {
                _responseToReturn = responseToReturn;
            }

            public Task ConnectAsync(CancellationToken ct) => Task.CompletedTask;
            public Task DisconnectAsync(CancellationToken ct) => Task.CompletedTask;

            public Task<BridgeMessageEnvelope> SendAsync(BridgeMessageEnvelope request, CancellationToken ct)
            {
                if (_responseToReturn == null)
                {
                    throw new InvalidOperationException("No response stubbed.");
                }
                return Task.FromResult(_responseToReturn);
            }
        }

        private class StubSession : IMt5Session
        {
            public string SessionId => "StubId";
            public GatewayConnectionStatus Status => GatewayConnectionStatus.Connected;
            public event Action<GatewayConnectionStatus>? OnStatusChanged;
            public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Dispose() { }
        }

        private class StubAccountService : IMt5AccountService
        {
            public bool ThrowException { get; set; }

            public Task<AccountSnapshotDto> GetAccountSnapshotAsync(IMt5Session session, CancellationToken cancellationToken = default)
            {
                if (ThrowException)
                {
                    throw new Exception("Intentional Test Exception");
                }
                return Task.FromResult(new AccountSnapshotDto());
            }
        }

        private class StubAppConfigurationService : IAppConfigurationService
        {
            public AppSettings SettingsToReturn { get; set; } = new AppSettings();

            public AppSettings GetSettings() => SettingsToReturn;
            public void SaveSettings(AppSettings settings) { SettingsToReturn = settings; }
        }
    }
}
