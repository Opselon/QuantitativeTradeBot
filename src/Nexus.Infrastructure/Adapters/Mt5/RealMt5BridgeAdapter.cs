using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RealMt5BridgeAdapter : IMt5AccountService
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
    }
}
