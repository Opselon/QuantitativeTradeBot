using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Mt5Bridge
{
    public class MarketDataPipeline : IDisposable
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly INativeCoreService _nativeCore;
        private readonly ILogger<MarketDataPipeline> _logger;

        private long _processedTickCount;
        private double _lastProcessingLatencyMs;
        private string _lastProcessedSymbol = "None";
        private DateTime _lastProcessedTimestamp = DateTime.MinValue;
        private PriceTickEnvelope? _lastProcessedTick;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, PriceTickEnvelope> _latestTicks = new(StringComparer.OrdinalIgnoreCase);

        public long ProcessedTickCount => _processedTickCount;
        public double LastProcessingLatencyMs => _lastProcessingLatencyMs;
        public string LastProcessedSymbol => _lastProcessedSymbol;
        public DateTime LastProcessedTimestamp => _lastProcessedTimestamp;
        public PriceTickEnvelope? LastProcessedTick => _lastProcessedTick;

        public System.Collections.Generic.IReadOnlyCollection<PriceTickEnvelope> LatestTicks => (System.Collections.Generic.IReadOnlyCollection<PriceTickEnvelope>)_latestTicks.Values;
        public PriceTickEnvelope? GetLatestTick(string symbol) => _latestTicks.TryGetValue(symbol, out var t) ? t : null;

        public event Action<PriceTickEnvelope>? OnPipelineTickProcessed;

        public MarketDataPipeline(
            IMt5BridgeService bridgeService,
            INativeCoreService nativeCore,
            ILogger<MarketDataPipeline> logger)
        {
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
            _nativeCore = nativeCore ?? throw new ArgumentNullException(nameof(nativeCore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _bridgeService.OnTickReceived += ProcessIncomingTick;
        }

        private void ProcessIncomingTick(PriceTickEnvelope envelope)
        {
            if (envelope == null) return;

            var sw = Stopwatch.StartNew();
            try
            {
                // 1. Normalization & Validation
                if (string.IsNullOrWhiteSpace(envelope.SymbolName))
                {
                    _logger.LogWarning("[MarketDataPipeline] Received tick with empty symbol. Dropped.");
                    return;
                }

                if (envelope.Bid <= 0 || envelope.Ask <= 0)
                {
                    _logger.LogWarning("[MarketDataPipeline] Received tick with invalid prices (Bid={Bid}, Ask={Ask}). Dropped.", envelope.Bid, envelope.Ask);
                    return;
                }

                // Ensure consistent UTC timestamping
                DateTime timestamp = envelope.Timestamp.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(envelope.Timestamp, DateTimeKind.Utc)
                    : envelope.Timestamp.ToUniversalTime();

                // 2. Conversion to Domain Tick
                var symbol = new Symbol(envelope.SymbolName);
                var tick = new Tick(symbol, timestamp, envelope.Bid, envelope.Ask);

                // 3. Ingestion into Native Core
                if (_nativeCore.IsAvailable)
                {
                    _nativeCore.UpdateTick(tick);
                }
                else
                {
                    // Fallback log or handle gracefully
                    _logger.LogDebug("[MarketDataPipeline] Native core unavailable, bypassing update tick interop call.");
                }

                _latestTicks[envelope.SymbolName] = envelope;

                sw.Stop();

                // 4. Update Telemetry Metrics
                _processedTickCount++;
                _lastProcessingLatencyMs = sw.Elapsed.TotalMilliseconds;
                _lastProcessedSymbol = envelope.SymbolName;
                _lastProcessedTimestamp = DateTime.UtcNow;
                _lastProcessedTick = envelope;

                _logger.LogDebug("[MarketDataPipeline] Pipeline Tick Ingested: Symbol={Symbol}, Latency={Latency:F4}ms", envelope.SymbolName, _lastProcessingLatencyMs);

                // Trigger downstream pipeline notification (e.g. for WPF real-time updates)
                OnPipelineTickProcessed?.Invoke(envelope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketDataPipeline] Critical error processing tick for symbol '{Symbol}'", envelope.SymbolName);
            }
        }

        public void Dispose()
        {
            _bridgeService.OnTickReceived -= ProcessIncomingTick;
        }
    }
}
