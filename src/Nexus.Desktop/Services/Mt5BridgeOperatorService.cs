using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Desktop.Services
{
    public class Mt5BridgeOperatorService : IMt5BridgeOperatorService, IDisposable
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly MarketDataPipeline _pipeline;
        private readonly ConcurrentDictionary<string, PriceTickEnvelope> _latestTicks = new(StringComparer.OrdinalIgnoreCase);

        public bool IsConnected => _bridgeService.IsConnected;
        public bool IsAuthenticated => _bridgeService.IsAuthenticated;
        public string ConnectionStatusText => _bridgeService.ConnectionStatusText;
        public double PingLatencyMs => _bridgeService.PingLatencyMs;
        public DateTime LastHeartbeatUtc => _bridgeService.LastHeartbeatUtc;
        public string LastErrorMessage => _bridgeService.LastErrorMessage;
        public IReadOnlyCollection<string> SubscribedSymbols => _bridgeService.SubscribedSymbols;

        public long ProcessedTickCount => _pipeline.ProcessedTickCount;
        public double LastProcessingLatencyMs => _pipeline.LastProcessingLatencyMs;
        public string LastProcessedSymbol => _pipeline.LastProcessedSymbol;
        public DateTime LastProcessedTimestamp => _pipeline.LastProcessedTimestamp;

        public event Action<PriceTickEnvelope>? OnTickReceived;
        public event Action<string>? OnStatusChanged;

        public Mt5BridgeOperatorService(IMt5BridgeService bridgeService, MarketDataPipeline pipeline)
        {
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

            _bridgeService.OnTickReceived += HandleOnTickReceived;
            _bridgeService.OnStatusChanged += HandleOnStatusChanged;
        }

        private void HandleOnTickReceived(PriceTickEnvelope tick)
        {
            if (tick == null) return;
            _latestTicks[tick.SymbolName] = tick;
            OnTickReceived?.Invoke(tick);
        }

        private void HandleOnStatusChanged(string status)
        {
            OnStatusChanged?.Invoke(status);
        }

        public Task ConnectAsync(string host, int port, CancellationToken ct = default)
        {
            return _bridgeService.ConnectAsync(host, port, ct);
        }

        public Task DisconnectAsync(CancellationToken ct = default)
        {
            return _bridgeService.DisconnectAsync(ct);
        }

        public Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default)
        {
            return _bridgeService.LoginAsync(accountId, password, brokerServer, ct);
        }

        public Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken ct = default)
        {
            return _bridgeService.GetAccountSnapshotAsync(ct);
        }

        public Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default)
        {
            return _bridgeService.SubscribeSymbolAsync(symbol, ct);
        }

        public Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default)
        {
            return _bridgeService.UnsubscribeSymbolAsync(symbol, ct);
        }

        public PriceTickEnvelope? GetLatestTick(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return null;
            return _latestTicks.TryGetValue(symbol, out var tick) ? tick : null;
        }

        public void Dispose()
        {
            _bridgeService.OnTickReceived -= HandleOnTickReceived;
            _bridgeService.OnStatusChanged -= HandleOnStatusChanged;
        }
    }
}
