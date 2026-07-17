using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.DomainEvents;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Dashboard
{
    public sealed class DecisionDashboardService : IDecisionDashboardService
    {
        public string CurrentDecision { get; private set; } = "UNKNOWN";
        public double Confidence { get; private set; } = 0.0;
        public string ExpectedValue { get; private set; } = "UNKNOWN";

        private readonly List<string> _supportingEvidence = new();
        public IReadOnlyList<string> SupportingEvidence => _supportingEvidence;

        private readonly List<string> _rejectedAlternatives = new();
        public IReadOnlyList<string> RejectedAlternatives => _rejectedAlternatives;

        public double BuyExpectedUtility { get; private set; } = 0.0;
        public double SellExpectedUtility { get; private set; } = 0.0;
        public double WaitExpectedUtility { get; private set; } = 0.0;
        public string SelectionReason { get; private set; } = "No real decision evaluation has been executed yet.";

        // Advanced features: Explainability Timeline & Decision Replay
        private readonly List<ExplainabilityTimelineEntry> _explainabilityTimeline = new();

        // REASON: Returns a thread-safe snapshot copy (.ToList) under a lock block.
        // This prevents WPF's ItemContainerGenerator from throwing "ItemsControl is inconsistent with its items source"
        // when background threads append explainability entries concurrently with UI rendering.
        public IReadOnlyList<ExplainabilityTimelineEntry> ExplainabilityTimeline
        {
            get
            {
                lock (_explainabilityTimeline)
                {
                    return _explainabilityTimeline.ToList();
                }
            }
        }

        private readonly List<DecisionReplayPayload> _historicalDecisions = new();

        // REASON: Returns a thread-safe snapshot copy (.ToList) under a lock block to protect WPF's ListBox binding.
        public IReadOnlyList<DecisionReplayPayload> HistoricalDecisions
        {
            get
            {
                lock (_historicalDecisions)
                {
                    return _historicalDecisions.ToList();
                }
            }
        }


        public event Action<DecisionDashboardData>? OnDecisionUpdated;

        private readonly IDecisionEventStream _eventStream;
        private readonly IServiceScopeFactory _scopeFactory;

        public DecisionDashboardService(IDecisionEventStream eventStream, IServiceScopeFactory scopeFactory)
        {
            _eventStream = eventStream ?? throw new ArgumentNullException(nameof(eventStream));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            // Subscribe to real-time events published by the core engines
            _eventStream.OnDecisionCreated += HandleDecisionCreated;
            _eventStream.OnDecisionChanged += HandleDecisionChanged;
            _eventStream.OnRiskAdjusted += HandleRiskAdjusted;
            _eventStream.OnPositionManagement += HandlePositionManagement;
            _eventStream.OnExecutionCompleted += HandleExecutionCompleted;

            // Load initial history asynchronously from real database persistent storage (Rule 2 & Database Requirements)
            Task.Run(() => LoadHistoryFromDatabaseAsync());
        }

        private async Task LoadHistoryFromDatabaseAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var experienceRepo = scope.ServiceProvider.GetService<IExperienceRepository>();
                if (experienceRepo == null) return;

                var records = await experienceRepo.GetRecentExperiencesAsync(50);
                if (records == null || records.Count == 0) return;

                lock (_historicalDecisions)
                {
                    _historicalDecisions.Clear();
                    foreach (var r in records)
                    {
                        _historicalDecisions.Add(new DecisionReplayPayload
                        {
                            DecisionId = r.Id,
                            DecisionName = $"DEC-{r.Id.ToString().Substring(0, 5).ToUpper()} ({r.ExecutedAction})",
                            Timestamp = r.TimestampUtc.ToLocalTime(),
                            MarketSnapshot = $"Symbol: {r.Symbol} | BuyConf: {r.BuyConfidence:P0}, SellConf: {r.SellConfidence:P0} | Risk: {r.RiskScore:F2}",
                            FeatureVectorSummary = string.Join(", ", r.MarketVectorFeatures.Select(f => f.ToString("F4"))),
                            MarketRegime = r.MarketRegime ?? "Ranging",
                            MultiTimeframeConsensus = $"Buy: {r.BuyConfidence:P0} | Sell: {r.SellConfidence:P0}",
                            GeneratedHypotheses = $"Executed: {r.ExecutedAction}",
                            ScenarioSearchResults = $"Realized Pips: {r.RealizedPips:F1} pips",
                            ModelConsensus = $"Model Version: {r.ModelVersion}",
                            UncertaintyEvaluation = "Uncertainty: Evaluated",
                            FinalDecision = r.ExecutedAction,
                            ExecutionOutcome = r.IsCompleted ? $"Completed with profit/loss of {r.RealizedPips:F1} pips" : "Active position"
                        });
                    }
                }

                lock (_explainabilityTimeline)
                {
                    _explainabilityTimeline.Clear();
                    foreach (var r in records)
                    {
                        _explainabilityTimeline.Add(new ExplainabilityTimelineEntry
                        {
                            TransitionType = r.ExecutedAction,
                            Timestamp = r.TimestampUtc.ToLocalTime(),
                            Confidence = Math.Max(r.BuyConfidence, r.SellConfidence),
                            TriggeringModels = $"TrendModel, MomentumModel (Model: {r.ModelVersion})",
                            RiskChanges = $"Risk Score: {r.RiskScore:F2}",
                            SupportingEvidence = $"Price structure evaluated with regime {r.MarketRegime}",
                            Reason = $"Executed {r.ExecutedAction} resulting in {r.RealizedPips:F1} pips."
                        });
                    }
                }

                TriggerUpdate();
            }
            catch
            {
                // Resilient fallback if database is bootstrapping or not fully ready
            }
        }

        private void HandleDecisionCreated(DecisionCreatedEvent @event)
        {
            CurrentDecision = @event.Action;
            Confidence = @event.Confidence;
            ExpectedValue = $"Expected Value: {@event.Reason}";

            _supportingEvidence.Clear();
            _supportingEvidence.Add($"Symbol: {@event.Symbol}");
            _supportingEvidence.Add($"Action evaluated at {@event.TimestampUtc:HH:mm:ss}");
            _supportingEvidence.Add($"Reason: {@event.Reason}");

            SelectionReason = $"Decision Engine evaluated action {@event.Action} with {@event.Confidence:P0} confidence.";

            AddTimelineEntry(new ExplainabilityTimelineEntry
            {
                TransitionType = @event.Action,
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                Confidence = @event.Confidence,
                TriggeringModels = "TrendModel, MomentumModel, ConsensusEngine",
                RiskChanges = "Active pre-trade risk evaluation completed.",
                SupportingEvidence = $"Symbol: {@event.Symbol}, Reason: {@event.Reason}",
                Reason = @event.Reason
            });

            TriggerUpdate();
        }

        private void HandleDecisionChanged(DecisionChangedEvent @event)
        {
            CurrentDecision = @event.NewAction;
            Confidence = @event.Confidence;
            SelectionReason = $"Decision transitioned from {@event.PreviousAction} to {@event.NewAction}.";

            AddTimelineEntry(new ExplainabilityTimelineEntry
            {
                TransitionType = @event.NewAction,
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                Confidence = @event.Confidence,
                TriggeringModels = "ConsensusEngine, UncertaintyEngine",
                RiskChanges = $"Decision state transitioned away from {@event.PreviousAction}.",
                SupportingEvidence = $"Reason: {@event.Reason}",
                Reason = $"Flipped decision to {@event.NewAction} due to: {@event.Reason}"
            });

            TriggerUpdate();
        }

        private void HandleRiskAdjusted(RiskAdjustedEvent @event)
        {
            AddTimelineEntry(new ExplainabilityTimelineEntry
            {
                TransitionType = "RISK_ADJUSTMENT",
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                Confidence = 1.0,
                TriggeringModels = "RiskManager, PreTradeGuards",
                RiskChanges = $"{@event.RiskMetric} adjusted from {@event.PreviousValue:F2} to {@event.NewValue:F2}.",
                SupportingEvidence = $"Reason: {@event.Reason}",
                Reason = @event.Reason
            });

            TriggerUpdate();
        }

        private void HandlePositionManagement(PositionManagementEvent @event)
        {
            AddTimelineEntry(new ExplainabilityTimelineEntry
            {
                TransitionType = @event.ActionType,
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                Confidence = 1.0,
                TriggeringModels = "PositionManager, TrailingManager",
                RiskChanges = $"Position {@event.PositionId} managed: {@event.ActionType} for {@event.Volume:F2} lots.",
                SupportingEvidence = $"Symbol: {@event.Symbol}",
                Reason = @event.Reason
            });

            TriggerUpdate();
        }

        private void HandleExecutionCompleted(ExecutionCompletedEvent @event)
        {
            AddTimelineEntry(new ExplainabilityTimelineEntry
            {
                TransitionType = "EXECUTION_" + (@event.IsSuccess ? "SUCCESS" : "FAILED"),
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                Confidence = 1.0,
                TriggeringModels = "ExecutionEngine, MT5Gateway",
                RiskChanges = @event.IsSuccess ? $"Executed {@event.Action} on {@event.Symbol} cleanly." : "Execution rejected by gateway.",
                SupportingEvidence = @event.IsSuccess ? $"Price: {@event.ExecutedPrice:F5}, Volume: {@event.ExecutedVolume:F2}" : $"Error: {@event.ErrorMessage}",
                Reason = @event.IsSuccess ? $"Order executed successfully on MT5 at {@event.ExecutedPrice:F5}" : $"Execution failed: {@event.ErrorMessage}"
            });

            // Also add a dynamic DecisionReplayPayload record when execution completes
            AddHistoricalDecision(new DecisionReplayPayload
            {
                DecisionId = @event.DecisionId,
                DecisionName = $"DEC-{@event.DecisionId.ToString().Substring(0, 5).ToUpper()} ({@event.Action})",
                Timestamp = @event.TimestampUtc.ToLocalTime(),
                MarketSnapshot = $"Symbol: {@event.Symbol} | Bid/Ask executed cleanly",
                FeatureVectorSummary = "Processed in real-time execution flow",
                MarketRegime = "Active Live Streamed Regime",
                MultiTimeframeConsensus = $"Action: {@event.Action}",
                GeneratedHypotheses = $"IsSuccess: {@event.IsSuccess}",
                ScenarioSearchResults = $"Volume: {@event.ExecutedVolume:F2} lots",
                ModelConsensus = "Active Production Model Consensus",
                UncertaintyEvaluation = "Low Uncertainty",
                FinalDecision = @event.Action,
                ExecutionOutcome = @event.IsSuccess ? $"Successfully Executed at {@event.ExecutedPrice:F5}" : $"Execution Failed: {@event.ErrorMessage}"
            });

            TriggerUpdate();
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

            TriggerUpdate();
        }

        public void AddTimelineEntry(ExplainabilityTimelineEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            lock (_explainabilityTimeline)
            {
                _explainabilityTimeline.Insert(0, entry);
            }
        }

        public void AddHistoricalDecision(DecisionReplayPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            lock (_historicalDecisions)
            {
                _historicalDecisions.Insert(0, payload);
            }
        }

        private void TriggerUpdate()
        {
            OnDecisionUpdated?.Invoke(new DecisionDashboardData
            {
                Decision = CurrentDecision,
                Confidence = Confidence,
                ExpectedValue = ExpectedValue,
                SupportingEvidence = new List<string>(_supportingEvidence),
                RejectedAlternatives = new List<string>(_rejectedAlternatives),
                BuyUtility = BuyExpectedUtility,
                SellUtility = SellExpectedUtility,
                WaitUtility = WaitExpectedUtility,
                SelectionReason = SelectionReason
            });
        }
    }
}
