using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    public class RealMt5BridgeConnectionService : IMt5ConnectionService
    {
        private readonly IMt5BridgeClient _bridgeClient;
        private readonly IMt5AccountService _realAccountService;

        public RealMt5BridgeConnectionService(IMt5BridgeClient bridgeClient, IMt5AccountService realAccountService)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
            _realAccountService = realAccountService ?? throw new ArgumentNullException(nameof(realAccountService));
        }

        public async Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            try
            {
                // Start TCP bridge server listener
                await _bridgeClient.ConnectAsync(cancellationToken);

                Console.WriteLine("[RealMt5BridgeConnectionService] Waiting for MT5 EA connection...");

                // Poll for connection status (IsConnected) up to the configured profile timeout or 15 seconds
                int pollLimit = Math.Max(5, Math.Min(profile.TimeoutSeconds, 15));
                bool isConnected = false;

                // Let's check TcpMt5BridgeClient connection status dynamically
                if (_bridgeClient is TcpMt5BridgeClient tcpClient)
                {
                    for (int i = 0; i < pollLimit * 2; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (tcpClient.IsConnected)
                        {
                            isConnected = true;
                            break;
                        }

                        await Task.Delay(500, cancellationToken);
                    }
                }
                else
                {
                    // Generic fallback delay
                    await Task.Delay(1000, cancellationToken);
                    isConnected = true; // assume active for mock/unit tests if needed
                }

                if (!isConnected)
                {
                    return new ConnectionTestResultDto
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Connection Failed: Timed out waiting for MetaTrader 5 EA to connect to bridge on port. " +
                                       $"Ensure 'NexusBridge.mq5' is running on your MT5 terminal chart and configured correctly."
                    };
                }

                // Create a temporary session to execute the test query
                using var session = new RealMt5BridgeSession(_bridgeClient);
                var snapshot = await _realAccountService.GetAccountSnapshotAsync(session, cancellationToken);

                return new ConnectionTestResultDto
                {
                    IsSuccess = true,
                    AccountSnapshot = snapshot
                };
            }
            catch (Exception ex)
            {
                return new ConnectionTestResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = $"Connection Failed: {ex.Message}"
                };
            }
        }

        public async Task<IMt5Session> CreateSessionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            var session = new RealMt5BridgeSession(_bridgeClient);
            await session.ConnectAsync(cancellationToken);
            return session;
        }
    }
}
