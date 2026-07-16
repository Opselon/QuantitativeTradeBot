# Nexus Trading Engine - Changelog

This document tracks all additions, modifications, and updates made across the platform repositories.

---

## [Phase 08: Autonomous Decision Intelligence Engine] - 2026-04-07

This release delivers the Autonomous Decision Intelligence Engine (Phase 08), the central reasoning quantitative intelligence layer of the Nexus Trading Engine. It implements a complete 9-stage Decision Pipeline, an advanced Stockfish-inspired tree scenario search engine, competing market hypothesis engines, modular multi-model consensus, first-class explainability telemetry, and uncertainty-driven risk management.

### Added
* **src/Nexus.DecisionEngine/Nexus.DecisionEngine.csproj**: Class library targeting .NET 10.0 containing the central quantitative reasoning logic.
* **src/Nexus.DecisionEngine/DecisionPackage.cs**: Represents the ultimate, explainable decision package containing Selected Action, Confidence, Evidence, Alternatives, Risk Summary, Expected Outcome, and Execution Readiness.
* **src/Nexus.DecisionEngine/MarketHypothesis.cs**: Represents a competing market hypothesis with its probability, expected reward, risk, and expected utility.
* **src/Nexus.DecisionEngine/IModelEvaluator.cs**: Modular contract and models supporting multi-model consensus aggregation.
* **src/Nexus.DecisionEngine/ModelEvaluators.cs**: Implements specialized evaluators (Trend, Volatility, Momentum, Liquidity, Pattern, Order Flow, Macro) alongside a stub market memory.
* **src/Nexus.DecisionEngine/IMarketMemory.cs**: Interface enabling retrieval of historical match success rates and pattern frequencies without tight coupling to training.
* **src/Nexus.DecisionEngine/DecisionScenarioSearchEngine.cs**: Stockfish-inspired tree-search algorithm evaluating action paths over an expanded action space.
* **src/Nexus.DecisionEngine/MarketHypothesisEngine.cs**: Compares competing hypotheses (Trend Continuation vs Trend Reversal vs Sideways Mean Reversion).
* **src/Nexus.DecisionEngine/MultiModelConsensusAggregator.cs**: Weighs and merges modular model evaluation results into an aggregated score, confidence, and dominant bias.
* **src/Nexus.DecisionEngine/UncertaintyEngine.cs**: Classifies uncertainty (High, Medium, Low, Unknown) to protect capital during market volatility or model divergence.
* **src/Nexus.DecisionEngine/DecisionPipelineOrchestrator.cs**: Coordinates the 9-stage pipeline from market snapshot to final executable decision packages.
* **tests/Nexus.Tests.Unit/DecisionEngine/DecisionEngineTests.cs**: Comprehensive test suite verifying scenario search ranking, hypothesis comparison, consensus, uncertainty triggers, and end-to-end pipeline execution.
* **docs/DECISION_ENGINE.md**: Engineering documentation detailing pipeline stages, responsibilities, and flows.
* **docs/Architecture/ADR-008-Decision-Intelligence-Architecture.md**: Architectural record explaining decoupling, tree-search utility math, and modular consensus.

### Modified
* **tests/Nexus.Tests.Unit/Nexus.Tests.Unit.csproj**: Added project reference to `Nexus.DecisionEngine.csproj`.
* **tests/Nexus.Tests.Unit/Intelligence/MarketIntelligenceTests.cs**: Resolved name collision for `DecisionEngine` type references.
* **docs/PROGRESS.md**: Marked Phase 08 complete.
* **docs/ROADMAP.md**: Incremented phase roadmap tracker.

---

## [Phase 07: Automated Execution Sandbox & Risk-Controlled Runtime] - 2026-04-06

This release delivers the Automated Execution Sandbox & Risk-Controlled Runtime (Phase 07), completing the professional-grade execution pipeline, risk gate controls, safe-mode profiles, position management synchronization, database auditing logs, and multi-dimensional unit test validations.

