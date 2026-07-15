# Nexus Trading Engine - Changelog

This document tracks all additions, modifications, and updates made across the platform repositories.

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
