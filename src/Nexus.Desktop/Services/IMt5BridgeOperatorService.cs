using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Desktop.Services
{
    public interface IMt5BridgeOperatorService
    {
        bool IsConnected { get; }
        bool IsAuthenticated { get; }
        string ConnectionStatusText { get; }
        double PingLatencyMs { get; }
        DateTime LastHeartbeatUtc { get; }
        string LastErrorMessage { get; }
        IReadOnlyCollection<string> SubscribedSymbols { get; }

        long ProcessedTickCount { get; }
        double LastProcessingLatencyMs { get; }
        string LastProcessedSymbol { get; }
        DateTime LastProcessedTimestamp { get; }

        event Action<PriceTickEnvelope>? OnTickReceived;
        event Action<string>? OnStatusChanged;

        Task ConnectAsync(string host, int port, CancellationToken ct = default);
        Task DisconnectAsync(CancellationToken ct = default);
        Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default);
        Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken ct = default);
        Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default);
        Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default);

        PriceTickEnvelope? GetLatestTick(string symbol);
    }
}