### Added
* **src/Nexus.Execution/Nexus.Execution.csproj**: Class library targeting .NET 10.0 containing the execution sandbox and risk-control layer.
* **src/Nexus.Execution/Enums/ExecutionProfile.cs**: Enum defining execution safety profiles (`Simulation`, `Paper`, `Live`).
* **src/Nexus.Execution/Enums/ExecutionState.cs**: Enum establishing the explicit states machine (`Created`, `Validated`, `Submitted`, `Accepted`, `Rejected`, `Filled`, `PartiallyFilled`, `Closed`).
* **src/Nexus.Execution/Domain/OrderRequest.cs**: Concept representing immutable trade/order requests (Symbol, Side, Volume, Entry, Stop Loss, Take Profit, Reason, State).
* **src/Nexus.Execution/Domain/ExecutionResult.cs**: Concept returning execution success, order/ticket IDs, error reason, and execution latency.
* **src/Nexus.Execution/Domain/PositionSnapshot.cs**: Concept returning current open position status, P/L, and risk exposures.
* **src/Nexus.Execution/Gateways/IExecutionGateway.cs**: Port abstraction hiding individual brokers.
* **src/Nexus.Execution/Gateways/SimulationExecutionGateway.cs**: Virtual in-memory adapter for safe sandboxed paper-trading.
* **src/Nexus.Execution/Gateways/MT5ExecutionGateway.cs**: MetaTrader 5 adapter translating requests and mapping open position fields.
* **src/Nexus.Execution/Risk/IRiskExecutionGuard.cs**: Port interface for pre-trade risk validations.
* **src/Nexus.Execution/Risk/RiskExecutionGuard.cs**: Implements multi-dimensional risk rules including mandatory Stop Loss validation, daily loss caps, position lot limits, cumulative exposure limits, risk percentage relative to equity, and regime restrictions.
* **src/Nexus.Execution/Management/PositionManager.cs**: Tracking engine supporting open/closed/pending collections, partial close splits, stop modifications, and state synchronizations.
* **src/Nexus.Execution/Events/OrderSubmittedEvent.cs**: Event fired when order is dispatched.
* **src/Nexus.Execution/Events/OrderFilledEvent.cs**: Event fired when order successfully executes at the gateway.
* **src/Nexus.Execution/Events/OrderRejectedEvent.cs**: Event fired when order is rejected by risk or gateway.
* **src/Nexus.Execution/Events/PositionClosedEvent.cs**: Event fired on position closure with realized P/L details.
* **src/Nexus.Execution/Auditing/IExecutionAuditService.cs**: Port interface for DB auditable records.
* **src/Nexus.Execution/RiskControlledExecutionEngine.cs**: Sequential coordination pipeline executing trades securely (Decision -> Risk Validation -> Permission -> Routing).
* **src/Nexus.Infrastructure/Persistence/Models/ExecutionErrorDbModel.cs**: Mapped DB model for archiving execution failures.
* **src/Nexus.Infrastructure/Persistence/Configurations/ExecutionErrorConfiguration.cs**: EF Core mapping configuration for execution errors.
* **src/Nexus.Infrastructure/Persistence/Repositories/DbExecutionAuditService.cs**: DB persistence adapter implementing `IExecutionAuditService` saving orders, positions, and errors.
* **tests/Nexus.Tests.Unit/Execution/ExecutionEngineTests.cs**: Comprehensive test suite verifying order lifecycles, risk rejections, simulation runs, partial close calculations, and MT5 gateway mocking.
* **docs/Architecture/ADR-007-Execution-Architecture.md**: Architectural record outlining broker isolation, risk gates, simulation-first approach, and the execution lifecycle.
* **docs/EXECUTION_ENGINE.md**: Technical documentation detailing execution architectures, flows, state machines, and experience engine integrations.

