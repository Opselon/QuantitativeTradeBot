using Nexus.AI;
using Nexus.Application.Intelligence;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.Intelligence
{
    public class MarketIntelligenceTests
    {
        [Fact]
        public void MarketVector_ToFloatArray_ReturnsCorrectLayout()
        {
            // Arrange
            var vector = new MarketVector(1.1, 2.2, 3.3, 4.4, 5.5, 6.6, 7.7, 8.8, 9.9, 10.0);

            // Act
            float[] array = vector.ToFloatArray();

            // Assert
            Assert.Equal(10, array.Length);
            Assert.Equal(1.1f, array[0]);
            Assert.Equal(2.2f, array[1]);
            Assert.Equal(3.3f, array[2]);
            Assert.Equal(4.4f, array[3]);
            Assert.Equal(5.5f, array[4]);
            Assert.Equal(6.6f, array[5]);
            Assert.Equal(7.7f, array[6]);
            Assert.Equal(8.8f, array[7]);
            Assert.Equal(9.9f, array[8]);
            Assert.Equal(10.0f, array[9]);
        }

        [Fact]
        public void CurrencyStrengthEngine_NeutralInitialState_ReturnsFifty()
        {
            // Arrange
            var engine = new CurrencyStrengthEngine();

            // Act
            double usdStrength = engine.GetStrengthScore("USD");
            double eurStrength = engine.GetStrengthScore("EUR");

            // Assert
            Assert.Equal(50.0, usdStrength);
            Assert.Equal(50.0, eurStrength);
        }

        [Fact]
        public void CurrencyStrengthEngine_ReceivesTickStream_UpdatesStrengthScores()
        {
            // Arrange
            var engine = new CurrencyStrengthEngine();
            var eurUsdSymbol = new Symbol("EURUSD");

            // Base currency rises -> EUR strengthens, USD weakens
            var tick1 = new Tick(eurUsdSymbol, DateTime.UtcNow, 1.08000, 1.08010); // Baseline
            var tick2 = new Tick(eurUsdSymbol, DateTime.UtcNow, 1.09000, 1.09010); // Price rises

            // Act
            engine.UpdateFromTick(tick1);
            engine.UpdateFromTick(tick2);

            double eurStrength = engine.GetStrengthScore("EUR");
            double usdStrength = engine.GetStrengthScore("USD");

            // Assert
            Assert.True(eurStrength > 50.0, "EUR should strengthen.");
            Assert.True(usdStrength < 50.0, "USD should weaken.");
        }

        [Fact]
        public void AccumulatorService_TracksAndUpdatesStateIncrementally()
        {
            // Arrange
            var service = new AccumulatorService();
            string symbol = "GBPUSD";

            // Act
            var state = service.GetState(symbol);
            Assert.Equal(0, state.TickCount);

            // Apply first delta
            var delta1 = new FeatureDelta(symbol, DateTime.UtcNow, 10.0, 1.0);
            var updatedState1 = service.UpdateState(delta1);

            // Apply second delta
            var delta2 = new FeatureDelta(symbol, DateTime.UtcNow, 20.0, 1.0);
            var updatedState2 = service.UpdateState(delta2);

            // Assert
            Assert.Equal(2, updatedState2.TickCount);
            Assert.Equal(30.0, updatedState2.SumPrices);
            Assert.Equal(15.0, updatedState2.CalculateMean());
            Assert.Equal(25.0, updatedState2.CalculateVariance());
            Assert.Equal(5.0, updatedState2.CalculateStandardDeviation());
        }

        [Fact]
        public async Task NeuralModelService_FallbackMode_ProducesDeterministicEvaluations()
        {
            // Arrange
            using var service = new NeuralModelService();
            await service.LoadModelAsync(string.Empty); // Trigger fallback loading

            var bullVector = new MarketVector(0.5, 0.8, 0.9, 0.1, 0.5, 0.9, 0.5, 0.5, 0.8, 0.1);
            var bearVector = new MarketVector(0.5, -0.8, -0.9, 0.1, 0.5, 0.9, 0.5, 0.5, -0.8, 0.1);

            // Act
            var bullResult = await service.EvaluateAsync(bullVector);
            var bearResult = await service.EvaluateAsync(bearVector);

            // Assert
            Assert.True(bullResult.BuyConfidence > bullResult.SellConfidence, "Bull vector should favor BUY.");
            Assert.True(bearResult.SellConfidence > bearResult.BuyConfidence, "Bear vector should favor SELL.");
            Assert.Equal(ModelMode.FALLBACK_MODE, service.CurrentMode);
        }

        [Fact]
        public void DecisionEngine_PreTradeRiskBlocked_ReturnsWaitAction()
        {
            // Arrange
            var engine = new Nexus.Application.Intelligence.DecisionEngine();
            var evaluation = new EvaluationResult(0.9, 0.1, 0.0, 0.015, 0.2, 0.9, "Bullish");
            var market = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.9, 0.7, 0.9, 0.1, 80.0, "Trend Bullish");
            var riskBlocked = new RiskState(150.0, 0.1, 0.15, 2, 4.5, isTradingBlocked: true);

            // Act
            var decision = engine.Evaluate(evaluation, market, riskBlocked);

            // Assert
            Assert.Equal(DecisionAction.WAIT, decision.Action);
            Assert.Equal(0.0, decision.TargetVolume);
            Assert.Contains("blocked by pre-trade risk", decision.Reason);
        }

        [Fact]
        public void DecisionEngine_HighConfidenceBuy_ReturnsBuyAction()
        {
            // Arrange
            var engine = new Nexus.Application.Intelligence.DecisionEngine();
            var evaluation = new EvaluationResult(0.85, 0.1, 0.05, 0.015, 0.2, 0.85, "Bullish");
            var market = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.8, 0.9, 0.7, 0.9, 0.1, 80.0, "Trend Bullish");
            var riskNormal = new RiskState(500.0, 0.1, 0.02, 0, 0.0, isTradingBlocked: false);

            // Act
            var decision = engine.Evaluate(evaluation, market, riskNormal);

            // Assert
            Assert.Equal(DecisionAction.BUY, decision.Action);
            Assert.True(decision.TargetVolume > 0.0);
            Assert.Contains("buy opportunity identified", decision.Reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ScenarioEvaluationEngine_CalculatesScoresFromMarketState()
        {
            // Arrange
            var engine = new ScenarioEvaluationEngine();
            var state = new MarketState("EURUSD", DateTime.UtcNow, 0.8, 0.9, 0.1, 0.7, 0.9, 0.1, 80.0, "Trend Bullish");

            // Act
            var score = engine.EvaluateScenarios(state);

            // Assert
            Assert.True(score.TrendContinuationScore > 0, "Scores should be populated.");
            Assert.True(score.VolatilityExpansionScore > 0, "Scores should be populated.");
            Assert.Equal("Trend Continuation", score.GetDominantScenario());
        }

        [Fact]
        public void PatternMemory_StoresAndRetrievesSymmetricSimilarPatterns()
        {
            // Arrange
            var memory = new PatternMemory();
            var pattern1 = new MarketVector(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
            var pattern2 = new MarketVector(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1);

            memory.Store(pattern1, "Bullish", "Succeeded", 1.5);
            memory.Store(pattern2, "Bearish", "Failed", -1.0);

            // Act
            var query = new MarketVector(0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9, 0.9);
            var results = memory.Search(query, 0.95);

            // Assert
            Assert.Single(results);
            Assert.Equal("Bullish", results[0].Conditions);
            Assert.Equal("Succeeded", results[0].Outcome);
            Assert.True(results[0].Similarity > 0.98);
        }
    }
}
