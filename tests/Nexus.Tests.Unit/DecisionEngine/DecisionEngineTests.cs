using Nexus.Core.Entities;
using Nexus.DecisionEngine;

namespace Nexus.Tests.Unit.DecisionEngine
{
    public sealed class DecisionEngineTests
    {
        [Fact]
        public async Task DecisionScenarioSearchEngine_SearchesAndRanksCandidatePaths()
        {
            // Arrange
            var searchEngine = new DecisionScenarioSearchEngine();
            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.9, 0.5, 0.8, 0.2, 80.0, "TrendContinuation");
            var riskState = new RiskState(10000.0, 0.05, 0.0, 0, 0.0, isTradingBlocked: false);

            // Act
            var bestNode = await searchEngine.SearchBestActionAsync(marketState, riskState, 0.85, 0.10, CancellationToken.None);

            // Assert
            Assert.NotNull(bestNode);
            Assert.Contains(bestNode.Action, new[] { DecisionAction.BUY, DecisionAction.SELL, DecisionAction.WAIT, DecisionAction.CLOSE, DecisionAction.REDUCE, DecisionAction.ADD });
            Assert.NotEmpty(bestNode.ProjectedScenarios);
            Assert.True(bestNode.Score > -100.0);
        }

        [Fact]
        public async Task DecisionScenarioSearchEngine_PenalizesOrBlocksActionsWhenRiskStateBlocked()
        {
            // Arrange
            var searchEngine = new DecisionScenarioSearchEngine();
            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.9, 0.5, 0.8, 0.2, 80.0, "TrendContinuation");
            var blockedRiskState = new RiskState(10000.0, 0.05, 0.05, 2, 2.0, isTradingBlocked: true);

            // Act
            var nodeForBuy = await searchEngine.SearchBestActionAsync(marketState, blockedRiskState, 0.85, 0.10, CancellationToken.None);

