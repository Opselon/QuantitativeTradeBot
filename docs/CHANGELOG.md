# Changelog

All notable changes to the Nexus Trading Engine project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.10.0] - Phase 10 - 2025-02-18

### Added
- **Decoupled Dashboard Services**: Authored application-layer services under `src/Nexus.Application/Dashboard/` to manage presentation data models and reactive streams.
  - `IMarketDashboardService` / `MarketDashboardService` (Regime, Quality, Consensuses)
  - `IDecisionDashboardService` / `DecisionDashboardService` (Decisions, Supporting/Rejected details, Expected Utility)
  - `IExecutionDashboardService` / `ExecutionDashboardService` (Profiles, Permission gates, Monaco account stats, Security logs)
  - `ITrainingDashboardService` / `TrainingDashboardService` (Model status, backtest performance, training history)
  - `ISystemHealthMonitorService` / `SystemHealthMonitorService` (Subsystem states, CPU/RAM performance, Thread pool, latencies)
- **Advanced Workstation UI View**: Overhauled `DashboardView.xaml` and `DashboardView.xaml.cs` to deliver a professional dark-themed, 4K-responsive quant terminal.
- **Explainability Timeline**: Real-time chronological list showing how and why the AI's decision evolved over time, tracking TransitionType, Timestamp, Confidence, Risk changes, supporting evidence, and reasons.
- **Deterministic Decision Replay**: Master-detail reasoning reconstruction view allowing operators to select historical decision payloads and deterministically review feature vectors, hypotheses, consensus weights, and execution outcomes.
- **System Health Monitor Panel**: Color-coded subsystem indicator lamps, monospace thread counters, and high-precision latency gauges (tick, decision, execution) suitable for production environments.
- **Airtight Security Guardrails**: Live trading controls are shielded behind verification gates requiring explicit user confirmations. Profile changes automatically revoke live permissions and are recorded inside the Security Audit Trail list.
- **Targeted Unit Tests**: Added complete coverage in `tests/Nexus.Tests.Unit/Desktop/DashboardViewModelTests.cs` verifying ViewModel states, dialog callbacks, live permissions, automatic revocation, and update notifications.

---

## [0.9.0] - Phase 09 - 2025-02-17

### Added
- **Decoupled Data Source Ports**: Abstract interface adapters for tick streaming, OHLC bars, volume, calendar, and news.
- **Multi-Timeframe Engine**: Centralized alignment of Trend, Momentum, and Price Structure across M1 to D1 chart intervals producing a weighted `ConsensusScore`.
- **Market Regime Classification**: Deterministic regime detector automatically evaluating 9 key structural regimes.
- **Market Quality Score Generator**: Multi-dimensional score evaluating Liquidity, Spread, Noise, and Execution Risk.
- **AI Feature Extraction Pipeline**: Sorted feature vectors for neural network inputs.
- **Cosine Similarity Pattern Memory**: Decoupled memory matching contract.

---

## [0.8.0] - Phase 08 - 2025-02-16

### Added
- **9-Stage Decision Pipeline**: Core orchestrator (`DecisionPipelineOrchestrator`) coordinating the entire quantitative reasoning pipeline.
- **Stockfish Scenario Search Tree**: Multi-depth simulation traversing BUY, SELL, WAIT, CLOSE, REDUCE, and ADD options scored by expected utility.
- **Hypothesis Engine**: Evaluates Trend Continuation, Trend Reversal, and Sideways Consolidation.
- **Multi-Model Consensus Aggregator**: Merges signals from specialized evaluators weighted by their confidence levels.
- **Explainability Payload**: Comprehensive telemetries reporting primary choices, alternative utilities, and risk boundaries.
- **Uncertainty Gate**: Automated protection prioritizing WAIT actions under conflicting or erratic indicators.

---

## [0.5.0] - Phase 05 - 2025-02-15

### Added
- Neural model valuations using ONNX Runtime with high-fidelity deterministic mathematical fallbacks.
- Live MetaTrader 5 bridge integration supporting bidirectional order execution and position synchronization over secure localhost TCP connection profile clients.
- Automated Symbol subscription watchdog routing live feed ticks directly to native core engines.
