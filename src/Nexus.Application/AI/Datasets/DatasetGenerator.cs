using Microsoft.Extensions.Logging;
using Nexus.Application.AI.Features;
using Nexus.Application.Ports;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Interfaces;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.AI.Datasets
{
    /// <summary>
    /// Generates immutable offline datasets by merging historical market data with target labels.
    /// Separates data preparation latency from the actual TorchSharp training loop.
    /// </summary>
    public class DatasetGenerator
    {
        private readonly FeatureOrchestrator _featureOrchestrator;
        private readonly IMarketDataRepository _marketDataRepo;
        private readonly IDatasetRegistry _datasetRegistry;
        private readonly ILogger<DatasetGenerator> _logger;
        private readonly string _datasetDirectory;

        public DatasetGenerator(
            FeatureOrchestrator featureOrchestrator,
            IMarketDataRepository marketDataRepo,
            IDatasetRegistry datasetRegistry,
            ILogger<DatasetGenerator> logger)
        {
            _featureOrchestrator = featureOrchestrator ?? throw new ArgumentNullException(nameof(featureOrchestrator));
            _marketDataRepo = marketDataRepo ?? throw new ArgumentNullException(nameof(marketDataRepo));
            _datasetRegistry = datasetRegistry ?? throw new ArgumentNullException(nameof(datasetRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _datasetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI", "Datasets", "train");
            if (!Directory.Exists(_datasetDirectory))
                Directory.CreateDirectory(_datasetDirectory);
        }

        public async Task<DatasetMetadata> GenerateDatasetAsync(string symbol, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            _logger.LogInformation("Generating offline dataset for {Symbol} from {Start} to {End}", symbol, startDate, endDate);

            // 1. Fetch raw historical operational data
            var candles = await _marketDataRepo.GetCandlesAsync(symbol, "M15", startDate, endDate, ct);
            if (candles == null || candles.Count < 100)
            {
                throw new InvalidOperationException("Insufficient historical data to generate a dataset.");
            }

            string datasetId = $"DS_{symbol}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            string filePath = Path.Combine(_datasetDirectory, $"{datasetId}.jsonl"); // JSON Lines format for scalable streaming

            long sampleCount = 0;

            // 2. Iterate through history and generate Feature-Label pairs
            using (var writer = new StreamWriter(filePath, append: false))
            {
                // We stop 10 candles before the end because we need future data to compute the Label (Target Profit)
                for (int i = 50; i < candles.Count - 10; i++)
                {
                    if (ct.IsCancellationRequested) break;

                    // Extract context window
                    var contextCandles = candles.Skip(i - 50).Take(50).ToList();

                    // Generate Features using the orchestrated Store
                    var mockState = new MarketState(symbol, contextCandles[^1].Timestamp, 0.1, 0, 1, 0.5, 0.5, 0.1, 50, "Unknown");
                    double[] features = _featureOrchestrator.GenerateFeatureVector(mockState, contextCandles, new List<Tick>());

                    // LABEL ENGINEERING: Look 10 candles into the future to find the max excursion
                    double currentPrice = contextCandles[^1].Close.Value;
                    double maxFuturePrice = candles.Skip(i + 1).Take(10).Max(c => c.High.Value);
                    double minFuturePrice = candles.Skip(i + 1).Take(10).Min(c => c.Low.Value);

                    double maxBuyProfit = maxFuturePrice - currentPrice;
                    double maxSellProfit = currentPrice - minFuturePrice;

                    // Policy target: 0=Wait, 1=Buy, 2=Sell
                    int policyTarget = 0;
                    double expectedValue = 0;

                    if (maxBuyProfit > maxSellProfit && maxBuyProfit > 0.0020) // e.g. 20 pips
                    {
                        policyTarget = 1;
                        expectedValue = maxBuyProfit;
                    }
                    else if (maxSellProfit > maxBuyProfit && maxSellProfit > 0.0020)
                    {
                        policyTarget = 2;
                        expectedValue = maxSellProfit; // Directional value
                    }

                    // Serialize row
                    var sample = new
                    {
                        Timestamp = contextCandles[^1].Timestamp,
                        Features = features,
                        PolicyLabel = policyTarget,
                        ValueLabel = expectedValue
                    };

                    await writer.WriteLineAsync(JsonSerializer.Serialize(sample));
                    sampleCount++;
                }
            }

            // 3. Create Immutable Metadata Record
            var metadata = new DatasetMetadata(
                DatasetId: datasetId,
                DatasetVersion: "1.0",
                FeatureVersion: _featureOrchestrator.GetFeatureSetVersionHash(),
                LabelVersion: "MaxExcursion_10Candles_v1",
                GeneratorVersion: "1.0",
                GitCommit: "UNKNOWN",
                StartDate: startDate,
                EndDate: endDate,
                Symbols: new List<string> { symbol },
                Timeframes: new List<string> { "M15" },
                NumberOfSamples: sampleCount,
                Hash: datasetId.GetHashCode().ToString("X"), // Placeholder
                CreationTimeUtc: DateTime.UtcNow
            );

            // 4. Register inside the AI Storage DB
            await _datasetRegistry.RegisterDatasetAsync(metadata, ct);

            _logger.LogInformation("Dataset {DatasetId} generated successfully with {Count} samples.", datasetId, sampleCount);

            return metadata;
        }
    }
}