### Modified
* **src/Nexus.Infrastructure/Persistence/NexusDbContext.cs**: Added `DbSet<ExecutionErrorDbModel> ExecutionErrors` for audited errors.
* **src/Nexus.Infrastructure/Persistence/DependencyInjection.cs**: Registered `DbExecutionAuditService` in the service container.
* **src/Nexus.Infrastructure/Persistence/Scripts/001_create_schema.sql**: Appended SQL script defining `execution_errors` table for Postgres.
* **src/Nexus.Infrastructure/Nexus.Infrastructure.csproj**: Added project reference to `Nexus.Execution.csproj`.
* **docs/PROGRESS.md**: Marked Phase 07 Completed and Phase 08 Pending.
* **docs/ROADMAP.md**: Moved Phase 07 to completed status.

---

## [Phase 06: Autonomous Learning & Experience Engine Foundation] - 2026-04-05

This release delivers the Autonomous Learning and Experience Engine foundation. It introduces RL-inspired experience buffers, sophisticated multi-dimensional quantitative reward evaluation, model registries with robust security controls, multi-gate statistical validation engines, separate timeframe learning category managers, and a unified offline-first training pipeline orchestrator.

### Added
* **src/Nexus.Training/Nexus.Training.csproj**: Class library targeting .NET 10.0 for learning, training, and experience processing workflows.
* **src/Nexus.Training/ExperienceEngine.cs**: Converts trading decisions and market parameters into structured `ExperienceSample` objects, supporting outcome finalization.
* **src/Nexus.Training/ExperienceReplayBuffer.cs**: Thread-safe reinforcement learning experience replay storage supporting randomized, chronological, and regime-based sampling.
* **src/Nexus.Training/RewardEvaluator.cs**: Multi-dimensional quantitative reward calculator prioritizing decision quality, risk-adjusted performance, timing, and drawdown over raw profit.
* **src/Nexus.Training/ModelVersionInfo.cs**: Data models tracking model version, creation date, training dataset version, validation score, performance metrics, and status.
* **src/Nexus.Training/IModelStorage.cs**: Port interface for saving, loading, and deleting raw model weights and ONNX binaries.
* **src/Nexus.Training/FileModelStorage.cs**: File-based implementation of model storage, utilizing strict path-traversal prevention checks.
* **src/Nexus.Training/ModelRegistry.cs**: Thread-safe registry for model tracking, managing life cycle state transitions, and active promotions.
* **src/Nexus.Training/ValidationResult.cs**: Detailed output from the validation system capturing results of the multi-gate checks.
* **src/Nexus.Training/ValidationEngine.cs**: Evaluates candidate models against Backtesting, Walk-Forward, Out-of-Sample, and Paper Trading validation gates.
* **src/Nexus.Training/TimeframeLearningManager.cs**: Partitions datasets, metrics, and models separately for Scalping (M1, M5, M15), Intraday (M30, H1), and Swing (H4, D1).
* **src/Nexus.Training/TrainingPipeline.cs**: Unified orchestrator implementing the complete offline-first learning lifecycle.
* **tests/Nexus.Tests.Unit/Training/TrainingEngineTests.cs**: Comprehensive unit tests covering experience creation, replay buffer, reward evaluator, model registry, validation, and training pipeline cycles.
* **docs/Architecture/ADR-006-Learning-System-Architecture.md**: Architectural record explaining offline-first, experience replay, validation gates, and timeframe separation choices.
* **docs/TRAINING_ENGINE.md**: Complete engineering documentation detailing learning lifecycles, data flows, and future AI integration plans.

### Modified
* **src/Nexus.Core/Entities/ExperienceSample.cs**: Extended domain entity with continuous learning properties (`Confidence`, `ReasoningMetadata`, `Risk`, `Reward`, `Result`, and `QualityScore`) with complete backward-compatibility.
* **tests/Nexus.Tests.Unit/Nexus.Tests.Unit.csproj**: Added project reference to `Nexus.Training.csproj`.
* **docs/PROGRESS.md**: Marked Phase 05 and Phase 06 complete.
* **docs/ROADMAP.md**: Moved Phase 05 and Phase 06 to completed status, outlining Phase 07 targets.

