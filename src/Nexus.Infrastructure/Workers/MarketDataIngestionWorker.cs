using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Models;
using System.Collections.Concurrent;

namespace Nexus.Infrastructure.Workers
{
    /// <summary>
    /// Background Service that buffers streamed live price ticks in a high-speed queue,
    /// and performs optimized bulk-insertions into the SQL Database every 1 second.
    /// This protects the UI and network threads from database write latency.
    /// </summary>
    public sealed class MarketDataIngestionWorker : BackgroundService
    {
        #region Private Fields
        private readonly IMt5BridgeService _bridgeService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MarketDataIngestionWorker> _logger;

        // Lock-free high-speed queue for temporary tick buffering
        private readonly ConcurrentQueue<PriceTickEnvelope> _tickBuffer = new();
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1); // Bulk write interval
        #endregion

        #region Constructor
        public MarketDataIngestionWorker(
            IMt5BridgeService bridgeService,
            IServiceScopeFactory scopeFactory,
            ILogger<MarketDataIngestionWorker> logger)
        {
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Background Worker Execution
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[MarketDataIngestionWorker] High-speed database bulk-writer started.");

            // Subscribe to the live MT5 Bridge Tick Stream
            _bridgeService.OnTickReceived += EnqueueTick;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_flushInterval, stoppingToken);
                    await FlushBufferToDatabaseAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MarketDataIngestionWorker] Error flushing tick buffers.");
                }
            }

            _bridgeService.OnTickReceived -= EnqueueTick;
            _logger.LogInformation("[MarketDataIngestionWorker] Ingestion worker stopped.");
        }

        private void EnqueueTick(PriceTickEnvelope tick)
        {
            // Thread-safe O(1) buffer append
            _tickBuffer.Enqueue(tick);
        }
        #endregion

        #region Bulk SQL Write Engine
        private async Task FlushBufferToDatabaseAsync(CancellationToken ct)
        {
            if (_tickBuffer.IsEmpty) return;

            var ticksToWrite = new List<TickDbModel>();
            while (_tickBuffer.TryDequeue(out var tick))
            {
                ticksToWrite.Add(new TickDbModel
                {
                    Symbol = tick.SymbolName,
                    TimestampUtc = tick.Timestamp.ToUniversalTime(),
                    Bid = tick.Bid,
                    Ask = tick.Ask,
                    Volume = tick.SequenceNumber // Sequence or volume mapping
                });
            }

            _logger.LogDebug("[MarketDataIngestionWorker] Flushing {Count} ticks to operational database...", ticksToWrite.Count);

            // Open an isolated, fast database scope for bulk insertion
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();

            try
            {
                // Bulk insert via EF Core (Optimized batching)
                await dbContext.Ticks.AddRangeAsync(ticksToWrite, ct);
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketDataIngestionWorker] Database bulk-insert transaction failed.");
            }
        }
        #endregion
    }
}