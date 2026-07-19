using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.Entities;
using Nexus.Training;
using System.IO;

namespace Nexus.Tests.Unit.Training
{
    public sealed class TrainingEngineTests : IDisposable
    {
        private readonly string _tempModelDir;

        public TrainingEngineTests()
        {
            _tempModelDir = Path.Combine(Path.GetTempPath(), "nexus_unit_test_models_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(_tempModelDir))
            {
                Directory.CreateDirectory(_tempModelDir);
            }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempModelDir))
                {
                    Directory.Delete(_tempModelDir, recursive: true);
                }
            }
            catch
            {
                // Suppress disposal errors on file locking
            }
        }

        private ExperienceSample CreateTestSample(
            string symbol = "EURUSD",
            TimeframeInterval timeframe = TimeframeInterval.H1,
            DecisionAction action = DecisionAction.BUY,
            double entryPrice = 1.1000,
            string regime = "Trend Bullish")
        {
            var state = new MarketState(
                symbol,
                DateTime.UtcNow,
                0.0015,
                1.1000,
                0.95,
                0.5,
                0.5,
                0.1,
                55.0,
                regime
            );

            var vector = new float[64];
            vector[0] = 1.0f; // Momentum
            vector[1] = 0.5f; // Volatility

            var decision = new TradeDecision(action, 0.1, "Test decision", DateTime.UtcNow);

            return new ExperienceSample(
                symbol,
                timeframe,
                state,
                vector,
                decision,
                entryPrice,
                regime
            );
        }

        [Fact]
        public void ExperienceEngine_CreateAndFinalize_PopulatesCorrectFields()
        {
            // Arrange
            var engine = new ExperienceEngine();
            var state = new MarketState("GBPUSD", DateTime.UtcNow, 0.0020, 1.3000, 0.90, 0.4, 0.6, 0.2, 45.0, "Trend Bearish");
            var features = new float[64];
            var decision = new TradeDecision(DecisionAction.SELL, 0.2, "Bearish break", DateTime.UtcNow);

            // Act
            var sample = engine.CreateExperience(
                "GBPUSD",
                TimeframeInterval.M15,
                state,
                features,
                decision,
                1.3000,
                0.85,
                "Strong momentum cross",
                1.5,
                3.0
            );

            // Assert
            Assert.Equal("GBPUSD", sample.Symbol);
            Assert.Equal(TimeframeInterval.M15, sample.Timeframe);
            Assert.Equal(1.3000, sample.EntryPrice);
            Assert.Equal(0.85, sample.Confidence);
            Assert.Equal("Strong momentum cross", sample.ReasoningMetadata);
            Assert.Equal(1.5, sample.Risk);
            Assert.Equal(3.0, sample.Reward);

            // Act - Finalize
            engine.FinalizeExperienceOutcome(sample, 1.2950, 2.5, 45.0, 50.0, "None");

            // Assert outcome
            Assert.Equal(1.2950, sample.ExitPrice);
            Assert.Equal(2.5, sample.MaxDrawdown);
            Assert.Equal(45.0, sample.HoldingTimeMinutes);
            Assert.Equal(50.0, sample.Result);
            Assert.Equal("None", sample.MistakeClassification);
        }

        [Fact]
        public void ExperienceReplayBuffer_FIFO_EvictsCorrectly()
        {
            // Arrange
            var buffer = new ExperienceReplayBuffer(3);
            var sample1 = CreateTestSample();
            var sample2 = CreateTestSample();
            var sample3 = CreateTestSample();
            var sample4 = CreateTestSample();

            // Act
            buffer.Add(sample1);
            buffer.Add(sample2);
            buffer.Add(sample3);

            Assert.Equal(3, buffer.Count);

            buffer.Add(sample4); // Triggers eviction of sample1

            // Assert
            Assert.Equal(3, buffer.Count);
            var all = buffer.GetAllSamples();
            Assert.DoesNotContain(sample1, all);
            Assert.Contains(sample2, all);
            Assert.Contains(sample3, all);
            Assert.Contains(sample4, all);
        }

        [Fact]
        public void ExperienceReplayBuffer_Sampling_ReturnsCorrectSubsets()
        {
            // Arrange
            var buffer = new ExperienceReplayBuffer(10);
            var sampleBullish1 = CreateTestSample(regime: "Trend Bullish");
            var sampleBullish2 = CreateTestSample(regime: "Trend Bullish");
            var sampleRanging = CreateTestSample(regime: "Ranging Range");

            buffer.AddRange(new[] { sampleBullish1, sampleBullish2, sampleRanging });

            // Act - Random sampling
            var batch = buffer.GetRandomSamples(2);
            Assert.Equal(2, batch.Count);

            // Act - Regime sampling
            var bullishBatch = buffer.GetRegimeBasedSamples("Trend Bullish", 5);
            Assert.Equal(2, bullishBatch.Count);
            Assert.All(bullishBatch, x => Assert.Equal("Trend Bullish", x.MarketRegimeLabel));

            var rangingBatch = buffer.GetRegimeBasedSamples("Ranging Range", 5);
            Assert.Single(rangingBatch);

            // Act - Time sampling
            var timeBatch = buffer.GetTimeBasedSamples(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
            Assert.Equal(3, timeBatch.Count);
        }

        [Fact]
        public void RewardEvaluator_CalculatesComplexMultiDimensionalRewards()
        {
            // Arrange
            var evaluator = new RewardEvaluator();

            // Setup a very good BUY trade
            var goodSample = CreateTestSample(action: DecisionAction.BUY, entryPrice: 1.1000, regime: "Trend Bullish");
            goodSample.Confidence = 0.85;
            goodSample.Risk = 1.0;
            goodSample.Reward = 2.0;
            goodSample.ExitPrice = 1.1050; // 50 pips in direction
            goodSample.Result = 5.0;       // Positive P&L representation
            goodSample.MaxDrawdown = 0.5;  // Very low drawdown
            goodSample.HoldingTimeMinutes = 30.0;

            // Setup a bad risk trade (ignored uncertainty & high risk)
            var recklessSample = CreateTestSample(action: DecisionAction.BUY, entryPrice: 1.1000, regime: "Ranging Noise");
            recklessSample.Confidence = 0.40; // Extremely low confidence
            recklessSample.Risk = 4.0;       // Exceeded limit (> 3.0)
            recklessSample.ExitPrice = 1.0900;
            recklessSample.Result = -8.0;
            recklessSample.MaxDrawdown = 10.0; // High drawdown
            recklessSample.HoldingTimeMinutes = 240.0;

            // Act
            var goodReward = evaluator.Evaluate(goodSample);
            var badReward = evaluator.Evaluate(recklessSample);

            // Assert
            Assert.True(goodReward.TotalReward > 10.0, $"Expected high positive reward, got: {goodReward}");
            Assert.True(goodReward.ProfitComponent > 0);
            Assert.True(goodReward.PredictionAccuracyComponent > 0);
            Assert.True(goodReward.DrawdownPenalty <= 0); // DD penalties are <= 0

            Assert.True(badReward.TotalReward < -20.0, $"Expected high penalty negative reward, got: {badReward}");
            Assert.True(badReward.ProfitComponent < 0);
            Assert.True(badReward.RiskManagementPenalty < 0);
            Assert.True(badReward.UncertaintyPenalty < 0);
            Assert.True(badReward.DrawdownPenalty < -10.0);
        }

        [Fact]
        public void ModelRegistry_LifecycleTransitions_EnforceActiveExclusivity()
        {
            // Arrange
            var registry = new ModelRegistry();
            var model1 = new ModelVersionInfo("v1.0", "dataset_v1");
            var model2 = new ModelVersionInfo("v1.1", "dataset_v1");

            // Act
            registry.RegisterModel(model1);
            registry.RegisterModel(model2);

            Assert.Equal(2, registry.GetAllModels().Count);

            // Transition model1 to Active
            registry.UpdateStatus("v1.0", TrainingModelStatus.Active);
            Assert.Equal(TrainingModelStatus.Active, registry.GetModel("v1.0")!.Status);
            Assert.Equal("v1.0", registry.GetActiveModel()!.Version);

            // Transition model2 to Active -> model1 must become Approved
            registry.UpdateStatus("v1.1", TrainingModelStatus.Active);
            Assert.Equal(TrainingModelStatus.Approved, registry.GetModel("v1.0")!.Status);
            Assert.Equal(TrainingModelStatus.Active, registry.GetModel("v1.1")!.Status);
            Assert.Equal("v1.1", registry.GetActiveModel()!.Version);
        }

        [Fact]
        public async Task FileModelStorage_SavesAndLoadsCorrectly_EnforcesPathTraversalBoundaries()
        {
            // Arrange
            var storage = new FileModelStorage(_tempModelDir);
            byte[] weights = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Act
            await storage.SaveModelAsync("v1.0_weights.bin", weights, "BINARY");
            Assert.True(storage.ModelExists("v1.0_weights.bin"));

            byte[] loaded = await storage.LoadModelAsync("v1.0_weights.bin");

            // Assert
            Assert.Equal(weights, loaded);

            // Assert traversal security
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                storage.SaveModelAsync("../traversal.bin", weights, "BINARY"));
        }

        [Fact]
        public void TimeframeLearningManager_PartitionsSeparately()
        {
            // Arrange
            var manager = new TimeframeLearningManager();
            var scalpingSample = CreateTestSample(timeframe: TimeframeInterval.M5);
            scalpingSample.Result = 2.0;

            var swingSample = CreateTestSample(timeframe: TimeframeInterval.D1);
            swingSample.Result = -4.0;

            // Act
            manager.RegisterExperience(scalpingSample);
            manager.RegisterExperience(swingSample);

            // Assert
            var scalpingDataset = manager.GetDataset(TimeframeLearningCategory.Scalping);
            var swingDataset = manager.GetDataset(TimeframeLearningCategory.Swing);
            var intradayDataset = manager.GetDataset(TimeframeLearningCategory.Intraday);

            Assert.Single(scalpingDataset);
            Assert.Single(swingDataset);
            Assert.Empty(intradayDataset);

            // Metrics are separated
            var scalpingMetrics = manager.GetMetrics(TimeframeLearningCategory.Scalping);
            var swingMetrics = manager.GetMetrics(TimeframeLearningCategory.Swing);

            Assert.Equal(1, scalpingMetrics.TotalSamples);
            Assert.Equal(1.0, scalpingMetrics.WinRate);

            Assert.Equal(1, swingMetrics.TotalSamples);
            Assert.Equal(0.0, swingMetrics.WinRate);
        }

        [Fact]
        public async Task ValidationEngine_EvaluatesAllGates_PassesGoodModel()
        {
            // Arrange
            var engine = new ValidationEngine();
            var model = new ModelVersionInfo("v1.0", "dataset_v1");

            // Create a series of profitable experiences for validation data
            var list = new List<ExperienceSample>();
            for (int i = 0; i < 10; i++)
            {
                var sample = CreateTestSample(entryPrice: 1.1000);
                sample.Confidence = 0.85;
                sample.Risk = 1.0;
                sample.ExitPrice = 1.1040; // Profitable
                sample.Result = 4.0;
                sample.MaxDrawdown = 1.0; // Under safe limit
                list.Add(sample);
            }

            // Act
            var result = await engine.ValidateModelAsync(model, list);

            // Assert
            Assert.True(result.IsApproved, $"Validation failed: {result.FailureReason}");
            Assert.True(result.PassedBacktest);
            Assert.True(result.PassedWalkForward);
            Assert.True(result.PassedOutOfSample);
            Assert.True(result.PassedPaperTrading);
        }

        [Fact]
        public async Task ValidationEngine_RejectsUnprofitableOrRiskyModel()
        {
            // Arrange
            var engine = new ValidationEngine();
            var model = new ModelVersionInfo("v1.0-Unsafe", "dataset_v1");

            // Create risky or losing trades
            var list = new List<ExperienceSample>();
            for (int i = 0; i < 6; i++)
            {
                var sample = CreateTestSample();
                sample.Confidence = 0.50;
                sample.Risk = 4.5; // High risk
                sample.Result = -8.0; // Chronic loser
                sample.MaxDrawdown = 16.0; // Exceeded DD limit (15.0)
                list.Add(sample);
            }

            // Act
            var result = await engine.ValidateModelAsync(model, list);

            // Assert
            Assert.False(result.IsApproved);
            Assert.Contains("failure", result.FailureReason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TrainingPipeline_ExecutesFullEndToEndLearningCycle()
        {
            // Arrange
            var registry = new ModelRegistry();
            var storage = new FileModelStorage(_tempModelDir);
            var validation = new ValidationEngine();
            var learningManager = new TimeframeLearningManager();

            // Pre-populate learning manager with sufficient validation experiences
            for (int i = 0; i < 12; i++)
            {
                var sample = CreateTestSample(timeframe: TimeframeInterval.M15);
                sample.Confidence = 0.80;
                sample.Risk = 1.0;
                sample.ExitPrice = 1.1030;
                sample.Result = 3.0;
                sample.MaxDrawdown = 1.0;
                learningManager.RegisterExperience(sample);
            }

            // Use the manual fake service scope factory
            var fakeScopeFactory = new FakeServiceScopeFactory();
            var pipeline = new TrainingPipeline(registry, storage, validation, learningManager, fakeScopeFactory);

            // Act
            var trainedModel = await pipeline.ExecuteTrainingCycleAsync(
                TimeframeLearningCategory.Scalping,
                "ScalpingAI_v1.0",
                "dataset_v1.0"
            );

            // Assert
            Assert.NotNull(trainedModel);
            Assert.Equal(TrainingModelStatus.Active, trainedModel.Status);
            Assert.True(trainedModel.ValidationScore > 0);
            Assert.True(storage.ModelExists("ScalpingAI_v1.0"));

            var activeModel = registry.GetActiveModel();
            Assert.NotNull(activeModel);
            Assert.Equal("ScalpingAI_v1.0", activeModel.Version);

            Assert.NotEmpty(pipeline.PipelineLogs);
            Assert.Contains(pipeline.PipelineLogs, x => x.Contains("[APPROVED]"));
            Assert.Contains(pipeline.PipelineLogs, x => x.Contains("[PROMOTED]"));
        }
    }

    // --- MANUAL FAKES TO REPLACE MOQ ---

    internal class FakeServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope();
    }

    internal class FakeServiceScope : IServiceScope
    {
        public IServiceProvider ServiceProvider => new FakeServiceProvider();
        public void Dispose() { }
    }

    internal class FakeServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}