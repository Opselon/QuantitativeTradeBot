using System;
using System.Collections.Generic;

namespace Nexus.Application.Dashboard
{
    public sealed class DecisionDashboardService : IDecisionDashboardService
    {
        public string CurrentDecision { get; private set; } = "BUY";
        public double Confidence { get; private set; } = 0.84;
        public string ExpectedValue { get; private set; } = "Positive (EV: +12.4 pips)";
        private readonly List<string> _supportingEvidence = new() { "Trend alignment on H4/D1", "Momentum expansion on M15", "Symmetric Liquidity support" };
        public IReadOnlyList<string> SupportingEvidence => _supportingEvidence;
        private readonly List<string> _rejectedAlternatives = new() { "SELL (Confidence: 19%)", "WAIT (Confidence: 42%)" };
        public IReadOnlyList<string> RejectedAlternatives => _rejectedAlternatives;

        public double BuyExpectedUtility { get; private set; } = 8.5;
        public double SellExpectedUtility { get; private set; } = -3.2;
        public double WaitExpectedUtility { get; private set; } = 0.0;
        public string SelectionReason { get; private set; } = "BUY scenario yields maximum expected utility (+8.5) under current bullish momentum regime with minimal downside volatility risk.";

        // Advanced features: Explainability Timeline & Decision Replay
        private readonly List<ExplainabilityTimelineEntry> _explainabilityTimeline = new();
        public IReadOnlyList<ExplainabilityTimelineEntry> ExplainabilityTimeline => _explainabilityTimeline;

        private readonly List<DecisionReplayPayload> _historicalDecisions = new();
        public IReadOnlyList<DecisionReplayPayload> HistoricalDecisions => _historicalDecisions;

        public event Action<DecisionDashboardData>? OnDecisionUpdated;

        public DecisionDashboardService()
        {
            SeedInitialTimelineAndReplays();
        }

        public void PushDecisionUpdate(
            string decision,
            double confidence,
            string expectedValue,
            List<string> supportingEvidence,
            List<string> rejectedAlternatives,
            double buyUtility,
            double sellUtility,
            double waitUtility,
            string selectionReason)
        {
            CurrentDecision = decision;
            Confidence = confidence;
            ExpectedValue = expectedValue;

            _supportingEvidence.Clear();
            _supportingEvidence.AddRange(supportingEvidence);

            _rejectedAlternatives.Clear();
            _rejectedAlternatives.AddRange(rejectedAlternatives);

            BuyExpectedUtility = buyUtility;
            SellExpectedUtility = sellUtility;
            WaitExpectedUtility = waitUtility;
            SelectionReason = selectionReason;

            OnDecisionUpdated?.Invoke(new DecisionDashboardData
            {
                Decision = decision,
                Confidence = confidence,
                ExpectedValue = expectedValue,
                SupportingEvidence = new List<string>(supportingEvidence),
                RejectedAlternatives = new List<string>(rejectedAlternatives),
                BuyUtility = buyUtility,
                SellUtility = sellUtility,
                WaitUtility = waitUtility,
                SelectionReason = selectionReason
            });
        }

        public void AddTimelineEntry(ExplainabilityTimelineEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            _explainabilityTimeline.Insert(0, entry);
        }

        public void AddHistoricalDecision(DecisionReplayPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            _historicalDecisions.Insert(0, payload);
        }

