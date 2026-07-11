using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
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
