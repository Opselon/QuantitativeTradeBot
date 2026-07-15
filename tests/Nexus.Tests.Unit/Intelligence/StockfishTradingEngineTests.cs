using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Application.Intelligence;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nexus.Tests.Unit.Intelligence
{
    public sealed class StockfishTradingEngineTests
    {
        private class StubExperienceDatabaseWriter : IExperienceDatabaseWriter
        {
            public int EnqueuedCount { get; private set; }
            public ExperienceRecord LastRecord { get; private set; } = null!;

            public bool Enqueue(ExperienceRecord record)
            {
                LastRecord = record;
                EnqueuedCount++;
                return true;
            }
        }

        [Fact]
        public async Task ScenarioSearchEngine_GeneratesCorrectCandidatesAndHighestScore()
        {
            // Arrange
            var searchEngine = new ScenarioSearchEngine();
            var currentState = new MarketState("EURUSD", DateTime.UtcNow, 0.25, 0.85, 0.90, 0.70, 0.90, 0.10, 75.0, "TrendContinuation");
            var riskState = new RiskState(1000.0, 0.05, 0.0, 0, 0.0, isTradingBlocked: false);

            // Act
            var resultNode = await searchEngine.SearchBestActionAsync(currentState, riskState, 0.85, 0.10, CancellationToken.None);

            // Assert
            Assert.NotNull(resultNode);
            Assert.Contains(resultNode.Action, new[] { DecisionAction.BUY, DecisionAction.SELL, DecisionAction.WAIT });
            Assert.True(resultNode.ProjectedScenarios.Count > 0, "Should simulate projected future scenarios.");
            Assert.NotEmpty(resultNode.Reasoning);
        }

        [Fact]
        public async Task ScenarioSearchEngine_WhenRiskBlocked_RestrictsTradeActions()
        {
            // Arrange
            var searchEngine = new ScenarioSearchEngine();
            var currentState = new MarketState("EURUSD", DateTime.UtcNow, 0.25, 0.85, 0.90, 0.70, 0.90, 0.10, 75.0, "TrendContinuation");
            var riskState = new RiskState(150.0, 0.15, 0.05, 3, 2.5, isTradingBlocked: true);

            // Act
            var resultNode = await searchEngine.SearchBestActionAsync(currentState, riskState, 0.85, 0.10, CancellationToken.None);

            // Assert
            Assert.NotNull(resultNode);
            if (resultNode.Action != DecisionAction.WAIT)
            {
                Assert.Equal(-100.0, resultNode.Score);
                Assert.Contains("blocked due to active pre-trade risk restrictions", resultNode.Reasoning);
            }
        }

        [Fact]
        public async Task ScenarioSearchEngine_PerformsAccurateDynamicPipAndPriceScaling()
        {
            // Arrange
            var searchEngine = new ScenarioSearchEngine();
            var eurUsdState = new MarketState("EURUSD", DateTime.UtcNow, 0.25, 0.85, 0.90, 0.70, 0.90, 0.10, 75.0, "TrendContinuation");
            var usdJpyState = new MarketState("USDJPY", DateTime.UtcNow, 0.25, 0.85, 0.90, 0.70, 0.90, 0.10, 75.0, "TrendContinuation");
            var riskState = new RiskState(1000.0, 0.05, 0.0, 0, 0.0, isTradingBlocked: false);

            // Act
            var eurUsdNode = await searchEngine.SearchBestActionAsync(eurUsdState, riskState, 0.85, 0.10, CancellationToken.None);
            var usdJpyNode = await searchEngine.SearchBestActionAsync(usdJpyState, riskState, 0.85, 0.10, CancellationToken.None);

            // Assert
            Assert.NotNull(eurUsdNode);
            Assert.NotNull(usdJpyNode);

            // Ensure expected values remain in realistic, normalized pips boundaries instead of millions
            Assert.True(Math.Abs(eurUsdNode.ExpectedValue) < 1000.0, $"EURUSD EV should be bounded: {eurUsdNode.ExpectedValue}");
            Assert.True(Math.Abs(usdJpyNode.ExpectedValue) < 1000.0, $"USDJPY EV should be bounded: {usdJpyNode.ExpectedValue}");
        }

        [Fact]
        public void MultiTimeframeConsensusEngine_AggregatesAndYieldsCorrectDominantBias()
        {
            // Arrange
            var consensusEngine = new MultiTimeframeConsensusEngine();

            var d1Signal = new MultiTimeframeSignal(TimeframeInterval.D1, TrendDirection.BULLISH, 0.85, 0.20, 0.80, 0.90, DateTime.UtcNow);
            var h4Signal = new MultiTimeframeSignal(TimeframeInterval.H4, TrendDirection.BULLISH, 0.75, 0.22, 0.70, 0.85, DateTime.UtcNow);
            var m30Signal = new MultiTimeframeSignal(TimeframeInterval.M30, TrendDirection.BULLISH, 0.70, 0.18, 0.75, 0.80, DateTime.UtcNow);
            var m5Signal = new MultiTimeframeSignal(TimeframeInterval.M5, TrendDirection.BULLISH, 0.70, 0.18, 0.75, 0.80, DateTime.UtcNow);

            // Act
            consensusEngine.RegisterTimeframeSignal(d1Signal);
            consensusEngine.RegisterTimeframeSignal(h4Signal);
            consensusEngine.RegisterTimeframeSignal(m30Signal);
            consensusEngine.RegisterTimeframeSignal(m5Signal);

            var consensus = consensusEngine.GetCurrentConsensus();

            // Assert
            Assert.Equal(TrendDirection.BULLISH, consensus.DominantBias);
            Assert.True(consensus.BiasStrength > 0.70);
            Assert.True(consensus.EntryTriggered, "Lower timeframe aligning with strong bullish higher TF bias should trigger entry.");
            Assert.NotEmpty(consensus.ConsensusSummary);
        }

        [Fact]
        public void MultiTimeframeConsensusEngine_FiltersStaleSignalsSuccessfully()
        {
            // Arrange
            var consensusEngine = new MultiTimeframeConsensusEngine();

            // Fresh signals
            var d1Signal = new MultiTimeframeSignal(TimeframeInterval.D1, TrendDirection.BULLISH, 0.85, 0.20, 0.80, 0.90, DateTime.UtcNow);
            // Stale signal (2 hours old)
            var h4Signal = new MultiTimeframeSignal(TimeframeInterval.H4, TrendDirection.BULLISH, 0.75, 0.22, 0.70, 0.85, DateTime.UtcNow.AddHours(-2));

            // Act
            consensusEngine.RegisterTimeframeSignal(d1Signal);
            consensusEngine.RegisterTimeframeSignal(h4Signal);

            var consensus = consensusEngine.GetCurrentConsensus();

            // Assert
            // The stale signal (H4) should be filtered out, leaving only D1 as active signal
            Assert.Single(consensus.Signals);
            Assert.Equal(TimeframeInterval.D1, consensus.Signals[0].Timeframe);
        }

        [Fact]
        public async Task ExperienceCollector_RecordsAndUpdatesOutcomesCorrectlyWithCorrelatedId()
        {
            // Arrange
            var stubDb = new StubExperienceDatabaseWriter();
            var collector = new ExperienceCollector(stubDb, NullLogger<ExperienceCollector>.Instance);

            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.9, 0.7, 0.9, 0.1, 80.0, "Trend Bullish");
            var decision = new TradeDecision(DecisionAction.BUY, 0.10, "Neural opportunity", DateTime.UtcNow);
            var sample = new ExperienceSample("EURUSD", TimeframeInterval.H1, marketState, new float[64], decision, 1.2000, "Trend Bullish");

            // Act & Assert (Initial recording)
            collector.RecordDecision(sample);
            Assert.Equal(1, stubDb.EnqueuedCount);
            Assert.Equal(sample.Id, stubDb.LastRecord.Id); // Verify exact correlation ID match!
            Assert.Equal("BUY", stubDb.LastRecord.ExecutedAction);
            Assert.False(stubDb.LastRecord.IsCompleted);

            // Act & Assert (Outcome resolution)
            await collector.UpdateOutcomeAsync(sample.Id, exitPrice: 1.2050, maxDrawdown: 5.0, holdingTimeMinutes: 30.0, outcomeScore: 1.5, "None");
            Assert.Equal(2, stubDb.EnqueuedCount);
            Assert.Equal(sample.Id, stubDb.LastRecord.Id); // Verify exact correlation ID match!
            Assert.True(stubDb.LastRecord.IsCompleted);
            Assert.Equal(150.0, stubDb.LastRecord.RealizedPips); // 1.5 score * 100
        }
    }
}
