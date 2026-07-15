# Nexus Trading Engine (NTE)

[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-black?style=flat-square&logo=github)](https://github.com/Opselon/QuantitativeTradeBot)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue?style=flat-square)](https://dotnet.microsoft.com/)
[![Framework](https://img.shields.io/badge/.NET-10.0-blueviolet?style=flat-square)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-13.0-green?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![C++ Core](https://img.shields.io/badge/C++-Native%20Core-darkblue?style=flat-square)](https://visualstudio.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-SQLite%20%7C%20PostgreSQL-lightgrey?style=flat-square)](https://www.postgresql.org/)

The **Nexus Trading Engine (NTE)** is a production-oriented Windows algorithmic trading platform built with .NET 10, WPF, C#, and a native C++ quantitative core.

NTE connects to MetaTrader 5 through a dedicated bridge and enables users to:

*   Connect to an existing MetaTrader 5 trading account.
*   Configure and run automated trading strategies.
*   Place and close trades manually.
*   Track open positions continuously.
*   Apply strategy-driven exit, reversal, scaling, and hedging decisions.
*   Enforce risk controls before every trading action.
*   Operate in Simulation, Demo, or Live mode.
*   Use SQLite for simple local deployment or PostgreSQL for professional deployment.
*   Install and run the complete platform through a Windows installer.

> [!WARNING]  
> **Financial Risk Disclaimer:** NTE is a trading and automation platform, not financial advice. Live trading involves financial risk. Simulation and demo validation must be completed before enabling live execution.

---

## 📌 Table of Contents
- [High-Level Architecture](#high-level-architecture)
- [Instructions for AI Agents and Contributors](#instructions-for-ai-agents-and-contributors)
  - [Agent Operating Rules](#agent-operating-rules)
  - [Prohibited Patterns](#prohibited-patterns)
- [Product Vision](#product-vision)
- [Product Scope](#product-scope)
- [Supported Operating Modes](#supported-operating-modes)
- [Contributing](#-contributing)

---

## High-Level Architecture

The Nexus Trading Engine (NTE) is organized as a layered, decoupled, and extensible architecture following:

*   Domain-Driven Design (DDD)
*   Clean Architecture
*   Hexagonal Architecture (Ports & Adapters)
*   SOLID Principles
*   Dependency Injection
*   Async-First Design

### System Architecture Overview

```text
Nexus Trading Engine (NTE)

┌─────────────────────────────────────┐
│             Nexus.WpfUi             │
├─────────────────────────────────────┤
│ Operator Dashboard                  │
│ Strategy Management                 │
│ MT5 Trading Panel                   │
│ Position Monitoring                 │
│ Risk Monitoring                     │
│ Reporting & Analytics               │
│ Configuration Wizard                │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         Nexus.Application           │
├─────────────────────────────────────┤
│ Execution Coordinator               │
│ Strategy Coordinator                │
│ Risk Orchestrator                   │
│ Portfolio Coordinator               │
│ Position Tracking Engine            │
│ Trade Lifecycle Management          │
│ IMt5TradingService                  │
│ Application Commands & Queries      │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│            Nexus.Domain             │
├─────────────────────────────────────┤
│ Orders                              │
│ Positions                           │
│ Trades                              │
│ Accounts                            │
│ Strategies                          │
│ Risk Rules                          │
│ Portfolio Models                    │
│ Domain Events                       │
│ Value Objects                       │
│ Specifications                      │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│         Nexus.Infrastructure        │
├─────────────────────────────────────┤
│ MT5 Bridge Adapter                  │
│ Real MT5 Trading Service            │
│ Simulated Trading Service           │
│ Routing Trading Service             │
│ PostgreSQL Persistence              │
│ SQLite Persistence                  │
│ Background Workers                  │
│ Event Processing                    │
│ Logging & Audit                     │
│ Configuration Providers             │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│          Native C++ Core            │
├─────────────────────────────────────┤
│ EMA                                 │
│ SMA                                 │
│ RSI                                 │
│ ATR                                 │
│ Statistical Models                  │
│ Quantitative Calculations           │
│ Optimization Algorithms             │
└─────────────────────────────────────┘
```

---

## 🛠 Instructions for AI Agents and Contributors

Before modifying any code, every AI agent and contributor must:

1. Read this `README.md` completely.
2. Read all relevant files under `.project/`.
3. Read all relevant files under `.nexus_docs/`.
4. Inspect the existing solution and project structure.
5. Inspect existing implementations before creating new abstractions or files.
6. Read the latest progress, TODO, changelog, project-state, and next-session documents.
7. Run the baseline build and relevant tests before making changes.
8. Review build errors, analyzer warnings, test failures, and documented known issues.
9. Preserve existing architectural boundaries and naming conventions.
10. Avoid assuming that a requested class, interface, DTO, command, or service does not already exist.

> [!IMPORTANT]  
> Repository documentation is part of the implementation. A feature is not complete until the relevant `.project/` and `.nexus_docs/` documents are synchronized with the code.

### Agent Operating Rules

AI agents must:

*   **Prefer Extension:** Prefer extending existing abstractions over creating parallel abstractions.
*   **Scoped Changes:** Keep changes scoped to the requested stage.
*   **Preserve Worktree:** Preserve unrelated user changes in a dirty worktree. Never revert or overwrite unrelated work.
*   **Report Anomalies:** Stop and report unexpected changes that appear during implementation.
*   **Asynchronous I/O:** Use async APIs for I/O operations.
*   **Cancellation Support:** Propagate `CancellationToken` through cancellable workflows.
*   **Test Driven:** Add tests for new behavior and regressions.
*   **Verification:** Build and test before reporting completion. Report any test or build step that could not be executed.
*   **Code Review:** Perform a final code review before committing.
*   **Commit Rules:** Commit only when the task explicitly requires a commit.
*   **Security:** Never place credentials, account passwords, connection secrets, or private keys in source control.

### Prohibited Patterns

The following patterns are prohibited unless an existing documented exception explicitly permits them:

| Project Area | Prohibited Pattern | Reason |
| :--- | :--- | :--- |
| **WPF / UI** | Business logic in WPF Views or code-behind. | Violates MVVM and impedes testability. |
| **UI** | Direct MT5 Bridge access from the UI. | Violates Clean Architecture and layer isolation. |
| **UI** | Direct EF Core or database access from the UI. | Violates separation of concerns. |
| **Core / Domain** | Infrastructure-specific types leaking into the Domain layer. | Couples core logic to external tools/frameworks. |
| **Async Operations**| Blocking asynchronous code with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`. | Highly prone to causing application deadlocks. |
| **Workflows** | `Thread.Sleep` in application workflows. | Degrades threadpool efficiency and scheduler predictability. |
| **Architecture** | Unobserved fire-and-forget tasks or static service locators. | Reduces predictability and complicates dependency resolution. |
| **Security** | Plain-text credential storage in configuration. | High security vulnerability. |
| **User Experience** | Raw network or database exceptions displayed to end users. | Poor UX and potential information leakage. |
| **Native Core** | Native C++ code making strategy, portfolio, risk, or execution-policy decisions. | The C++ layer is dedicated purely to high-performance math and statistical calculations. |

---

## 🎯 Product Vision

The final product is distributed as a Windows installer:

```text
NexusTradingEngine-Setup.exe
```

After installation, the user should be able to:

```mermaid
graph TD
    A[Launch Configuration Wizard] --> B[Select SQLite or PostgreSQL]
    B --> C[Initialize/Validate Database]
    C --> D[Configure MT5 Terminal Path]
    D --> E[Login to MT5 Account]
    E --> F[Verify MT5 Bridge Connectivity]
    F --> G[Configure Strategies & Risk Profiles]
    G --> H[Test in Simulation or Demo Mode]
    H --> I[Confirm Safety Prompts to Enable Live Mode]
```

The application is structured to support both simple single-machine installations and advanced professional deployments without requiring modifications to core domain behavior.

---

## 🔍 Product Scope

NTE is responsible for:

*   Market-data ingestion and processing.
*   Strategy execution and signal generation.
*   Risk validation.
*   Order and position lifecycle orchestration.
*   MetaTrader 5 trade execution.
*   Position synchronization and reconciliation.
*   Continuous open-position tracking.
*   Portfolio and exposure monitoring.
*   Manual operator controls.
*   Persistent trade, audit, and configuration data.
*   Reporting and operational diagnostics.
*   High-performance quantitative calculations through native C++.

*Note: While MetaTrader 5 is the first official broker integration, the architecture remains decoupled to support future integrations such as FIX, REST, or WebSocket adapters.*

---

## ⚙️ Supported Operating Modes

### Simulation Mode
Simulation mode runs against deterministic, in-process simulated adapters without transmitting orders to an external broker.
*   **Purpose:** Development, automated testing, strategy validation, demonstrations, and failure-recovery testing.
*   **Behavior:** Uses the same Application-layer contracts as real execution to guarantee consistent workflow validation.

### Demo Mode
Demo mode connects to an MT5 demo account and sends actual broker requests to a non-production, simulated environment.
*   **Purpose:** Forward testing, broker-specific behavior validation, latency/slippage observation, and position reconciliation checks.

### Live Mode
Live mode routes orders to a live trading account.
*   **Requirements:** Explicit user activation, healthy connectivity status, active risk controls, complete audit logging, and clear visual cues within the UI to prevent accidental live execution or run-time operational mistakes.

---

## 🤝 Contributing

We welcome contributions from developers, quantitative researchers, and algorithmic trading enthusiasts to help improve the **Nexus Trading Engine**. 

### How to Contribute
*   **Enhance Strategies:** Implement new quantitative models or improve existing indicators in the C++ Core.
*   **Optimize the Core:** Refine async workflows, improve database persistence, or optimize the MT5 Bridge adapter.
*   **UI/UX Improvements:** Enhance the WPF dashboard's responsiveness and analytics visualization.
*   **Testing:** Write unit, integration, and regression tests to maintain platform stability.

To get started, please review the contribution guidelines outlined above, explore the existing codebase, and submit your Pull Request to the [GitHub Repository](https://github.com/Opselon/QuantitativeTradeBot).




<!-- NEXUS_AUTO_DOC_START -->

## 🏛️ Nexus Trading Engine (NTE) Architecture Summary
> **Architecture Style:** Decoupled Hexagonal / Clean Architecture + Search-Driven Decision Engine

### 🧩 Core System Layers
- 🔴 **`Nexus.Core` (Domain Layer):** Zero external dependencies. Defines the complete trading domain model, including `MarketState`, `MarketVector`, `TradeDecision`, `ScenarioScore`, `PatternMemory`, `EvaluationResult`, value objects (`Symbol`, `Money`, `LotSize`), and core interfaces for Strategy Runtime, Decision Engine, Risk Management, Neural Evaluation, Scenario Search, and Experience Database. Uses a *Zero-Allocation Tick Path* for deterministic, high-performance execution.
- ⚙️ **`Nexus.Application` (Orchestration):** Implements the complete Decision Pipeline, including Market State construction, Decision Generation, Scenario Search, Scenario Evaluation, Execution Coordination, Pattern Matching, Strategy Runtime, Experience Collection, AI orchestration, Risk Evaluation, and transformation of live market data into probabilistic trading decisions.
- 🔌 **`Nexus.Infrastructure` (Adapters):** Infrastructure adapters for MetaTrader 5 Bridge, EF Core persistence, Background Workers, Tick Streaming, Historical Data ingestion, Time-Series storage, Experience Database persistence, Training Dataset generation, Logging, Recovery services, and external platform integrations while keeping the domain completely isolated.
- ⚡ **`Nexus.Native.Core` (C++20):** High-performance quantitative computation engine accessed through P/Invoke. Responsible for ultra-low latency Market Vector generation, feature extraction, statistical calculations, numerical optimization, pattern processing, search acceleration, and future native AI optimizations beyond the .NET runtime.
- 🖥️ **`Nexus.WpfUi` (.NET 10 / WPF Layer):** Modern desktop workstation for monitoring Market States, Decision Engine execution, Pattern Memory, Experience Database, AI evaluation, Training progress, Strategy Runtime, Diagnostics, Bridge communication, and live execution management.

### 🧠 Intelligence & Execution Subsystems
- 🤖 **`Nexus.AI`:** AI-assisted Market Intelligence layer responsible for feature learning, neural evaluation, probabilistic scoring, confidence estimation, pattern recognition, and continuous model training. AI assists the decision engine instead of replacing it, following the philosophy of modern search engines such as Stockfish.
- 📚 **Experience & Training Engine:** Continuously records every Market State, generated decision, execution result, market evolution, and trade outcome. Builds an Experience Database that produces structured datasets for offline and online training, improving future evaluation, scenario ranking, pattern recognition, and decision quality.
- 🔍 **Search & Decision Engine:** Core intelligence of NTE inspired by modern chess engine architecture. Generates multiple candidate trading decisions, explores probabilistic market scenarios, prunes low-quality branches, ranks promising paths, combines historical experience with AI evaluation, and selects the highest-confidence decision under defined risk constraints.
- 📈 **MetaTrader 5 Integration Layer:** MT5 serves exclusively as the execution and market data platform. Supplies ticks, positions, account information, and order execution while remaining completely decoupled from the decision engine, allowing future support for additional trading platforms.

### 📊 Latest Build & Commit Metadata
| Field | Value |
| --- | --- |
| **Commit Message** | feat: establish Phase 03 infrastructure foundation |
| **Author** | google-labs-jules[bot] |
| **Branch** | `feat/infrastructure-foundation-phase-03-6605060012739411737` |
| **Run Number** | `71` |
| **Commit SHA** | `a0f7ada93f4aa6c695594ed8784444752b39acf8` |
| **Generated At** | `2026-07-15 23:33:15 UTC` |

---
### 📂 Interactive Project Structure Tree
<details open>
<summary><b>Click to expand Project Tree (Filtered with WPF, .NET, C/C++, CMake & MQL5 files)</b></summary>

```text
├── .github/
│   └── workflows/
│       ├── dotnet-build.yml
│       └── release.yml
├── .nexus_docs/
│   ├── 01_ARCHITECTURE.md
│   ├── 02_DATABASE_SCHEMA.md
│   ├── 03_PROGRESS.md
│   ├── 04_NEXT_STEPS.md
│   ├── 05_EXECUTION_PIPELINE.md
│   ├── 06_STRATEGY_RUNTIME.md
│   ├── 07_NATIVE_ACCELERATION.md
│   ├── 08_MT5_PROTOCOL.md
│   ├── 08_SECURITY_MODEL.md
│   ├── 09_E2E_TEST_PLAN.md
│   ├── 10_OBSERVABILITY.md
│   ├── 11_LOCAL_VALIDATION.md
│   ├── 12_DESKTOP_CLIENT.md
│   ├── 13_RELEASE_ENGINEERING.md
│   └── MetaTrade5.md
├── .project/
│   ├── 00_MASTER_PLAN.md
│   ├── 01_ARCHITECTURE.md
│   ├── 08_MT5_PROTOCOL.md
│   ├── 13_EXECUTION_ENGINE.md
│   ├── 21_PROGRESS.md
│   ├── 22_TODO.md
│   ├── 23_NEXT_SESSION.md
│   ├── 25_DECISIONS.md
│   ├── 26_CHANGELOG.md
│   └── 30_PROJECT_STATE.md
├── docs/
│   ├── Architecture/
│   │   ├── ADR-002-Domain-Model-Design.md
│   │   └── ADR-003-Infrastructure-Data-Architecture.md
│   ├── 01_ARCHITECTURE.md
│   ├── 02_AI_ARCHITECTURE.md
│   ├── 03_DATA_FLOW.md
│   ├── 04_NATIVE_CORE_DESIGN.md
│   ├── 05_NEURAL_ENGINE_DESIGN.md
│   ├── 06_TRAINING_PIPELINE.md
│   ├── 07_MODEL_DEPLOYMENT.md
│   ├── 08_PERFORMANCE.md
│   ├── 09_TESTING_STRATEGY.md
│   ├── 10_DECISIONS.md
│   ├── 11_ROADMAP.md
│   ├── ACCUMULATOR_DESIGN.md
│   ├── AI_TRAINING_PIPELINE.md
│   ├── ARCHITECTURE.md
│   ├── CHANGELOG.md
│   ├── CODING_STANDARDS.md
│   ├── DATABASE.md
│   ├── DEPENDENCY_GRAPH.md
│   ├── NATIVE_ENGINE.md
│   ├── PATTERN_MEMORY.md
│   ├── PROGRESS.md
│   └── ROADMAP.md
├── MQL5/
│   └── Experts/
│       └── Nexus/
│           ├── NexusBridge.mq5
│           └── ReadMe.md
├── native/
│   ├── Nexus.Native/
│   │   ├── NexusNative.cpp
│   │   └── NexusNative.h
│   └── build.sh
├── src/
│   ├── Nexus.AI/
│   │   ├── NeuralModelService.cs
│   │   └── Nexus.AI.csproj
│   ├── Nexus.Application/
│   │   ├── Analytics/
│   │   │   ├── IIndicatorEngine.cs
│   │   │   ├── INativeAnalyticsEngine.cs
│   │   │   ├── ManagedIndicatorEngine.cs
│   │   │   ├── NativeAnalyticsEngine.cs
│   │   │   └── NativeIndicatorEngine.cs
│   │   ├── Intelligence/
│   │   │   ├── AccumulatorService.cs
│   │   │   ├── CurrencyStrengthEngine.cs
│   │   │   ├── DecisionEngine.cs
│   │   │   ├── ExperienceCollector.cs
│   │   │   ├── MarketIntelligenceCoordinator.cs
│   │   │   ├── MultiTimeframeConsensusEngine.cs
│   │   │   ├── NativeMarketIntelligenceService.cs
│   │   │   ├── PatternMemory.cs
│   │   │   ├── ScenarioEvaluationEngine.cs
│   │   │   └── ScenarioSearchEngine.cs
│   │   ├── Mt5/
│   │   │   ├── ClosePositionResult.cs
│   │   │   ├── IMt5TradingService.cs
│   │   │   ├── OpenPositionDto.cs
│   │   │   └── PlaceOrderResult.cs
│   │   ├── Mt5Bridge/
│   │   │   └── Contracts/
│   │   │       ├── BridgeError.cs
│   │   │       ├── BridgeMessageEnvelope.cs
│   │   │       ├── BridgeOrderExecutionStatus.cs
│   │   │       ├── BridgeOrderSide.cs
│   │   │       ├── BridgePositionDto.cs
│   │   │       ├── BridgePositionSide.cs
│   │   │       ├── ClosePositionRequest.cs
│   │   │       ├── ClosePositionResponse.cs
│   │   │       ├── GetAccountSnapshotRequest.cs
│   │   │       ├── GetAccountSnapshotResponse.cs
│   │   │       ├── GetOpenPositionsRequest.cs
│   │   │       ├── GetOpenPositionsResponse.cs
│   │   │       ├── PingRequest.cs
│   │   │       ├── PingResponse.cs
│   │   │       ├── PlaceOrderRequest.cs
│   │   │       └── PlaceOrderResponse.cs
│   │   ├── Observability/
│   │   │   ├── DiagnosticRingBuffer.cs
│   │   │   ├── LogEventIds.cs
│   │   │   ├── LoggingExtensions.cs
│   │   │   ├── LogSanitizer.cs
│   │   │   └── WorkflowContext.cs
│   │   ├── Pipeline/
│   │   │   ├── DefaultRiskManager.cs
│   │   │   ├── ExecutionAuditService.cs
│   │   │   ├── ExecutionCoordinator.cs
│   │   │   ├── ExecutionRequest.cs
│   │   │   ├── ExecutionResult.cs
│   │   │   ├── OrderIntent.cs
│   │   │   ├── OrderIntentFactory.cs
│   │   │   ├── PipelineContext.cs
│   │   │   ├── PreTradeRiskEvaluator.cs
│   │   │   ├── RiskDecision.cs
│   │   │   ├── SignalRouter.cs
│   │   │   └── TradeSignal.cs
│   │   ├── Ports/
│   │   │   ├── BridgeDiagnosticLogEntry.cs
│   │   │   ├── ExecutionCommand.cs
│   │   │   ├── ExecutionReport.cs
│   │   │   ├── GatewayConnectionStatus.cs
│   │   │   ├── IAccountRepository.cs
│   │   │   ├── IAppConfigurationService.cs
│   │   │   ├── IApplicationLogger.cs
│   │   │   ├── IConnectionFactory.cs
│   │   │   ├── IConnectionHealthMonitor.cs
│   │   │   ├── IDatabaseBootstrapper.cs
│   │   │   ├── IDatabaseProvider.cs
│   │   │   ├── IExecutionGateway.cs
│   │   │   ├── IFileStorage.cs
│   │   │   ├── IGatewaySession.cs
│   │   │   ├── IGatewaySessionFactory.cs
│   │   │   ├── IMarketDataFeed.cs
│   │   │   ├── IMarketDataRepository.cs
│   │   │   ├── IMt5AccountService.cs
│   │   │   ├── IMt5BridgeClient.cs
│   │   │   ├── IMt5BridgeService.cs
│   │   │   ├── IMt5ConnectionService.cs
│   │   │   ├── IMt5Session.cs
│   │   │   ├── IMt5TradeService.cs
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IPositionRepository.cs
│   │   │   ├── IRepository.cs
│   │   │   ├── ITradingPlatformConnector.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── PriceTickEnvelope.cs
│   │   ├── Security/
│   │   │   ├── InputValidator.cs
│   │   │   ├── ISecretStore.cs
│   │   │   └── SecurityConfiguration.cs
│   │   ├── Strategies/
│   │   │   ├── InMemoryStrategyStateStore.cs
│   │   │   ├── IStrategyHost.cs
│   │   │   ├── IStrategyRegistry.cs
│   │   │   ├── IStrategyStateStore.cs
│   │   │   ├── StrategyDescriptor.cs
│   │   │   ├── StrategyExecutionContext.cs
│   │   │   ├── StrategyHost.cs
│   │   │   ├── StrategyRegistry.cs
│   │   │   └── StrategySupervisor.cs
│   │   ├── Workflows/
│   │   │   ├── DTOs/
│   │   │   │   ├── AccountSnapshotDto.cs
│   │   │   │   ├── ConnectionProfileDto.cs
│   │   │   │   └── ConnectionTestResultDto.cs
│   │   │   ├── ClosePositionCommand.cs
│   │   │   ├── CreateConnectionProfileCommand.cs
│   │   │   ├── DeleteConnectionProfileCommand.cs
│   │   │   ├── GetAccountSnapshotQuery.cs
│   │   │   ├── GetOpenPositionsQuery.cs
│   │   │   ├── GetPersistenceOptionsQuery.cs
│   │   │   ├── InitializeDatabaseCommand.cs
│   │   │   ├── LaunchWorkspaceCommand.cs
│   │   │   ├── MigrateDatabaseCommand.cs
│   │   │   ├── PlaceOrderCommand.cs
│   │   │   ├── SelectPersistenceProviderCommand.cs
│   │   │   ├── TestMt5ConnectionCommand.cs
│   │   │   └── UpdateConnectionProfileCommand.cs
│   │   └── Nexus.Application.csproj
│   ├── Nexus.Core/
│   │   ├── DomainEvents/
│   │   │   ├── MarginCallEvent.cs
│   │   │   ├── MarketStateUpdatedEvent.cs
│   │   │   ├── OrderExecutedEvent.cs
│   │   │   ├── PositionClosedEvent.cs
│   │   │   ├── PositionOpenedEvent.cs
│   │   │   └── RiskLimitReachedEvent.cs
│   │   ├── Entities/
│   │   │   ├── Interfaces/
│   │   │   │   └── IExperienceDatabaseWriter.cs
│   │   │   ├── Account.cs
│   │   │   ├── AccumulatorState.cs
│   │   │   ├── Bar.cs
│   │   │   ├── Candle.cs
│   │   │   ├── ConsensusState.cs
│   │   │   ├── EvaluationResult.cs
│   │   │   ├── ExperienceRecord.cs
│   │   │   ├── ExperienceSample.cs
│   │   │   ├── FeatureDelta.cs
│   │   │   ├── MarketState.cs
│   │   │   ├── MarketStateScenario.cs
│   │   │   ├── MarketVector.cs
│   │   │   ├── MultiTimeframeSignal.cs
│   │   │   ├── Order.cs
│   │   │   ├── PatternMatchResult.cs
│   │   │   ├── Position.cs
│   │   │   ├── RiskState.cs
│   │   │   ├── ScenarioScore.cs
│   │   │   ├── ScenarioSearchNode.cs
│   │   │   ├── Tick.cs
│   │   │   └── TradeDecision.cs
│   │   ├── Enums/
│   │   │   ├── MarketRegime.cs
│   │   │   ├── OrderSide.cs
│   │   │   ├── PositionStatus.cs
│   │   │   ├── RiskLevel.cs
│   │   │   ├── TimeframeType.cs
│   │   │   └── TradeAction.cs
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   ├── InvalidPercentageException.cs
│   │   │   ├── InvalidPositionException.cs
│   │   │   ├── InvalidPriceException.cs
│   │   │   ├── InvalidRiskException.cs
│   │   │   └── InvalidVolumeException.cs
│   │   ├── Interfaces/
│   │   │   ├── IAccumulatorService.cs
│   │   │   ├── ICurrencyStrengthEngine.cs
│   │   │   ├── IDecisionEngine.cs
│   │   │   ├── IExperienceCollector.cs
│   │   │   ├── IExperienceRecorder.cs
│   │   │   ├── IMarketEvaluator.cs
│   │   │   ├── IMultiTimeframeConsensusEngine.cs
│   │   │   ├── INativeCoreService.cs
│   │   │   ├── INeuralModelService.cs
│   │   │   ├── IPatternMemory.cs
│   │   │   ├── IPositionManager.cs
│   │   │   ├── IRiskManager.cs
│   │   │   ├── IScenarioEvaluationEngine.cs
│   │   │   ├── IScenarioSearchEngine.cs
│   │   │   ├── IStrategy.cs
│   │   │   ├── ITradingDecisionEngine.cs
│   │   │   └── ITrailingManager.cs
│   │   ├── ValueObjects/
│   │   │   ├── LotSize.cs
│   │   │   ├── MarketSession.cs
│   │   │   ├── Money.cs
│   │   │   ├── Percentage.cs
│   │   │   ├── Price.cs
│   │   │   ├── RiskAmount.cs
│   │   │   ├── Symbol.cs
│   │   │   ├── Timeframe.cs
│   │   │   └── Volume.cs
│   │   └── Nexus.Core.csproj
│   ├── Nexus.Desktop/
│   │   ├── Converters/
│   │   │   ├── EqualityToBooleanConverter.cs
│   │   │   └── ProfitToBrushConverter.cs
│   │   ├── Models/
│   │   │   ├── DesktopOrderSide.cs
│   │   │   ├── DesktopPositionDto.cs
│   │   │   └── DesktopTradeResult.cs
│   │   ├── Services/
│   │   │   ├── DiagnosticService.cs
│   │   │   ├── IDiagnosticService.cs
│   │   │   ├── IMt5BridgeOperatorService.cs
│   │   │   ├── IMt5OperatorService.cs
│   │   │   ├── Mt5BridgeOperatorService.cs
│   │   │   └── Mt5OperatorService.cs
│   │   ├── ViewModels/
│   │   │   ├── Workspaces/
│   │   │   │   ├── DashboardViewModel.cs
│   │   │   │   ├── DiagnosticsViewModel.cs
│   │   │   │   ├── ManualDeskViewModel.cs
│   │   │   │   ├── MarketWatchViewModel.cs
│   │   │   │   ├── Mt5BridgeViewModel.cs
│   │   │   │   ├── SettingsViewModel.cs
│   │   │   │   └── TestConsoleViewModel.cs
│   │   │   ├── AsyncRelayCommand.cs
│   │   │   ├── DesktopPositionViewModel.cs
│   │   │   ├── DesktopSymbolViewModel.cs
│   │   │   ├── MainViewModel.cs
│   │   │   ├── Mt5TradingViewModel.cs
│   │   │   ├── NexusIntelligenceViewModel.cs
│   │   │   ├── RelayCommand.cs
│   │   │   └── ViewModelBase.cs
│   │   ├── Views/
│   │   │   ├── Workspaces/
│   │   │   │   ├── DashboardView.xaml
│   │   │   │   ├── DashboardView.xaml.cs
│   │   │   │   ├── DiagnosticsView.xaml
│   │   │   │   ├── DiagnosticsView.xaml.cs
│   │   │   │   ├── ManualDeskView.xaml
│   │   │   │   ├── ManualDeskView.xaml.cs
│   │   │   │   ├── MarketWatchView.xaml
│   │   │   │   ├── MarketWatchView.xaml.cs
│   │   │   │   ├── Mt5BridgeView.xaml
│   │   │   │   ├── Mt5BridgeView.xaml.cs
│   │   │   │   ├── SettingsView.xaml
│   │   │   │   ├── SettingsView.xaml.cs
│   │   │   │   ├── TestConsoleView.xaml
│   │   │   │   └── TestConsoleView.xaml.cs
│   │   │   ├── Mt5TradingPanel.xaml
│   │   │   ├── Mt5TradingPanel.xaml.cs
│   │   │   ├── NexusIntelligenceDashboard.xaml
│   │   │   └── NexusIntelligenceDashboard.xaml.cs
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── DarkTheme.xaml
│   │   ├── LightTheme.xaml
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Nexus.Desktop.csproj
│   ├── Nexus.Infrastructure/
│   │   ├── Adapters/
│   │   │   └── Mt5/
│   │   │       ├── RealMt5BridgeAdapter.cs
│   │   │       ├── RealMt5BridgeConnectionService.cs
│   │   │       ├── RealMt5BridgeSession.cs
│   │   │       ├── RealMt5TradingService.cs
│   │   │       ├── RoutingMt5AccountService.cs
│   │   │       ├── RoutingMt5ConnectionService.cs
│   │   │       ├── RoutingMt5TradeService.cs
│   │   │       ├── RoutingMt5TradingService.cs
│   │   │       ├── SimulatedConnectionHealthMonitor.cs
│   │   │       ├── SimulatedMt5AccountService.cs
│   │   │       ├── SimulatedMt5ConnectionService.cs
│   │   │       ├── SimulatedMt5Session.cs
│   │   │       ├── SimulatedMt5TradeService.cs
│   │   │       ├── SimulatedMt5TradingService.cs
│   │   │       └── SimulatedTradingPlatformConnector.cs
│   │   ├── Configuration/
│   │   │   ├── ApplicationSettings.cs
│   │   │   ├── DatabaseSettings.cs
│   │   │   └── LoggingSettings.cs
│   │   ├── Logging/
│   │   │   └── ApplicationLogger.cs
│   │   ├── Models/
│   │   │   ├── ModelMetadata.cs
│   │   │   ├── ModelStatus.cs
│   │   │   └── ModelVersion.cs
│   │   ├── Mt5Bridge/
│   │   │   ├── LocalHttpApiRoutes.cs
│   │   │   ├── LocalHttpApiServer.cs
│   │   │   ├── MarketDataPipeline.cs
│   │   │   ├── Mt5BridgeService.cs
│   │   │   └── TcpMt5BridgeClient.cs
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   ├── AccountConfiguration.cs
│   │   │   │   ├── OrderConfiguration.cs
│   │   │   │   ├── PositionConfiguration.cs
│   │   │   │   └── TradeConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   │   └── 20260101000000_InitialTradingState.cs
│   │   │   ├── Models/
│   │   │   │   ├── AccountDbModel.cs
│   │   │   │   ├── ExperienceDbModel.cs
│   │   │   │   ├── OrderDbModel.cs
│   │   │   │   ├── PositionDbModel.cs
│   │   │   │   └── TradeDbModel.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── AccountRepository.cs
│   │   │   │   ├── EfRepository.cs
│   │   │   │   ├── MarketDataRepository.cs
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── PositionRepository.cs
│   │   │   │   └── UnitOfWork.cs
│   │   │   ├── AppConfigurationService.cs
│   │   │   ├── ConnectionFactory.cs
│   │   │   ├── DatabaseProvider.cs
│   │   │   ├── DependencyInjection.cs
│   │   │   ├── DesignTimeNexusDbContextFactory.cs
│   │   │   ├── ExperienceDatabaseWriter.cs
│   │   │   ├── NexusDbContext.cs
│   │   │   ├── PostgreSqlDatabaseBootstrapper.cs
│   │   │   └── SqliteDatabaseBootstrapper.cs
│   │   ├── Security/
│   │   │   └── WindowsSecretStore.cs
│   │   ├── Storage/
│   │   │   └── FileStorage/
│   │   │       └── LocalFileStorage.cs
│   │   ├── Workers/
│   │   │   ├── ExecutionWorker.cs
│   │   │   ├── MarketDataIngestionWorker.cs
│   │   │   ├── RecoveryStartupService.cs
│   │   │   └── StrategyDispatchWorker.cs
│   │   └── Nexus.Infrastructure.csproj
│   ├── Nexus.Infrastructure.Native/
│   │   ├── NativeCoreInterop.cs
│   │   ├── NativeCoreSafeHandle.cs
│   │   ├── NativeCoreService.cs
│   │   └── Nexus.Infrastructure.Native.csproj
│   ├── Nexus.Native.Core/
│   │   ├── include/
│   │   │   └── nexus_core/
│   │   │       ├── accumulator.h
│   │   │       ├── core_runtime.h
│   │   │       ├── interop_abi.h
│   │   │       ├── market_state.h
│   │   │       └── market_vector.h
│   │   ├── src/
│   │   │   ├── accumulator.cpp
│   │   │   ├── core_runtime.cpp
│   │   │   ├── market_state.cpp
│   │   │   └── market_vector.cpp
│   │   ├── tests/
│   │   │   └── native_tests.cpp
│   │   └── CMakeLists.txt
│   └── Nexus.WpfUi/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── AssemblyInfo.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       └── Nexus.WpfUi.csproj
├── tests/
│   ├── Nexus.Tests.EndToEnd/
│   │   ├── Fixture/
│   │   │   ├── E2ETestHost.cs
│   │   │   └── TestOutputLogger.cs
│   │   ├── Mocks/
│   │   │   ├── MockE2EStrategy.cs
│   │   │   ├── SimulatedExecutionGateway.cs
│   │   │   └── SimulatedMarketDataFeed.cs
│   │   ├── E2EWorkflowTests.cs
│   │   └── Nexus.Tests.EndToEnd.csproj
│   ├── Nexus.Tests.Integration/
│   │   ├── GlobalUsings.cs
│   │   ├── Nexus.Tests.Integration.csproj
│   │   └── PersistenceIntegrationTests.cs
│   └── Nexus.Tests.Unit/
│       ├── Desktop/
│       │   ├── DesktopTests.cs
│       │   ├── Mt5BridgeOperatorTests.cs
│       │   ├── Mt5BridgeTests.cs
│       │   └── Mt5TradingViewModelTests.cs
│       ├── Entities/
│       │   ├── AccountTests.cs
│       │   ├── CandleAndEventTests.cs
│       │   ├── OrderAndPositionTests.cs
│       │   └── TickAndBarTests.cs
│       ├── Infrastructure/
│       │   └── InfrastructureTest.cs
│       ├── Intelligence/
│       │   ├── MarketIntelligenceTests.cs
│       │   ├── NativeBridgeTests.cs
│       │   └── StockfishTradingEngineTests.cs
│       ├── ValueObjects/
│       │   ├── MoneyAndLotSizeTests.cs
│       │   ├── NewValueObjectTests.cs
│       │   └── SymbolTests.cs
│       ├── GlobalUsings.cs
│       ├── IndicatorEngineTests.cs
│       └── Nexus.Tests.Unit.csproj
├── NexusTradingEngine.sln
└── README.md
```
</details>

### 📈 Source File Counts

| File Type | Count |
| --- | ---: |
| C# (.cs) | 290 |
| WPF (.xaml) | 15 |
| C/C++ Source | 12 |
| CMake | 1 |
| MQL5 (.mq5) | 1 |
| Projects (.sln, .csproj) | 11 |

### 🐞 Pipeline Diagnostics (CI Stage - Ubuntu)
- **Job Status:** success
#### 🔴 Errors
```text
No explicit C# errors.
```
#### 🟡 Warnings
```text
No explicit C# warnings.
```

### 🚀 Pipeline Diagnostics (Build Stage - Windows)
- **Job Status:** success

#### 🔴 Errors
```text
No C# errors.
```
#### 🟡 Warnings
```text
7>D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5TradingViewModelTests.cs(374,53): warning CS0067: The event 'Mt5TradingViewModelTests.FakeBridgeService.OnTickReceived' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
7>D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5TradingViewModelTests.cs(375,42): warning CS0067: The event 'Mt5TradingViewModelTests.FakeBridgeService.OnStatusChanged' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5TradingViewModelTests.cs(374,53): warning CS0067: The event 'Mt5TradingViewModelTests.FakeBridgeService.OnTickReceived' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5TradingViewModelTests.cs(375,42): warning CS0067: The event 'Mt5TradingViewModelTests.FakeBridgeService.OnStatusChanged' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
```

<!-- NEXUS_AUTO_DOC_END -->
