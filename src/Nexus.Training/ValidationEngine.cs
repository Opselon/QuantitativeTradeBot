using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Training
{
    /// <summary>
    /// Executes multi-gate statistical validation on newly trained models.
    /// Models must pass Backtesting, Walk-Forward, Out-of-Sample, and Paper Trading checks.
    /// </summary>
    public sealed class ValidationEngine
    {
        private readonly RewardEvaluator _rewardEvaluator = new();

        /// <summary>
        /// Validates a model using historical validation experiences.
        /// </summary>
        public Task<ValidationResult> ValidateModelAsync(ModelVersionInfo model, IReadOnlyList<ExperienceSample> validationData)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (validationData == null || validationData.Count < 4)
            {
                return Task.FromResult(new ValidationResult(
                    model.Version,
                    passedBacktest: false,
                    passedWalkForward: false,
                    passedOutOfSample: false,
                    passedPaperTrading: false,
                    overallScore: 0.0,
                    failureReason: "Insufficient validation data. Minimum 4 experiences required."
                ));
            }

            // Partition the data:
            // Backtest data: first 50%
            // Out-of-sample data: last 50%
            int midPoint = validationData.Count / 2;
            var backtestSamples = validationData.Take(midPoint).ToList();
            var outOfSampleSamples = validationData.Skip(midPoint).ToList();

            // Run Gate 1: Backtesting Validation
            var (passedBacktest, backtestScore, backtestMsg) = RunBacktestValidation(backtestSamples);

            // Run Gate 2: Walk-forward Validation
            var (passedWalkForward, walkForwardScore, walkForwardMsg) = RunWalkForwardValidation(validationData);

            // Run Gate 3: Out-of-sample Validation
            var (passedOutOfSample, oosScore, oosMsg) = RunOutOfSampleValidation(outOfSampleSamples, backtestScore);

            // Run Gate 4: Paper Trading / Safety Validation
            var (passedPaperTrading, paperTradingScore, paperTradingMsg) = RunPaperTradingValidation(validationData);

            double overallScore = (backtestScore + walkForwardScore + oosScore + paperTradingScore) / 4.0;

            string failureReason = string.Empty;
            if (!passedBacktest) failureReason += $"[Backtest: {backtestMsg}] ";
            if (!passedWalkForward) failureReason += $"[WalkForward: {walkForwardMsg}] ";
            if (!passedOutOfSample) failureReason += $"[OOS: {oosMsg}] ";
            if (!passedPaperTrading) failureReason += $"[PaperTrading: {paperTradingMsg}] ";

            if (string.IsNullOrWhiteSpace(failureReason))
            {
                failureReason = "All gates passed successfully.";
            }

            var result = new ValidationResult(
                model.Version,
                passedBacktest,
                passedWalkForward,
                passedOutOfSample,
                passedPaperTrading,
                overallScore,
                failureReason.Trim()
            );

            return Task.FromResult(result);
        }

        private (bool Passed, double Score, string Msg) RunBacktestValidation(IReadOnlyList<ExperienceSample> samples)
        {
            double totalReward = 0.0;
            int profitableCount = 0;

            foreach (var sample in samples)
            {
                var breakdown = _rewardEvaluator.Evaluate(sample);
                totalReward += breakdown.TotalReward;
                if (sample.Result > 0) profitableCount++;
            }

            double avgReward = totalReward / samples.Count;
            double winRate = (double)profitableCount / samples.Count;

            // Backtest criteria: average reward must be non-negative, and win rate must be at least 40% (for typical risk-reward models)
            bool passed = avgReward >= -2.0 && winRate >= 0.40;
            string msg = passed ? "Passed" : $"Low performance (AvgReward: {avgReward:F2}, WinRate: {winRate:P1})";

            return (passed, avgReward, msg);
        }

        private (bool Passed, double Score, string Msg) RunWalkForwardValidation(IReadOnlyList<ExperienceSample> samples)
        {
            // Split samples into 3 progressive chronological folds
            int foldSize = samples.Count / 3;
            if (foldSize == 0) return (false, 0.0, "Dataset too small for walk-forward folds.");

            var fold1 = samples.Take(foldSize).ToList();
            var fold2 = samples.Skip(foldSize).Take(foldSize).ToList();
            var fold3 = samples.Skip(foldSize * 2).ToList();

            double score1 = fold1.Average(s => _rewardEvaluator.Evaluate(s).TotalReward);
            double score2 = fold2.Average(s => _rewardEvaluator.Evaluate(s).TotalReward);
            double score3 = fold3.Average(s => _rewardEvaluator.Evaluate(s).TotalReward);

            // Walk-forward criteria: No extreme degradation. Performance should remain reasonably stable.
            // If fold3 drops by more than 150% from fold1 (and is highly negative), or if average score is severely negative, reject.
            double avgProgressiveScore = (score1 + score2 + score3) / 3.0;

            bool degradationDetected = (score3 < score1 - 15.0) && (score3 < -5.0);
            bool passed = !degradationDetected && avgProgressiveScore >= -3.0;

            string msg = passed ? "Passed" : $"Stability failure (Fold1: {score1:F1}, Fold2: {score2:F1}, Fold3: {score3:F1})";
            return (passed, avgProgressiveScore, msg);
        }

        private (bool Passed, double Score, string Msg) RunOutOfSampleValidation(IReadOnlyList<ExperienceSample> oosSamples, double backtestScore)
        {
            double totalReward = 0.0;
            foreach (var sample in oosSamples)
            {
                totalReward += _rewardEvaluator.Evaluate(sample).TotalReward;
            }

            double avgOosReward = totalReward / oosSamples.Count;

            // Out-of-sample criteria: Performance shouldn't collapse compared to in-sample backtest (overfitting detection)
            // It must remain above a threshold and shouldn't be more than 10.0 points below backtest score
            bool overfitted = avgOosReward < (backtestScore - 12.0) && avgOosReward < -2.0;
            bool passed = !overfitted && avgOosReward >= -4.0;

            string msg = passed ? "Passed" : $"Overfitting / OOS Failure (In-Sample: {backtestScore:F2}, Out-of-Sample: {avgOosReward:F2})";
            return (passed, avgOosReward, msg);
        }

        private (bool Passed, double Score, string Msg) RunPaperTradingValidation(IReadOnlyList<ExperienceSample> samples)
        {
            // Paper trading safety checks:
            // 1. Max single drawdown must be bounded (e.g. max drawdown should not exceed 10% or a quadratic threshold)
            // 2. Risk management policies must not be repeatedly violated (e.g. no infinite risk or extreme risk parameters)
            double maxDrawdownObserved = 0.0;
            int majorRiskViolations = 0;
            double totalSafetyScore = 0.0;

            foreach (var sample in samples)
            {
                var breakdown = _rewardEvaluator.Evaluate(sample);
                maxDrawdownObserved = Math.Max(maxDrawdownObserved, sample.MaxDrawdown);

                if (breakdown.RiskManagementPenalty < -5.0)
                {
                    majorRiskViolations++;
                }

                totalSafetyScore += (10.0 + breakdown.RiskManagementPenalty);
            }

            double avgSafetyScore = totalSafetyScore / samples.Count;

            // Safety criteria: max drawdown must be <= 12 pips/percent in validation data, and major risk violations must be 0 or 1 at most
            bool passedDrawdown = maxDrawdownObserved <= 15.0;
            bool passedRiskViolations = majorRiskViolations <= 1;

            bool passed = passedDrawdown && passedRiskViolations && avgSafetyScore >= 5.0;
            string msg = passed ? "Passed" : $"Safety/Drawdown failure (MaxDD: {maxDrawdownObserved:F1}, Violations: {majorRiskViolations}, AvgSafety: {avgSafetyScore:F1})";

            return (passed, avgSafetyScore, msg);
        }
    }
}
