using System;
using System.Collections.Generic;

namespace Nexus.Training
{
    /// <summary>
    /// Holds the results of a multi-gate model validation workflow.
    /// </summary>
    public sealed class ValidationResult
    {
        public string ModelVersion { get; }
        public bool PassedBacktest { get; }
        public bool PassedWalkForward { get; }
        public bool PassedOutOfSample { get; }
        public bool PassedPaperTrading { get; }
        public double OverallScore { get; }
        public string FailureReason { get; }
        public DateTime ValidatedAtUtc { get; }

        public bool IsApproved => PassedBacktest && PassedWalkForward && PassedOutOfSample && PassedPaperTrading;

        public ValidationResult(
            string modelVersion,
            bool passedBacktest,
            bool passedWalkForward,
            bool passedOutOfSample,
            bool passedPaperTrading,
            double overallScore,
            string failureReason)
        {
            ModelVersion = modelVersion ?? throw new ArgumentNullException(nameof(modelVersion));
            PassedBacktest = passedBacktest;
            PassedWalkForward = passedWalkForward;
            PassedOutOfSample = passedOutOfSample;
            PassedPaperTrading = passedPaperTrading;
            OverallScore = overallScore;
            FailureReason = failureReason ?? string.Empty;
            ValidatedAtUtc = DateTime.UtcNow;
        }
    }
}
