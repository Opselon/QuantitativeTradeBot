// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Deep Learning Backend)
// FILE:    TorchTrainingPipeline.cs
// DESCRIPTION: Institutional-grade TorchSharp implementation of ITrainingPipeline.
//              Reads directly from zero-copy .dat files and trains the TFT model.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TorchSharp;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.TorchSharp.Models;
using static TorchSharp.torch;

namespace Nexus.Infrastructure.TorchSharp.Training
{
    /// <summary>
    /// Binary memory-aligned structure representing a single timeframe market row.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MarketKnowledgeRow
    {
        public long TimestampUnixNano;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
        public float[] Features;
        public float TargetReturn;
        public float TargetRisk;
        public float TargetDrawdown;
    }

    public class TorchTrainingPipeline : ITrainingPipeline
    {
        private const int SequenceLength = 30;
        private const int FeatureCount = 33;

        public async Task<ModelMetadata> ExecuteTrainingAsync(
            string experimentId,
            DatasetMetadata dataset,
            IExperimentTracker tracker,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                string datFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI", "Data", "Knowledge", $"{dataset.DatasetId}.dat");

                if (!File.Exists(datFilePath))
                {
                    throw new FileNotFoundException($"Knowledge dataset .dat file not found at {datFilePath}");
                }

                // 1. ZERO-COPY MEMORY MAPPED READER
                var fileInfo = new FileInfo(datFilePath);
                int rowSize = Marshal.SizeOf<MarketKnowledgeRow>();
                long totalRows = fileInfo.Length / rowSize;
                int samplesCount = (int)totalRows - SequenceLength;

                if (samplesCount <= 0)
                {
                    throw new InvalidOperationException("Dataset .dat file contains insufficient rows for temporal sequences.");
                }

                using (var scope = NewDisposeScope())
                {
                    // Pre-allocate flat arrays for tensor mapping
                    float[] flatFeatures = new float[samplesCount * SequenceLength * FeatureCount];
                    float[] flatTargets = new float[samplesCount * 7];
                    float[] flatOffsets = new float[samplesCount];

                    // Read binary data via MemoryMappedFile
                    using (var mmf = MemoryMappedFile.CreateFromFile(datFilePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read))
                    using (var accessor = mmf.CreateViewAccessor(0, fileInfo.Length, MemoryMappedFileAccess.Read))
                    {
                        MarketKnowledgeRow[] rows = new MarketKnowledgeRow[totalRows];
                        for (long i = 0; i < totalRows; i++)
                        {
                            accessor.Read(i * rowSize, out rows[i]);
                        }

                        // Map into chronological sequences
                        for (int i = 0; i < samplesCount; i++)
                        {
                            for (int seq = 0; seq < SequenceLength; seq++)
                            {
                                int targetIdx = i + seq;
                                for (int f = 0; f < FeatureCount; f++)
                                {
                                    flatFeatures[i * (SequenceLength * FeatureCount) + seq * FeatureCount + f] = rows[targetIdx].Features[f];
                                }
                            }

                            double closeReturn = rows[i + SequenceLength].TargetReturn;
                            flatTargets[i * 7 + 0] = closeReturn > 0.0005 ? 1.0f : 0.0f; // BUY
                            flatTargets[i * 7 + 1] = closeReturn < -0.0005 ? 1.0f : 0.0f; // SELL
                            flatTargets[i * 7 + 2] = Math.Abs(closeReturn) <= 0.0005 ? 1.0f : 0.0f; // NO_TRADE
                            flatTargets[i * 7 + 3] = (float)closeReturn; // Expected Return
                            flatTargets[i * 7 + 4] = rows[i + SequenceLength].TargetRisk; // Risk
                            flatTargets[i * 7 + 5] = rows[i + SequenceLength].TargetDrawdown; // MAE
                            flatTargets[i * 7 + 6] = 0.5f; // Confidence

                            flatOffsets[i] = (float)(samplesCount - i) / samplesCount; // Time decay
                        }
                    }

                    // 2. TENSOR INITIALIZATION
                    var inputTensor = tensor(flatFeatures, new long[] { samplesCount, SequenceLength, FeatureCount });
                    var targetTensor = tensor(flatTargets, new long[] { samplesCount, 7 });
                    var offsetsTensor = tensor(flatOffsets, new long[] { samplesCount });

                    var model = new TemporalFusionTransformer(FeatureCount, 1, 64, 0.1);
                    model.train();
                    var optimizer = torch.optim.Adam(model.parameters(), lr: 0.001);

                    int batchSize = 128;
                    int totalBatches = (int)Math.Ceiling((double)samplesCount / batchSize);
                    float finalLoss = 1.0f;

                    // 3. BACKPROPAGATION LOOP
                    for (int epoch = 1; epoch <= 5; epoch++)
                    {
                        ct.ThrowIfCancellationRequested();
                        float epochLossSum = 0;

                        for (int batchIdx = 0; batchIdx < totalBatches; batchIdx++)
                        {
                            int startIdx = batchIdx * batchSize;
                            int currentBatchSize = Math.Min(batchSize, samplesCount - startIdx);

                            using (var batchScope = NewDisposeScope())
                            {
                                optimizer.zero_grad();

                                var batchInput = inputTensor.narrow(0, startIdx, currentBatchSize);
                                var batchTarget = targetTensor.narrow(0, startIdx, currentBatchSize);
                                var batchOffsets = offsetsTensor.narrow(0, startIdx, currentBatchSize);

                                var prediction = model.forward(batchInput);
                                var loss = model.CalculateExponentialDecayLoss(prediction, batchTarget, batchOffsets, 0.5);

                                loss.backward();
                                optimizer.step();

                                epochLossSum += loss.data<float>()[0];
                            }
                        }
                        finalLoss = epochLossSum / totalBatches;
                    }

                    // 4. CHECKPOINT SERIALIZATION
                    string checkpointsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI", "Checkpoints");
                    Directory.CreateDirectory(checkpointsDir);
                    string checkpointPath = Path.Combine(checkpointsDir, $"{experimentId}.dat");
                    model.save(checkpointPath);

                    return new ModelMetadata(
                        ModelId: experimentId,
                        ArchitectureType: "TemporalFusionTransformer",
                        Backend: Core.AI.Enums.ExecutionBackend.TorchSharp,
                        DatasetId: dataset.DatasetId,
                        ExperimentId: experimentId,
                        FeatureVersion: "1.0",
                        LabelVersion: "1.0",
                        Status: Core.AI.Enums.ModelStatus.Candidate,
                        CheckpointPath: checkpointPath,
                        CreatedAtUtc: DateTime.UtcNow,
                        GitCommit: "HEAD"
                    );
                }
            }, ct);
        }
    }
}