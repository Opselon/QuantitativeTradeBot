using System;
using System.Collections.Generic;

namespace Nexus.Application.Dashboard
{
    /// <summary>
    /// Contract for the decision-making dashboard orchestration.
    /// Governs the explainability timeline and historical replays.
    /// </summary>
    public interface IDecisionDashboardService
    {
        string CurrentDecision { get; }
        double Confidence { get; }
        string ExpectedValue { get; }
        IReadOnlyList<string> SupportingEvidence { get; }
        IReadOnlyList<string> RejectedAlternatives { get; }

        // Scenario search values
        double BuyExpectedUtility { get; }
        double SellExpectedUtility { get; }
        double WaitExpectedUtility { get; }
        string SelectionReason { get; }

        // Advanced features: Explainability Timeline & Decision Replay
        IReadOnlyList<ExplainabilityTimelineEntry> ExplainabilityTimeline { get; }
        IReadOnlyList<DecisionReplayPayload> HistoricalDecisions { get; }

        event Action<DecisionDashboardData>? OnDecisionUpdated;

        void PushDecisionUpdate(
            string decision,
            double confidence,
            string expectedValue,
            List<string> supportingEvidence,
            List<string> rejectedAlternatives,
            double buyUtility,
            double sellUtility,
            double waitUtility,
            string selectionReason);

        void AddTimelineEntry(ExplainabilityTimelineEntry entry);
        void AddHistoricalDecision(DecisionReplayPayload payload);
    }

    public class ExplainabilityTimelineEntry
    {
        public string TransitionType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Confidence { get; set; }
        public string TriggeringModels { get; set; } = string.Empty;
        public string RiskChanges { get; set; } = string.Empty;
        public string SupportingEvidence { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class DecisionReplayPayload
    {
        public Guid DecisionId { get; set; }
        public string DecisionName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string MarketSnapshot { get; set; } = string.Empty;
        public string FeatureVectorSummary { get; set; } = string.Empty;
        public string MarketRegime { get; set; } = string.Empty;
        public string MultiTimeframeConsensus { get; set; } = string.Empty;
        public string GeneratedHypotheses { get; set; } = string.Empty;
        public string ScenarioSearchResults { get; set; } = string.Empty;
        public string ModelConsensus { get; set; } = string.Empty;
        public string UncertaintyEvaluation { get; set; } = string.Empty;
        public string FinalDecision { get; set; } = string.Empty;
        public string ExecutionOutcome { get; set; } = string.Empty;
    }

    public class DecisionDashboardData
    {
        public string Decision { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string ExpectedValue { get; set; } = string.Empty;
        public List<string> SupportingEvidence { get; set; } = new();
        public List<string> RejectedAlternatives { get; set; } = new();
        public double BuyUtility { get; set; }
        public double SellUtility { get; set; }
        public double WaitUtility { get; set; }
        public string SelectionReason { get; set; } = string.Empty;
    }
}