        private void SeedInitialTimelineAndReplays()
        {
            // Seed timeline
            var now = DateTime.Now;
            _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
            {
                TransitionType = "CLOSE",
                Timestamp = now.AddMinutes(-2),
                Confidence = 0.92,
                TriggeringModels = "TrendModel, VolatilityModel, PatternRecognitionModel",
                RiskChanges = "Leverage reduced from 1:100 to 1:0 (Position fully closed)",
                SupportingEvidence = "Bearish structural engulfing candle detected on M5 entry timeframe",
                Reason = "Locked-in gains (+14.2 pips) due to imminent counter-trend structural volatility rejection."
            });
            _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
            {
                TransitionType = "PARTIAL_CLOSE",
                Timestamp = now.AddMinutes(-12),
                Confidence = 0.81,
                TriggeringModels = "MomentumModel, VolatilityModel",
                RiskChanges = "Single Position Size exposure halved (0.50 Lots -> 0.25 Lots)",
                SupportingEvidence = "Average True Range (ATR) hit target multiplier; volume pressure reversing",
                Reason = "De-risked position by securing 50% profits at key resistance level (1.08750)."
            });
            _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
            {
                TransitionType = "MOVE_STOP",
                Timestamp = now.AddMinutes(-30),
                Confidence = 0.74,
                TriggeringModels = "TrailingManager, RiskModel",
                RiskChanges = "Stop Loss trailed upward from 1.08300 to 1.08550 (+25 pips lock-in)",
                SupportingEvidence = "Price structure confirmed a higher-low support point on M15 timeline",
                Reason = "Trailed Stop Loss to lock-in accumulated profits and maintain risk-to-reward constraints."
            });
            _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
            {
                TransitionType = "BUY",
                Timestamp = now.AddMinutes(-45),
                Confidence = 0.86,
                TriggeringModels = "TrendModel, MomentumModel, LiquidityModel",
                RiskChanges = "Activated initial risk exposure: 2.0% equity margin ($2,000 RiskAmount)",
                SupportingEvidence = "Consensus bullish on D1 and H4; M15 Entry timing triggered at liquidity block",
                Reason = "High-probability trend continuation setup identified with positive expected utility (+8.5)."
            });
            _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
            {
                TransitionType = "WAIT",
                Timestamp = now.AddMinutes(-60),
                Confidence = 0.95,
                TriggeringModels = "UncertaintyEngine, VolatilityModel",
                RiskChanges = "Zero live market risk exposure. Orders frozen.",
                SupportingEvidence = "High-noise regime detected prior to London session open",
                Reason = "High system uncertainty and conflicting indicators triggered safety WAIT mode to bypass execution."
            });

            // Seed historical decision replays
            _historicalDecisions.Add(new DecisionReplayPayload
            {
                DecisionId = Guid.NewGuid(),
                DecisionName = "DEC-048 (CLOSE)",
                Timestamp = now.AddMinutes(-2),
                MarketSnapshot = "Symbol: EURUSD | Bid: 1.08720, Ask: 1.08730 | Session: London",
                FeatureVectorSummary = "F[0..3] = {0.12, 0.45, -0.68, -0.15} (Engulfing Bearish Pattern)",
                MarketRegime = "High Volatility Range (Rejection Zone)",
                MultiTimeframeConsensus = "D1: Bullish | H4: Neutral | M15: Bearish",
                GeneratedHypotheses = "Hypothesis A: Reversal (Prob: 68%) | Hypothesis B: Continuation (Prob: 32%)",
                ScenarioSearchResults = "CLOSE (EV: +14.2) | WAIT (EV: 0.0) | BUY (EV: -4.5)",
                ModelConsensus = "Specialized Models: VolatilityModel (CLOSE: 0.92) | PatternModel (CLOSE: 0.85)",
                UncertaintyEvaluation = "System Uncertainty: Low (System highly confident in engulfing pattern)",
                FinalDecision = "CLOSE (Volume: 0.50 Lots)",
                ExecutionOutcome = "Successfully Executed - Position Closed cleanly at 1.08720 with profit: +$71.00."
            });

            _historicalDecisions.Add(new DecisionReplayPayload
            {
                DecisionId = Guid.NewGuid(),
                DecisionName = "DEC-047 (PARTIAL_CLOSE)",
                Timestamp = now.AddMinutes(-12),
                MarketSnapshot = "Symbol: EURUSD | Bid: 1.08750, Ask: 1.08760 | Session: London",
                FeatureVectorSummary = "F[0..3] = {0.35, 0.82, -0.10, 0.55} (Exhaustion Candle at resistance)",
                MarketRegime = "Trending Bullish (Exhaustion Candidate)",
                MultiTimeframeConsensus = "D1: Bullish | H4: Bullish | M15: Overbought",
                GeneratedHypotheses = "Hypothesis A: Pullback (Prob: 55%) | Hypothesis B: Breakout (Prob: 45%)",
                ScenarioSearchResults = "PARTIAL_CLOSE (EV: +6.4) | WAIT (EV: 0.0) | BUY (EV: +2.1)",
                ModelConsensus = "Specialized Models: MomentumModel (REDUCE: 0.78) | VolatilityModel (REDUCE: 0.83)",
                UncertaintyEvaluation = "System Uncertainty: Medium",
                FinalDecision = "PARTIAL_CLOSE (Halved size to 0.25 Lots)",
                ExecutionOutcome = "Successfully Executed - Halved position ticket #1024; locked in $125.00 profit."
            });

            _historicalDecisions.Add(new DecisionReplayPayload
            {
                DecisionId = Guid.NewGuid(),
                DecisionName = "DEC-046 (BUY)",
                Timestamp = now.AddMinutes(-45),
                MarketSnapshot = "Symbol: EURUSD | Bid: 1.08500, Ask: 1.08510 | Session: London",
                FeatureVectorSummary = "F[0..3] = {0.75, 0.65, 0.82, 0.92} (Double Bottom Support bounce)",
                MarketRegime = "Trending Bullish (Support Bounce)",
                MultiTimeframeConsensus = "D1: Bullish | H4: Bullish | M15: Bullish",
                GeneratedHypotheses = "Hypothesis A: Continuation (Prob: 84%) | Hypothesis B: Range Reversal (Prob: 16%)",
                ScenarioSearchResults = "BUY (EV: +8.5) | WAIT (EV: 0.0) | SELL (EV: -9.2)",
                ModelConsensus = "Specialized Models: TrendModel (BUY: 0.91) | MomentumModel (BUY: 0.88) | LiquidityModel (BUY: 0.94)",
                UncertaintyEvaluation = "System Uncertainty: Low (All timeframes aligned Bullish)",
                FinalDecision = "BUY (Volume: 0.50 Lots)",
                ExecutionOutcome = "Successfully Executed - Opened position ticket #1024 at entry: 1.08510."
            });
        }
    }
}
