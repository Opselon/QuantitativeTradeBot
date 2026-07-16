# Changelog

All notable changes to the Nexus Trading Engine project will be documented in this file. This project adheres to Semantic Versioning.

---

## [Unreleased] - Phase 10 Active Integration & Simulation

### Added
- **Application Dashboard Services**: Engineered four decoupled services (`IMarketDashboardService`, `IDecisionDashboardService`, `IExecutionDashboardService`, `ITrainingDashboardService`) and `ISystemHealthMonitorService` to bridge views and ViewModels with core domain systems.
- **Premium Dark-Themed WPF UI Workspace**: Formulated a 4K-responsive, GPU-accelerated workspace layout with Fluent design paradigms inside `DashboardView.xaml`.
- **System Health Monitor Ribbon**: Created an institutional status ribbon displaying sub-millisecond latencies, active thread counts, memory footprints, and CPU load for C++ Native, Decision, Intelligence, Learning, Execution, DB, and MT5 systems.
- **Real-Time Vector Sparklines**: Implemented dynamic coordinates scaling in `DashboardViewModel` plotting historical prices on a vector-based `Polyline` inside the Market Panel.
- **Dynamic Risk Utilization Gauges**: Created color-shifting UI progress bars mapping Daily Loss, Single Position Exposure, and Cumulative Exposure risk with auto color transitions (Green, Amber, Red).
- **Interactive What-If Overrides**: Introduced adjustable sliders allowing simulated Volatility and Momentum variables to override scenarios and recalculate expected utilities dynamically.
- **Decision Replays & Explainability Timeline**: Formulated sequential AI execution logs (Timeline Entries) and a master-detail panel to replay and reconstruct previous trade decisions deterministically.
- **Airtight Security Live Gates**: Implemented an explicit permission confirmation popup requiring manual approval when toggling Live Mode, coupled with an immutable monospace Security Audit Log trail.
- **Desktop xUnit Test Suite**: Developed robust unit tests verifying safety-critical states, model parameter updates, timeline logs, and profile toggles within `tests/Nexus.Tests.Unit/Desktop/DashboardViewModelTests.cs`.
- **Comprehensive Documentation**: Drafted architectural guidelines under `docs/Architecture/ADR-010-Desktop-Architecture.md` and user instructions inside `docs/WPF_WORKSTATION.md`.

---

## [0.9.0] - Phase 09 Market Intelligence & Data Fusion Engine (Completed)

### Added
- Multi-Timeframe engine aligning trend, momentum, volatility, and structural patterns from M1 to D1 intervals.
- Market Regime Detector dynamically classifying nine distinct market states.
- High-Performance Tick Aggregator compiling raw ticks into standard candles.
- Cosine-similarity pattern-matching database for historical regime comparisons.

---

## [0.8.0] - Phase 08 Autonomous Decision Intelligence Engine (Completed)

### Added
- Stockfish-inspired tree scenario search evaluating optimal expected utility.
- Multi-Model consensus scoring combining trend, momentum, and pattern classifiers.
- Uncertainty Engine prioritizing protective WAIT actions during low-confidence regimes.

---

## [0.7.0] - Phase 07 Automated Execution Sandbox (Completed)

### Added
- Risk-Controlled Execution Engine wrapping order dispatch with safety limit rules.
- Isolated Paper/Live gateways routing through simulated engines or real MT5 endpoints.
- Persistent audit logging storing execution traces and errors inside `NexusDbContext`.

---

## [0.6.0] - Phase 06 Autonomous Offline-First Self-Learning Engine (Completed)

### Added
- Experience replay buffering organizing chronological and regime-based simulation samples.
- Validation engine performing out-of-sample and walk-forward verification.
- Timeframe learning pathways isolating Fast Scalping from Intraday and Swing strategies.

---

## [0.5.0] - Phase 05 Stateful Real-Time Ingestion & MT5 Handshake (Completed)

### Added
- Stateful MT5 expert advisor (`NexusBridge.mq5`) executing orders and syncing positions over TCP.
- Bridge Connection State machine tracking states from Stopped to Authenticated with visual node status updates.
- Real-time market data pipeline feeding normalized tick updates to Native intelligence.

---

## [0.4.0] - Phase 04 C++20 Quantitative Indicator Engine (Completed)

### Added
- High-performance bare-metal C++20 shared library (`nexus_native_core.dll` / `libnexus_native_core.so`).
- NNUE-style incremental feature calculation with SIMD-aligned contiguous structs.
- Safe P/Invoke wrappers managing native heap memory with SafeHandles.

---

## [0.3.0] - Phase 03 Infrastructure & Dual Relational Abstraction (Completed)

### Added
- Option-pattern parameters configuration.
- SQLite database provider alongside PostgreSQL monthly-partitioned high-volume tables.
- LocalFileStorage adapter with strict path traversal vulnerability prevention.

---

## [0.2.0] - Phase 02 Pure Core Domain Foundation (Completed)

### Added
- Framework-independent domain aggregate structures with zero external dependencies.
- Strongly-typed immutable Value Objects enforcing boundaries against primitive obsession.

---

## [0.1.0] - Phase 01 Solution Setup & Interop Boundary (Completed)

### Added
- Initial Hexagonal architecture solutions layout.
- Platform logging abstractions with automated secret redaction masks.
- Git-ignored central package management setups.
