# ADR-003: Infrastructure & Data Architecture Design

## Status
Approved

## Context
A professional quantitative trading engine requires robust, high-performance, and scalable persistence, configuration, storage, and logging models. These mechanisms must handle high-throughput timeseries market tick feeds, keep detailed audit records for AI model training sessions, store learning trade experiences, and handle instant, portable workspace states.

To preserve the maintainability and purity of the trading system, several structural rules must be enforced:
1. **Purity of Domain Layer (`Nexus.Core`)**: The domain model has zero external package references (including Entity Framework Core, SQL clients, or concrete logging frameworks).
2. **Dual-Provider Relational Persistence**: SQLite for lightweight, instant, offline-simulation development, and PostgreSQL + TimescaleDB for monthly-partitioned, highly-concurrent production enterprise workloads.
3. **Decoupled Logging and File Systems**: The application must remain uncommitted to a single cloud provider or local disk-specific APIs, allowing easy migration between local storage and AWS S3/Azure Blob or various structured log aggregators.

## Decisions

### 1. Separation of Domain from Persistence Models
We enforce a strict separation between Core Domain entities and Persistence Database models. Domain classes are fully decoupled from persistence structures. We map domain models to EF persistence models (`AccountDbModel`, `OrderDbModel`, etc.) within adapters. This keeps raw SQL logic, index definitions, and framework-specific annotations completely separated from trading rules.

### 2. Dual Relational Providers
We officially maintain two relational pathways:
- **SQLite**: Managed via `SqliteDatabaseBootstrapper`, configured using lightweight soft-concurrency tokens, and instantly initialized on the local machine using `EnsureCreated()`. Used for simulation mode and local unit testing.
- **PostgreSQL**: Managed via `PostgreSqlDatabaseBootstrapper`, utilizing native monthly partitioned table structures (`market_ticks`, `market_bars`) and leveraging raw PostgreSQL `xmin` system transaction tokens to achieve optimal transaction-level optimistic concurrency. Used for live and paper execution environments.

The application remains agnostic of the active relational backend by communicating strictly through the `IDatabaseProvider` and `IConnectionFactory` abstractions.

### 3. Repository Decisions
To balance extensibility with simplicity, we employ:
- **Specialized Repositories** (`IAccountRepository`, `IOrderRepository`, etc.) for critical high-speed order execution and account state routines.
- **Generic Base Repository** (`IRepository<T>` and `EfRepository<T>`) for CRUD capabilities over non-core entities (such as configuration entities or future models) without introducing redundant boilerplates.

### 4. Storage Decoupling (`IFileStorage`)
We decouple storage via the `IFileStorage` port, and implement `LocalFileStorage` as the default host filesystem adapter. This establishes a clean path to support cloud storage adapters (e.g., `CloudStorage` using AWS S3) in future enterprise distributions without requiring changes to high-level application workflows.

### 5. Options Pattern Configuration
Configuration is structured using the standard .NET Options Pattern:
- `DatabaseSettings`: Handles provider profiles, connection credentials, and partitioning flags.
- `LoggingSettings`: Configures structured outputs, paths, and level thresholds.
- `ApplicationSettings`: Tracks sandbox/live execution profiles and environment names.

## Consequences
- **Absolute Separation of Concerns**: Quantitative indicator computations and strategy sandbox hosts are fully protected against database schema revisions.
- **No Direct Vendor Lock-in**: Logging and file storages can be scaled out or hot-swapped dynamically.
- **Full Offline Portability**: Developers can boot and run a perfect replica of the system locally in seconds using the SQLite engine.