            // Assert
            Assert.NotNull(nodeForBuy);
            // Buy/Sell should be heavily penalized or Wait chosen because trade is blocked
            if (nodeForBuy.Action == DecisionAction.BUY || nodeForBuy.Action == DecisionAction.SELL || nodeForBuy.Action == DecisionAction.ADD)
            {
                Assert.Equal(-100.0, nodeForBuy.Score);
                Assert.Contains("blocked due to active pre-trade risk restrictions", nodeForBuy.Reasoning);
            }
        }

        [Fact]
        public void MarketHypothesisEngine_GeneratesCompetingHypothesesWithCorrectOutputs()
        {
            // Arrange
            var engine = new MarketHypothesisEngine();
            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.3, 0.5, 0.8, 0.5, 0.8, 0.2, 75.0, "TrendContinuation");

            // Act
            var hypotheses = engine.GenerateHypotheses(marketState, 0.80, 0.15);

            // Assert
            Assert.NotNull(hypotheses);
            Assert.Equal(3, hypotheses.Count);

            var continuation = hypotheses.Find(h => h.Name == "Trend Continuation");
            var reversal = hypotheses.Find(h => h.Name == "Trend Reversal");
            var sideways = hypotheses.Find(h => h.Name == "Sideways / Mean Reversion");

            Assert.NotNull(continuation);
            Assert.NotNull(reversal);
            Assert.NotNull(sideways);

            Assert.True(continuation.Probability > 0);
            Assert.True(continuation.ExpectedRewardPips > 0);
            Assert.True(continuation.RiskPips > 0);
            Assert.True(continuation.Confidence > 0);

            // Expect Trend Continuation to have highest probability under bullish setup
            Assert.True(continuation.Probability >= sideways.Probability);
        }

        [Fact]
        public async Task MultiModelConsensusAggregator_ComputesWeighedAverageAcrossModels()
        {
            // Arrange
            var evaluators = new List<IModelEvaluator>
            {
                new TrendModel(),
                new VolatilityModel(),
                new MomentumModel(),
                new LiquidityModel()
            };
            var aggregator = new MultiModelConsensusAggregator(evaluators);
            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.6, 0.9, 0.5, 0.8, 0.2, 80.0, "TrendContinuation");

            // Act
            var consensus = await aggregator.AggregateConsensusAsync(marketState, CancellationToken.None);

            // Assert
            Assert.NotNull(consensus);
            Assert.True(consensus.AggregatedConfidence >= 0.0 && consensus.AggregatedConfidence <= 1.0);
            Assert.True(consensus.AggregatedScore >= -1.0 && consensus.AggregatedScore <= 1.0);
            Assert.Equal(evaluators.Count, consensus.ContributorSummaries.Count);
            Assert.Equal("Bullish", consensus.DominantBias); // Since momentum/trend scores are positive
        }

        [Fact]
        public void UncertaintyEngine_ClassifiesUncertaintyLevelsCorrectly()
        {
            // Arrange
            var engine = new UncertaintyEngine();
            var quietMarket = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.1, 0.9, 0.5, 0.8, 0.2, 80.0, "Consolidation");
            var extremeVolMarket = new MarketState("EURUSD", DateTime.UtcNow, 0.9, 0.9, 0.3, 0.5, 0.8, 0.2, 80.0, "VolatilityExpansion");

            // Act & Assert
            // Quiet, stable market + good consensus/neural confidence = Low Uncertainty
            var quietUncertainty = engine.EvaluateUncertainty(quietMarket, 0.85, 0.80);
            Assert.Equal(UncertaintyLevel.Low, quietUncertainty);

            // High Volatility = High Uncertainty
            var volUncertainty = engine.EvaluateUncertainty(extremeVolMarket, 0.85, 0.80);
            Assert.Equal(UncertaintyLevel.High, volUncertainty);

            // Low Neural Confidence = High Uncertainty
            var lowConfUncertainty = engine.EvaluateUncertainty(quietMarket, 0.35, 0.80);
            Assert.Equal(UncertaintyLevel.High, lowConfUncertainty);

            // Low Consensus Agreement = High Uncertainty
            var lowAgreementUncertainty = engine.EvaluateUncertainty(quietMarket, 0.85, 0.25);
            Assert.Equal(UncertaintyLevel.High, lowAgreementUncertainty);
        }

        [Fact]
        public async Task DecisionPipelineOrchestrator_SucceedsEndToEndWithDetailedExplanation()
        {
            // Arrange
            var evaluators = new List<IModelEvaluator>
            {
                new TrendModel(),
                new VolatilityModel(),
                new MomentumModel(),
                new LiquidityModel(),
                new PatternRecognitionModel(),
                new OrderFlowModel(),
                new MacroModel()
            };
            var consensusAggregator = new MultiModelConsensusAggregator(evaluators);
            var hypothesisEngine = new MarketHypothesisEngine();
            var scenarioSearchEngine = new DecisionScenarioSearchEngine();
            var uncertaintyEngine = new UncertaintyEngine();
            var marketMemory = new StubMarketMemory();

            var orchestrator = new DecisionPipelineOrchestrator(
                consensusAggregator,
                hypothesisEngine,
                scenarioSearchEngine,
                uncertaintyEngine,
                marketMemory
            );

            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.8, 0.5, 0.8, 0.2, 80.0, "TrendContinuation");
            var riskState = new RiskState(10000.0, 0.05, 0.0, 0, 0.0, isTradingBlocked: false);
            var features = new float[64];

            // Act
            var package = await orchestrator.OrchestrateDecisionAsync(
                marketState,
                riskState,
                features,
                neuralBuyConfidence: 0.85,
                neuralSellConfidence: 0.10,
                CancellationToken.None
            );

            // Assert
            Assert.NotNull(package);
            Assert.True(package.Confidence > 0);
            Assert.NotEmpty(package.Evidence);
            Assert.NotEmpty(package.RiskSummary);
            Assert.NotEmpty(package.ExpectedOutcome);
            Assert.True(package.AlternativeDecisions.ContainsKey(DecisionAction.BUY));

            // Under strong bullish setup with low uncertainty, we expect BUY or ADD to be preferred and execution-ready
            Assert.True(package.IsExecutionReady);
            Assert.Contains(package.SelectedAction, new[] { DecisionAction.BUY, DecisionAction.ADD });
        }

        [Fact]
        public async Task DecisionPipelineOrchestrator_ForcesWaitWhenUncertaintyIsHigh()
        {
            // Arrange
            var evaluators = new List<IModelEvaluator> { new TrendModel() };
            var consensusAggregator = new MultiModelConsensusAggregator(evaluators);
            var hypothesisEngine = new MarketHypothesisEngine();
            var scenarioSearchEngine = new DecisionScenarioSearchEngine();
            var uncertaintyEngine = new UncertaintyEngine();
            var marketMemory = new StubMarketMemory();

            var orchestrator = new DecisionPipelineOrchestrator(
                consensusAggregator,
                hypothesisEngine,
                scenarioSearchEngine,
                uncertaintyEngine,
                marketMemory
            );

            // High Volatility forces uncertainty level to High
            var marketState = new MarketState("EURUSD", DateTime.UtcNow, 0.95, 0.8, 0.8, 0.5, 0.8, 0.2, 80.0, "VolatilityExpansion");
            var riskState = new RiskState(10000.0, 0.05, 0.0, 0, 0.0, isTradingBlocked: false);
            var features = new float[64];

            // Act
            var package = await orchestrator.OrchestrateDecisionAsync(
                marketState,
                riskState,
                features,
                neuralBuyConfidence: 0.85,
                neuralSellConfidence: 0.10,
                CancellationToken.None
            );

            // Assert
            Assert.NotNull(package);
            Assert.Equal(DecisionAction.WAIT, package.SelectedAction); // High uncertainty must override to WAIT
            Assert.False(package.IsExecutionReady);
            Assert.Contains("Uncertainty Level: High", package.Evidence);
        }
    }
}
