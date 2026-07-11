# Structured Observability & Telemetry Model

This document outlines the production-grade structured logging, correlation propagation, and exception tracking design implemented in the Nexus Trading Engine.

---

## 1. Core Architecture

The observability layer is built directly into `src/Nexus.Application/Observability` to provide structured, low-overhead telemetry without external dependencies. This decoupling guarantees high performance while supporting advanced analytics backends (like Serilog, Elasticsearch, and Grafana).

### Abstractions:
- **`LogEventIds`**: Declares stable EventIds for all key lifecycle operations.
- **`WorkflowContext`**: Encapsulates metadata (CorrelationId, AccountId, StrategyId, etc.) passed downstream and attached to logging scopes.
- **`LogSanitizer`**: A regex-driven parser that enforces secure log redacting (masking keys, secrets, and connection strings).
- **`LoggingExtensions`**: Extensions extending `ILogger` with scoped workflow logging, structured event logs, and structured exception formats.

---

## 2. EventId Strategy

Nexus Trading Engine defines stable, named `EventId`s for major events:

| EventId | Event Name | Target Subsystem | Description |
| :---: | :--- | :--- | :--- |
| **1001** | `MarketDataReceived` | Ingestion | Triggered when a new tick is received. |
| **1002** | `ValidationRejected` | Ingestion | Malformed symbol/price inputs. |
| **2001** | `StrategyStarted` | Strategy | Emitted when strategy begins. |
| **2002** | `StrategyStopped` | Strategy | Emitted when strategy halts gracefully. |
| **2003** | `StrategyFailed` | Strategy | Strategy execution crashes. |
| **3001** | `SignalEmitted` | Execution | Emitted when a trade signal is created. |
| **3002** | `RiskRejected` | Execution | Rejection by risk checks. |
| **4001** | `OrderSubmitted` | Execution | Dispatched order command to broker. |
| **4002** | `OrderFilled` | Execution | Trade successfully filled on exchange. |
| **4003** | `OrderRejected` | Execution | Broker execution rejection. |
| **5001** | `RecoveryStarted` | Recovery | Host initialization state checks. |
| **5002** | `RecoveryCompleted` | Recovery | State reconstruction complete. |
| **6001** | `NativeComputeInvoked` | Analytics | Rolling indicators run on Native C++. |
| **6002** | `NativeFallbackUsed` | Analytics | Falling back to pure-managed code. |
| **7001** | `WorkerStartup` | Host | Worker background service starting. |
| **7002** | `WorkerShutdown` | Host | Worker background service stopping. |

---

## 3. Correlation & Context Propagation

Nexus propagates identifiers across the execution pipeline:
1. **Market Data Ingestion**: At the boundary, a new tick creates a `WorkflowContext` with a unique `CorrelationId` and `OperationId`.
2. **Strategy Dispatch**: Ticks and bars are routed to the supervisor. If a strategy fires a signal, it forwards the `CorrelationId` to the execution queue.
3. **Execution Pipeline**: `ExecutionCoordinator` wraps execution inside a scoped logger carrying:
   - `CorrelationId`
   - `OperationId`
   - `Workflow`
   - `StrategyId`
   - `Symbol`
   - `AccountId`
   - `OrderId`
   - `PositionId`
   - `Gateway`
   - `Subsystem`

This guarantees that a single tick trigger can be traced continuously from intake, through indicators, strategy decision, pre-trade risk, and finally gateway fill.

---

## 4. Secret Redaction Policy

Structured logs run through `LogSanitizer.Sanitize(args)` which automatically redacts:
- DB connections passwords (`Password`, `pwd`).
- Security authentication payloads (`secret`, `token`, `apikey`, `key`).
- Credentials in raw connection strings.

Any matched values are immediately masked to `******` before logs are outputted, ensuring sensitive details are never leaked.

---

## 5. Recovery Boundaries

Nexus divides recovery/restart state into two categories:

### A. Persisted State (Must Survive Restart)
The following objects are considered fully persistent and are recovered from PostgreSQL/EF Core:
- **`Account`**: Balances, margin, equity, and leverage limits.
- **`Position`**: Entry price, current price, volume, SL, TP, and Unrealized Pnl.
- **`Order`**: TicketId, symbol, volume, status (`Filled`, `Rejected`, `Pending`).

### B. Transient State (Intentionally Ephemeral)
The following runtime objects are intentionally ephemeral and reset on reboot:
- **Active host registries** (Strategies are registered cleanly on host startup).
- **In-memory thread-safe queues** (`System.Threading.Channels` channels for tick/bar buffering).
- **Transient active indicators caches**.

---

## 6. CI/E2E Diagnostics Approach

During test execution inside the CI runner, logs are routed using:
- **`TestOutputLogger`**: An implementation of `ILogger` routing structured log messages directly to xUnit's `ITestOutputHelper`.
- **Diagnostic Failures Reports**: On test failure, a custom reporter captures the exact contextual parameters (`CorrelationId`, `StrategyId`, `AccountId`, `Symbol`, `OrderId`, `PositionId`) and prints them alongside the exception trace.
- **Recovery Flow Checkpoints**: Clear, traceable events are outputted chronologically:
  - `[RecoveryStart]`
  - `[StateSnapshotLoaded]`
  - `[TradingStateRestored]`
  - `[RuntimeRehydrationBoundaryEvaluated]`
  - `[RecoveryCompleted]`
