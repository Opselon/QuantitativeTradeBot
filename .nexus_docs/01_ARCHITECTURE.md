# Nexus Trading Engine (NTE) - Architecture Document

## 1. Architectural Style: Decoupled Hexagonal / Clean Architecture

The Nexus Trading Engine (NTE) strictly adopts **Hexagonal Architecture** (also known as Ports and Adapters or Clean Architecture) to isolate core business domain models, risk controls, and algorithmic strategy structures from concrete technical infrastructure, communication frameworks, and presentation logic.

```
       +-------------------------------------------------------+
       |                  Nexus.Infrastructure                  |
       |  +-------------------------------------------------+  |
       |  |                 Nexus.Application               |  |
       |  |  +-------------------------------------------+  |  |
       |  |  |                Nexus.Core                 |  |  |
       |  |  |                                           |  |  |
       |  |  | - Value Objects: Symbol, Money, LotSize   |  |  |
       |  |  | - Entities: Tick, Bar, Order, Position    |  |  |
       |  |  | - Interfaces: IStrategy, IRiskManager     |  |  |
       |  |  +-------------------------------------------+  |  |
       |  |                                                 |  |
       |  | - ExecutionCoordinator, BacktestRunner          |  |
       |  | - Ports (IExecutionGateway, IDbContext)          |  |
       |  +-------------------------------------------------+  |
       |                                                       |
       | - PostgreSQL (EF Core, Dapper Bulk Copy)              |
       | - MT5 gRPC / Named Pipes Bridge                       |
       | - Telemetry (Serilog, Prometheus)                     |
       +-------------------------------------------------------+
                                  |
                                  v
                             Nexus.WpfUi
                       (Dashboard, Live PnL,
                       Open Positions, Charts)
```

### Core Architecture Layers:
1. **Nexus.Core (The Domain Model):**
   - Zero external library dependencies, framework-agnostic.
   - Contains high-performance value objects (`Symbol`, `Money`, `LotSize`) and core domain entities (`Tick`, `Bar`, `Order`, `Position`, `Account`).
   - Declares the core contracts and algorithmic interfaces (`IStrategy`, `IRiskManager`, `ITrailingManager`).
   - Contains strict mathematical calculations (e.g., drawdown, margin, standard deviations, moving averages) without external dependencies.

2. **Nexus.Application (Orchestration & Application Services):**
   - Implements execution logic, order coordination, and historical backtesting runners.
   - Defines output and input Ports (interfaces such as `IExecutionGateway`, `IDbContext`, `IMarketDataRepository`) that concrete Infrastructure classes implement.
   - Hosts pluggable strategy implementations (e.g., `GoldScalperM1`, `EmaCross`).

3. **Nexus.Infrastructure (Adapters & Integrations):**
   - Implements concrete gateways to real trading environments (e.g., MetaTrader 5 Bridge Adapter utilizing Named Pipes or gRPC).
   - Manages persistence using Entity Framework Core for transactional storage and high-speed Dapper/ADO.NET binary copy writers for high-throughput time-series streaming tick data.
   - Integrates logging, telemetry, and system monitoring (Serilog, Prometheus).

4. **Nexus.WpfUi (Presentation Layer):**
   - Desktop user interface constructed in WPF on .NET 8/9.
   - Leverages MVVM pattern via the modern `CommunityToolkit.Mvvm` source generator library (`[ObservableProperty]`, `[RelayCommand]`).
   - Communicates with the Application layer via Dependency Injection (Microsoft Extensions DI).

---

## 2. Low-Latency & Zero-Allocation Path Design

In high-frequency algorithmic systems, garbage collection (GC) pauses introduce catastrophic slippage. NTE prevents GC pressure via a strict **Zero-Allocation Tick Path** design:
- **`Tick` is a `readonly struct`**: Allocated entirely on the stack. No heap allocation occurs during high-rate market-data stream processing.
- **Pass-by-Reference**: Where appropriate, large structures are passed using `in` parameters to prevent copying overhead.
- **High-Performance Buffers**: Internal queues and tick streams utilize pre-allocated ring buffers or `System.Threading.Channels` to handle backpressure and multithreaded handoffs with zero allocations.
- **Avoid Boxing**: Logging systems and event propagation are explicitly typed to avoid casting structures to `object`.

---

## 3. High-Precision Mathematical Model

- **Financial Values**: All monetary transactions, account balances, spreads, and execution costs are handled using high-precision data types or dedicated `Money` value objects representing clean, scale-safe decimals.
- **Lot Sizing**: `LotSize` handles volume constraints and minimum increments configured for individual symbols.
- **Risk Evaluation**: Execution routes always pass through an `IRiskManager` pre-trade check before order dispatch to MT5.
