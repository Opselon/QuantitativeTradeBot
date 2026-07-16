using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.DecisionEngine
{
    /// <summary>
    /// Contract for independent evaluators that contribute to multi-model consensus.
    /// </summary>
    public interface IModelEvaluator
    {
        string Name { get; }
        Task<ModelEvaluationResult> EvaluateAsync(MarketState marketState, CancellationToken ct);
    }

    /// <summary>
    /// Holds the evaluation output of an individual model evaluator.
    /// </summary>
    public sealed class ModelEvaluationResult
    {
        public double Score { get; }      // Numeric score, e.g. -1.0 (very bearish) to 1.0 (very bullish)
        public double Confidence { get; } // 0.0 to 1.0
        public string Explanation { get; }

        public ModelEvaluationResult(double score, double confidence, string explanation)
        {
            Score = score;
            Confidence = confidence;
            Explanation = explanation ?? string.Empty;
        }
    }
}
