using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Tests.Unit.Desktop
{
    public class Mt5BridgeOperatorTests
    {
        private class StubBridgeClient : IMt5BridgeClient
        {
            private readonly BridgeMessageEnvelope? _responseToReturn;

            public event Action<BridgeMessageEnvelope>? OnMessageReceived;

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

            public void TriggerMessage(BridgeMessageEnvelope envelope)
            {
                OnMessageReceived?.Invoke(envelope);
            }
        }

        private class StubNativeCore : Nexus.Core.Interfaces.INativeCoreService
        {
            public bool IsAvailable => false;
            public string LastError => "Stub invalid.";
            public void UpdateTick(Tick tick) { }
            public MarketVector GetMarketVector() => throw new NotImplementedException();
            public MarketState GetMarketState() => throw new NotImplementedException();
        }

        [Fact]
        public async Task Mt5BridgeOperatorService_Connect_DelegatesCorrectly()
        {
            // Arrange
            var stubClient = new StubBridgeClient(null);
            var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Mt5BridgeService>();
            var bridgeService = new Mt5BridgeService(stubClient, mockLogger);
            var pipelineLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<MarketDataPipeline>();
            var nativeCore = new StubNativeCore();
            var pipeline = new MarketDataPipeline(bridgeService, nativeCore, pipelineLogger);
            var operatorService = new Mt5BridgeOperatorService(bridgeService, pipeline);

            // Act
            await operatorService.ConnectAsync("127.0.0.1", 5000);

            // Assert
            Assert.True(operatorService.IsConnected);
            Assert.Equal("Connected (Unauthenticated)", operatorService.ConnectionStatusText);
        }

        [Fact]
        public async Task Mt5BridgeOperatorService_SubscribeSymbol_AddsToActiveList()
        {
            // Arrange
            var stubClient = new StubBridgeClient(null);
            var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Mt5BridgeService>();
            var bridgeService = new Mt5BridgeService(stubClient, mockLogger);
            var pipelineLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<MarketDataPipeline>();
            var nativeCore = new StubNativeCore();
            var pipeline = new MarketDataPipeline(bridgeService, nativeCore, pipelineLogger);
            var operatorService = new Mt5BridgeOperatorService(bridgeService, pipeline);

            // Act
            await operatorService.SubscribeSymbolAsync("EURUSD");

            // Assert
            Assert.Contains("EURUSD", operatorService.SubscribedSymbols);
        }

        [Fact]
        public async Task Mt5BridgeOperatorService_OnTickReceived_UpdatesLatestTick()
        {
            // Arrange
            var stubClient = new StubBridgeClient(null);
            var mockLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<Mt5BridgeService>();
            var bridgeService = new Mt5BridgeService(stubClient, mockLogger);
            var pipelineLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<MarketDataPipeline>();
            var nativeCore = new StubNativeCore();
            var pipeline = new MarketDataPipeline(bridgeService, nativeCore, pipelineLogger);
            var operatorService = new Mt5BridgeOperatorService(bridgeService, pipeline);

            PriceTickEnvelope? receivedTick = null;
            operatorService.OnTickReceived += tick => receivedTick = tick;

            // Prepare incoming ReceiveTickStream request from EA
            var payload = new System.Text.Json.Nodes.JsonObject
            {
                ["symbol"] = "EURUSD",
                ["timestamp"] = "2025-05-20T12:00:00Z",
                ["bid"] = 1.08500,
                ["ask"] = 1.08510,
                ["spread"] = 0.00010,
                ["volume"] = 1.0
            };
            var tickEnvelope = BridgeMessageEnvelope.CreateRequest("req-tick", "ReceiveTickStream", payload);

            // Act - simulate receiving message from TCP Client
            stubClient.TriggerMessage(tickEnvelope);

            // Assert
            Assert.NotNull(receivedTick);
            Assert.Equal("EURUSD", receivedTick.SymbolName);
            Assert.Equal(1.08500, receivedTick.Bid);
            Assert.Equal(1.08510, receivedTick.Ask);

            var latest = operatorService.GetLatestTick("EURUSD");
            Assert.NotNull(latest);
            Assert.Equal(1.08500, latest.Bid);
        }
    }
}
