using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;
using Nexus.Application.Observability;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Workers
{
    public class MarketDataIngestionWorker : BackgroundService
    {
        private readonly IMarketDataFeed _marketDataFeed;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly ChannelWriter<Tick> _tickChannelWriter;
        private readonly ILogger<MarketDataIngestionWorker> _logger;

        public MarketDataIngestionWorker(
            IMarketDataFeed marketDataFeed,
            IMarketDataRepository marketDataRepository,
            ChannelWriter<Tick> tickChannelWriter,
            ILogger<MarketDataIngestionWorker> logger)
        {
            _marketDataFeed = marketDataFeed ?? throw new ArgumentNullException(nameof(marketDataFeed));
            _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
            _tickChannelWriter = tickChannelWriter ?? throw new ArgumentNullException(nameof(tickChannelWriter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerStartup, "Market Data Ingestion Worker is starting...");

            _marketDataFeed.OnTickReceived += OnTickReceivedAsync;

            await _marketDataFeed.StartAsync(stoppingToken);

            try
            {
                // Wait until cancellation is requested
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Market Data Ingestion Worker cancellation requested.");
            }
            finally
            {
                _marketDataFeed.OnTickReceived -= OnTickReceivedAsync;
                await _marketDataFeed.StopAsync(CancellationToken.None);
                _logger.LogStructured(LogLevel.Information, LogEventIds.WorkerShutdown, "Market Data Ingestion Worker stopped.");
            }
        }

        private async Task OnTickReceivedAsync(PriceTickEnvelope tickEnvelope)
        {
            var context = WorkflowContext.Create("MarketDataIngestion", subsystem: "Ingestion");
            context.Symbol = tickEnvelope.SymbolName;

            using var scope = _logger.BeginWorkflowScope(context);

            try
            {
                _logger.LogStructured(LogLevel.Debug, LogEventIds.MarketDataReceived,
                    "Market data received: Symbol={Symbol}, Bid={Bid}, Ask={Ask}",
                    tickEnvelope.SymbolName, tickEnvelope.Bid, tickEnvelope.Ask);

                // Convert envelope to Zero-Allocation Tick
                var symbol = new Symbol(tickEnvelope.SymbolName);
                var tick = new Tick(symbol, tickEnvelope.Timestamp, tickEnvelope.Bid, tickEnvelope.Ask);

                // 1. Persist the tick
                await _marketDataRepository.AppendTickAsync(tick);

                // 2. Queue for Strategy Dispatching
                if (!_tickChannelWriter.TryWrite(tick))
                {
                    // If channel writer is full/blocked, use async write to support backpressure
                    await _tickChannelWriter.WriteAsync(tick);
                }
            }
            catch (Exception ex)
            {
                _logger.LogStructuredError(ex, LogEventIds.MarketDataReceived,
                    "Error processing incoming tick for {Symbol}", tickEnvelope.SymbolName);
            }
        }
    }
}
