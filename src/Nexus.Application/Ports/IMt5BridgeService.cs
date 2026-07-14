using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Ports
{
    public enum BridgeLifecycleState
    {
        Stopped,            // Bridge listener is offline
        Listening,          // Socket server listening on 127.0.0.1:5000 (breathing blue glow)
        SocketConnected,    // TCP socket connection established, awaiting handshake (pulsing orange)
        Handshaking,        // Handshake packet sent, waiting for EA response
        HandshakeSuccess,   // Handshake completed, EA metadata mapped (breathing green glow)
        Authenticated,      // Broker credentials verified, trade path ready
        ConnectionError     // Connection failed or timed out (warning red)
    }

    public interface IMt5BridgeService
    {
        bool IsConnected { get; }
        bool IsAuthenticated { get; }
        BridgeLifecycleState CurrentState { get; }
        string ConnectionStatusText { get; }
        double PingLatencyMs { get; }
        DateTime LastHeartbeatUtc { get; }
        string LastErrorMessage { get; }
        IReadOnlyCollection<string> SubscribedSymbols { get; }

        // EA status & file properties
        bool IsEaPresentInRepository { get; }
        long EaRepositoryFileSize { get; }
        DateTime EaRepositoryFileLastModifiedUtc { get; }
        string EaRepositoryFilePath { get; }
        bool IsEaInstalledConfirmed { get; set; }
        bool IsHandshakeSucceeded { get; }
        string EaName { get; }
        string EaVersion { get; }
        string ChartSymbol { get; }
        string HandshakeAccountId { get; }
        string HandshakeBrokerServer { get; }

        event Action<PriceTickEnvelope>? OnTickReceived;
        event Action<string>? OnStatusChanged;

        Task ConnectAsync(string host, int port, CancellationToken ct = default);
        Task DisconnectAsync(CancellationToken ct = default);
        Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default);
        Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken ct = default);
        Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default);
        Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default);
        Task<string> AutoDetectAndInstallEaAsync(CancellationToken cancellationToken);
        Task ExportEaAsync(string destinationDirectory, CancellationToken cancellationToken);
    }
}
