using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Persistence.Models;
using System.Threading.Channels;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// High-performance background writer that queues and writes AI experience records and trade 
    /// results to SQLite or PostgreSQL in batches, ensuring zero blockages on the live trading loop.
    /// </summary>
    public class ExperienceDatabaseWriter : BackgroundService, IExperienceDatabaseWriter
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExperienceDatabaseWriter> _logger;
        private readonly Channel<ExperienceRecord> _queue;

        public ExperienceDatabaseWriter(IServiceScopeFactory scopeFactory, ILogger<ExperienceDatabaseWriter> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var options = new BoundedChannelOptions(50000)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            };
            _queue = Channel.CreateBounded<ExperienceRecord>(options);
        }

        /// <summary>
        /// Instantly queues an experience snapshot from the trading loop. Zero blocking.
        /// </summary>
        public bool Enqueue(ExperienceRecord record)
        {
            return _queue.Writer.TryWrite(record);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ExperienceDatabaseWriter] Background Persistence Pipeline started.");
            var batch = new List<ExperienceRecord>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _queue.Reader.WaitToReadAsync(stoppingToken))
                    {
                        while (_queue.Reader.TryRead(out var record))
                        {
                            batch.Add(record);
                            if (batch.Count >= 100) break;
                        }

                        if (batch.Count > 0)
                        {
                            await WriteBatchToDatabaseAsync(batch);
                            batch.Clear();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ExperienceDatabaseWriter] Error occurred processing background database writer batch.");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task WriteBatchToDatabaseAsync(List<ExperienceRecord> records)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NexusDbContext>();

            try
            {
                foreach (var record in records)
                {
                    var dbModel = new ExperienceDbModel
                    {
                        Id = record.Id,
                        Symbol = record.Symbol,
                        TimestampUtc = record.TimestampUtc,
                        MarketVectorCsv = string.Join(",", record.MarketVectorFeatures.Select(f => f.ToString("F6"))),
                        ModelVersion = record.ModelVersion,
                        BuyConfidence = record.BuyConfidence,
                        SellConfidence = record.SellConfidence,
                        RiskScore = record.RiskScore,
                        MarketRegime = record.MarketRegime,
                        ExecutedAction = record.ExecutedAction,
                        RealizedPips = record.RealizedPips,
                        IsCompleted = record.IsCompleted
                    };

                    dbContext.Add(dbModel);
                }

                await dbContext.SaveChangesAsync();
                _logger.LogDebug("[ExperienceDatabaseWriter] Successfully committed {Count} Experience records to the database.", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExperienceDatabaseWriter] Failed to persist batch of {Count} records to database.", records.Count);
            }
        }
    }
}