---

## [Phase 05: Autonomous Strategy Runtime & Neural Evaluators] - 2026-04-04

This release completes the Autonomous Strategy Runtime and Neural Evaluators. It integrates Microsoft.ML.OnnxRuntime for deep model evaluations, implements stateful live market intelligence orchestration via NativeMarketIntelligenceService, and integrates MetaTrader 5 Bridge Operations.

### Added
* **src/Nexus.AI/NeuralModelService.cs**: Loads ONNX model weights and evaluates inference scenarios with high-fidelity deterministic fallback.
* **src/Nexus.Application/Intelligence/NativeMarketIntelligenceService.cs**: Stateful orchestration connecting tick ingestion, C++ native core, and ONNX evaluations.

---

## [Phase 04: C++20 Quantitative Evaluation Engine Foundation] - 2026-04-03

This release delivers the high-performance native C++20 computation engine foundation. Inspired by chess-engine design patterns (Stockfish), this release provides low-latency market state representations, preallocated memory pools, incremental evaluation caching, scalable thread pools, and zero-allocation C# interop.

### Added
* **src/Nexus.Native.Core/include/nexus_core/market_state_native.h**: Aligned `MarketStateNative` representation containing comprehensive prices, volume, and indicator properties.
* **src/Nexus.Native.Core/include/nexus_core/market_evaluator.h**: Calculates `EvaluationResult` mapping component scores (Trend, Momentum, Liquidity, Risk).
* **src/Nexus.Native.Core/include/nexus_core/memory_pool.h**: Pre-allocated memory block template avoiding heap allocation overhead on hot paths.
* **src/Nexus.Native.Core/include/nexus_core/threading_foundation.h**: Lightweight task runner thread pool and generic queues.
* **src/Nexus.Native.Core/include/nexus_core/lock_free_foundation.h**: Standard lock-free interface designs for high-frequency market channels.
* **docs/Architecture/ADR-004-Native-Engine-Architecture.md**: Architecture Decision Record explaining C++20 choices, memory patterns, and interops.

### Modified
* **src/Nexus.Native.Core/include/nexus_core/interop_abi.h**: Integrated Phase 04 lifecycles (`NativeEngineInitialize`, `NativeEngineEvaluate`, `NativeEngineShutdown`, `RegisterLoggingCallback`).
* **src/Nexus.Native.Core/include/nexus_core/accumulator.h**: Implemented incremental `AccumulatorState`, `AccumulatorUpdate`, and fast `EvaluationCache`.
* **src/Nexus.Native.Core/include/nexus_core/market_vector.h**: Implemented spaceship-compared, memory-contiguous `FeatureVector`.
* **src/Nexus.Native.Core/src/core_runtime.cpp**: C-linkage implementation of all interop boundaries with `noexcept` safety and logging callback support.
* **tests/Nexus.Native.Core/tests/native_tests.cpp**: Comprehensive native test and microsecond-level latency performance benchmark suite.
* **docs/NATIVE_ENGINE.md**: Added pipeline diagrams, hot-path memory architectures, and the phase roadmap.
* **docs/PROGRESS.md**: Marked Phase 04 complete, detailing performance metrics.
* **docs/ROADMAP.md**: Moved Phase 04 to completed status, outlining Phase 05 targets.

---

## [Phase 03: Nexus.Infrastructure Foundation] - 2026-04-02

This release delivers the production-grade Infrastructure and Persistence foundation of the autonomous quantitative trading platform, enabling decoupled dual-mode database operations, options configuration, structured logging, file storage abstractions, and AI model version metadata tracking.

