using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Ports
{
    public interface IMt5BridgeService
    {
        bool IsConnected { get; }
        bool IsAuthenticated { get; }
        string ConnectionStatusText { get; }
        double PingLatencyMs { get; }
        DateTime LastHeartbeatUtc { get; }
        string LastErrorMessage { get; }
        IReadOnlyCollection<string> SubscribedSymbols { get; }

        event Action<PriceTickEnvelope>? OnTickReceived;
        event Action<string>? OnStatusChanged;

        Task ConnectAsync(string host, int port, CancellationToken ct = default);
        Task DisconnectAsync(CancellationToken ct = default);
        Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default);
        Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken ct = default);
        Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default);
        Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default);
    }
}
