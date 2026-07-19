using Nexus.Core.Entities;
using System.Text.Json;

namespace Nexus.Training
{
    /// <summary>
    /// The physical file-system orchestrator for the AI Learning ecosystem.
    /// Builds the exact folder tree required for Champion/Challenger model routing,
    /// Backtests, Reinforcement Replay Buffers, and Knowledge Graphs.
    /// </summary>
    public sealed class TradingLearningPlatform
    {
        public string BaseDirectory { get; }

        public TradingLearningPlatform()
        {
            BaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NexusAI");
            InitializePlatformArchitecture();
        }

        private void InitializePlatformArchitecture()
        {
            string[] directories = new[]
            {
                "Experience",
                "Datasets/train",
                "Datasets/validation",
                "Datasets/test",
                "Datasets/benchmark",
                "Models/current",
                "Models/candidates",
                "Models/archive",
                "Models/champion",
                "Backtests",
                "Reinforcement/ReplayBuffer",
                "Memory",
                "Knowledge",
                "Statistics",
                "FeatureEngineering",
                "Checkpoints"
            };

            foreach (var dir in directories)
            {
                string fullPath = Path.Combine(BaseDirectory, dir);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
            }
        }

        /// <summary>
        /// Saves a highly contextual deep experience record into the Reinforcement Replay Buffer.
        /// This file is formatted for easy ingestion by Python ML scripts.
        /// </summary>
        public async Task SaveToReplayBufferAsync(DeepExperienceRecord record, CancellationToken ct = default)
        {
            string replayDir = Path.Combine(BaseDirectory, "Reinforcement", "ReplayBuffer");
            string fileName = $"EXP_{record.TimestampUtc:yyyyMMdd_HHmmss}_{record.Symbol}_{record.IsWin}.json";
            string filePath = Path.Combine(replayDir, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(record, options);

            // Thread-safe async file write
            await File.WriteAllTextAsync(filePath, json, ct);
        }
    }
}