### Added
* **src/Nexus.Application/Ports/IDatabaseProvider.cs**: Port interface for querying active database provider metadata.
* **src/Nexus.Application/Ports/IConnectionFactory.cs**: Port interface for abstracting database connections.
* **src/Nexus.Application/Ports/IRepository.cs**: Port interface for generic EF Core-based repositories.
* **src/Nexus.Application/Ports/IApplicationLogger.cs**: Port interface for structured platform logging.
* **src/Nexus.Application/Ports/IFileStorage.cs**: Port interface for file-system and object storage operations.
* **src/Nexus.Infrastructure/Configuration/DatabaseSettings.cs**: Configuration option model mapping database providers.
* **src/Nexus.Infrastructure/Configuration/LoggingSettings.cs**: Configuration option model mapping log thresholds.
* **src/Nexus.Infrastructure/Configuration/ApplicationSettings.cs**: Configuration option model mapping system execution modes.
* **src/Nexus.Infrastructure/Logging/ApplicationLogger.cs**: Standard logging wrapper adapter implementing `IApplicationLogger`.
* **src/Nexus.Infrastructure/Storage/FileStorage/LocalFileStorage.cs**: Local filesystem IO adapter implementing `IFileStorage` with path traversal guards.
* **src/Nexus.Infrastructure/Persistence/DatabaseProvider.cs**: Implements `IDatabaseProvider` using active application configuration.
* **src/Nexus.Infrastructure/Persistence/ConnectionFactory.cs**: Implements `IConnectionFactory` returning SQLite/PostgreSQL connection drivers.
* **src/Nexus.Infrastructure/Persistence/Repositories/EfRepository.cs**: Implements generic `IRepository<T>` using Entity Framework Core.
* **src/Nexus.Infrastructure/Models/ModelStatus.cs**: Enum defining life cycle states of AI quantitative models.
* **src/Nexus.Infrastructure/Models/ModelVersion.cs**: Model tracking specific AI model metadata and training details.
* **src/Nexus.Infrastructure/Models/ModelMetadata.cs**: Container mapping parent AI Model details and its active training versions.
* **tests/Nexus.Tests.Unit/Infrastructure/InfrastructureTest.cs**: Comprehensive unit tests covering generic repositories, file storage, database connections, and options binding.

### Modified
* **src/Nexus.Infrastructure/Persistence/DependencyInjection.cs**: Updated to register new options pattern bindings, file storages, database providers, and connection builders.
* **tests/Nexus.Tests.Unit/Nexus.Tests.Unit.csproj**: Added `Microsoft.EntityFrameworkCore.InMemory` package reference for in-memory database tests.

---

## [Phase 02: Nexus.Core Domain Foundation] - 2026-04-01

This release delivers the pure business domain foundation of the autonomous quantitative trading platform with zero external dependencies.

