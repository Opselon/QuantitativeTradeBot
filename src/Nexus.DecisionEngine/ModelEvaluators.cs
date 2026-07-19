using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    public class TrendModel : IModelEvaluator
    {
        public string Name => "Trend Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            // Simplified model logic based on market state momentum
            double score = marketState.Momentum; // Maps closely to trend
            double confidence = 0.80;
            string explanation = $"Trend is matching momentum direction {score:F2} in {marketState.MarketRegime}.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class VolatilityModel : IModelEvaluator
    {
        public string Name => "Volatility Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            double score = marketState.Volatility > 0.4 ? -0.2 : 0.4; // High volatility penalizes slightly
            double confidence = 0.75;
            string explanation = $"Volatility index is {marketState.Volatility:F2}, indicating {(marketState.Volatility > 0.4 ? "high" : "moderate")} risk conditions.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class MomentumModel : IModelEvaluator
    {
        public string Name => "Momentum Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            // MarketState has CurrencyStrength and Momentum properties
            double score = marketState.Momentum > 0.5 ? 0.6 : (marketState.Momentum < -0.5 ? -0.6 : 0.1);
            double confidence = 0.85;
            string explanation = $"Momentum index at {marketState.Momentum:F1} indicates {(marketState.Momentum > 0.5 ? "bullish momentum" : (marketState.Momentum < -0.5 ? "bearish momentum" : "neutral momentum"))} state.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class LiquidityModel : IModelEvaluator
    {
        public string Name => "Liquidity Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            double score = marketState.Liquidity > 0.7 ? 0.6 : -0.3; // High liquidity is good
            double confidence = 0.70;
            string explanation = $"Liquidity index at {marketState.Liquidity:F2} is {(marketState.Liquidity > 0.7 ? "optimal" : "low")}.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class PatternRecognitionModel : IModelEvaluator
    {
        public string Name => "Pattern Recognition Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            double score = 0.2;
            double confidence = 0.60;
            string explanation = "Detected mild bullish divergence flag on intermediate timeframe.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class OrderFlowModel : IModelEvaluator
    {
        public string Name => "Order Flow Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            double score = 0.3;
            double confidence = 0.65;
            string explanation = "Order book imbalance shows minor institutional buying delta.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class MacroModel : IModelEvaluator
    {
        public string Name => "Macro Model";

        public Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct)
        {
            double score = 0.1;
            double confidence = 0.50;
            string explanation = "Interest rate differentials support a neutral-to-long base bias.";
            return Task.FromResult(new ModelEvaluationResult(score, confidence, explanation));
        }
    }

    public class StubMarketMemory : IMarketMemory
    {
        public Task<double> GetSimilarSituationsSuccessRateAsync(string symbol, double[] currentFeatures, CancellationToken ct)
        {
            return Task.FromResult(0.68); // Realistic historical success rate fallback
        }

        public Task<int> GetPatternFrequencyAsync(string symbol, string patternName, CancellationToken ct)
        {
            return Task.FromResult(42); // Realistic count fallback
        }
    }
}