### Added
* **src/Nexus.Core/Exceptions/DomainException.cs**: Base exception class for all domain-specific errors.
* **src/Nexus.Core/Exceptions/InvalidPriceException.cs**: Exception thrown when price boundary validations fail.
* **src/Nexus.Core/Exceptions/InvalidRiskException.cs**: Exception thrown when risk limits or checks fail.
* **src/Nexus.Core/Exceptions/InvalidPositionException.cs**: Exception thrown when position operations violate state constraints.
* **src/Nexus.Core/Exceptions/InvalidVolumeException.cs**: Exception thrown when volume validation fails.
* **src/Nexus.Core/Exceptions/InvalidPercentageException.cs**: Exception thrown when percentage validation fails.
* **src/Nexus.Core/Enums/OrderSide.cs**: Domain enum representing Buy/Sell sides.
* **src/Nexus.Core/Enums/PositionStatus.cs**: Domain enum representing position lifecycle status.
* **src/Nexus.Core/Enums/TradeAction.cs**: Domain enum representing engine-driven execution decisions.
* **src/Nexus.Core/Enums/RiskLevel.cs**: Domain enum representing evaluated risk classification.
* **src/Nexus.Core/Enums/MarketRegime.cs**: Domain enum representing structural market characteristics.
* **src/Nexus.Core/Enums/TimeframeType.cs**: Domain enum representing discrete chart interval types.
* **src/Nexus.Core/ValueObjects/Price.cs**: Immutable, self-validating double-precision instrument price object.
* **src/Nexus.Core/ValueObjects/Volume.cs**: Immutable, self-validating trading or transaction volume object.
* **src/Nexus.Core/ValueObjects/Percentage.cs**: Immutable, self-validating percentage representation with fractional math behavior.
* **src/Nexus.Core/ValueObjects/RiskAmount.cs**: Immutable, self-validating capital/monetary risk representation.
* **src/Nexus.Core/ValueObjects/Timeframe.cs**: Immutable, self-validating timeframe interval abstraction mapping to TimeSpan durations.
* **src/Nexus.Core/ValueObjects/MarketSession.cs**: Immutable, self-validating representation of global active/overnight trading sessions.
* **src/Nexus.Core/Entities/Candle.cs**: Self-validating OHLCV price bar with price range constraint checking.
* **src/Nexus.Core/Interfaces/IMarketEvaluator.cs**: Port interface for assessing market regimes from candle streams.
* **src/Nexus.Core/Interfaces/ITradingDecisionEngine.cs**: Port interface for generating trade decisions from classified states.
* **src/Nexus.Core/Interfaces/IPositionManager.cs**: Port interface for position profit/loss synchronization and risk modification.
* **src/Nexus.Core/Interfaces/IExperienceRecorder.cs**: Port interface for learning feedback loops of decisions.
* **src/Nexus.Core/DomainEvents/PositionOpenedEvent.cs**: Simple domain event for position initialization tracking.
* **src/Nexus.Core/DomainEvents/PositionClosedEvent.cs**: Simple domain event for position termination and realized profit/loss.
* **src/Nexus.Core/DomainEvents/RiskLimitReachedEvent.cs**: Simple domain event for risk policy breaches.
* **src/Nexus.Core/DomainEvents/MarketStateUpdatedEvent.cs**: Simple domain event for classified market state adjustments.
* **tests/Nexus.Tests.Unit/ValueObjects/NewValueObjectTests.cs**: Unit test suite for new Value Objects (Price, Volume, Percentage, RiskAmount, Timeframe, MarketSession).
* **tests/Nexus.Tests.Unit/Entities/CandleAndEventTests.cs**: Unit test suite for Candle validation and domain events field mapping.

---

## [Phase 01: Platform Foundation] - 2026-03-31

This release establishes the architectural layout, core coding conventions, visual dependency maps, dual database adapters, native C++ interop specifications, and secure logging protocols.

### Added
* **docs/ARCHITECTURE.md**: Comprehensive structural documentation outlining Clean Architecture layers, Hexagonal Ports/Adapters design boundaries, and key asynchronous data-flow patterns.
* **docs/DEPENDENCY_GRAPH.md**: Complete project-by-project visual and matrix guide. Defines strict permission and restriction rules preventing circular dependencies or domain model contamination.
* **docs/NATIVE_ENGINE.md**: Complete technical overview of C++20 compiler optimization flags, AVX2-aligned memory layouts (`alignas(32)`), `NativeCoreSafeHandle` lifecycles, and managed C# fallback workflows.
* **docs/DATABASE.md**: Implementation details of dual persistence strategies (PostgreSQL/SQLite), raw monthly table partitioning schemes, specialized composite indexes, and optimistic concurrency patterns.
* **docs/CODING_STANDARDS.md**: Comprehensive list of engineering directives, naming guidelines, approved `#region` categories, and automated testing principles.
* **docs/ROADMAP.md**: Multi-stage developmental milestone tracker marking Phase 01 Completed and Phase 02/03 Pending.
* **docs/PROGRESS.md**: Phase 01 progress tracking report highlighting current architectural stability, known technical risks, and next phase checklists.
* **docs/CHANGELOG.md**: This document, cataloging all foundational releases.
