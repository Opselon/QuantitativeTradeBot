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
       |  | - ExecutionCoordinator, Pipeline Context        |  |
       |  | - Ports (IExecutionGateway, IMarketDataFeed)    |  |
       |  | - Strategy runtime: Host, Registry, Supervisor  |  |
       |  | - Security Hardening & Input Validator          |  |
       |  +-------------------------------------------------+  |
       |                                                       |
       | - PostgreSQL (EF Core, Binary COPY)                   |
       | - Background Workers & Recovery Services             |
       | - Native C++ Interop (EMA indicator)                  |
       +-------------------------------------------------------+
                                  |
                                  v
                             Nexus.WpfUi
                       (Deferred to Phase 3)
```

### Core Architecture Layers:
1. **Nexus.Core (The Domain Model):**
   - Zero external library dependencies, framework-agnostic.
   - Contains high-performance value objects (`Symbol`, `Money`, `LotSize`) and core domain entities (`Tick`, `Bar`, `Order`, `Position`, `Account`).
   - Declares the core contracts and algorithmic interfaces (`IStrategy`, `IRiskManager`, `ITrailingManager`).

2. **Nexus.Application (Orchestration & Application Services):**
   - Implements execution logic, order coordination, and historical backtesting runners.
   - Defines output and input Ports (interfaces such as `IExecutionGateway`, `IDbContext`, `IMarketDataRepository`) that concrete Infrastructure classes implement.
   - Hosts pluggable strategy implementations.
   - Implements the complete **Strategy Hosting Architecture** (`IStrategyHost`, `StrategyHost`, `StrategySupervisor`, `StrategyRegistry`) and **Layered Execution Pipeline** (`ExecutionCoordinator`, `SignalRouter`, `PreTradeRiskEvaluator`).
   - Hardens inputs and configurations using `InputValidator` and `SecurityConfiguration`.

3. **Nexus.Infrastructure (Adapters & Integrations):**
   - Implements concrete gateways to real trading environments (e.g., MetaTrader 5 Bridge contracts).
   - Manages persistence using Entity Framework Core for transactional storage and high-speed ADO.NET binary copy writers for high-throughput time-series tick data.
   - Implements Hosted Background Services (`MarketDataIngestionWorker`, `StrategyDispatchWorker`, `ExecutionWorker`) and the `RecoveryStartupService` utilizing `System.Threading.Channels` for backpressure handling.

4. **Nexus.WpfUi (Presentation Layer):**
   - Desktop user interface constructed in WPF on .NET 10.
   - Deferred until the execution pipeline and MT5 backend substrate are completely hardened.

---

## 2. Low-Latency & Zero-Allocation Path Design

In high-frequency algorithmic systems, garbage collection (GC) pauses introduce catastrophic slippage. NTE prevents GC pressure via a strict **Zero-Allocation Tick Path** design:
- **`Tick` is a `readonly struct`**: Allocated entirely on the stack. No heap allocation occurs during high-rate market-data stream processing.
- **High-Performance Buffers**: Internal queues and tick streams utilize pre-allocated ring buffers or `System.Threading.Channels` to handle backpressure and multithreaded handoffs with zero allocations.
- **Avoid Boxing**: Logging systems and event propagation are explicitly typed to avoid casting structures to `object`.

---

## 3. High-Precision Mathematical Model & Native Acceleration

- **Financial Values**: All monetary transactions, account balances, spreads, and execution costs are handled using high-precision data types or dedicated `Money` value objects representing clean, scale-safe decimals.
- **Native Quantitative Engine**: Move intensive quant calculations (e.g. EMA rolling indicator) into native C++ (`native/Nexus.Native`) to bypass JIT compilation overhead and maximize vectorization. Bridge using high-performance P/Invoke.

### 4. Application-Level MT5 Trading Service (IMt5TradingService)

A core addition to the Application layer is the introduction of `IMt5TradingService`. This interface serves as the application-facing port for all MT5 trading operations, separating domain logic from underlying bridge/transport details:
- **Port Interface**: `IMt5TradingService` in the Application layer abstracts execution actions (`PlaceMarketOrderAsync`, `ClosePositionAsync`, `GetOpenPositionsAsync`) using broker-agnostic, clean Application-level DTOs (`PlaceOrderResult`, `ClosePositionResult`, `OpenPositionDto`).
- **Simulated Adapter**: `SimulatedMt5TradingService` executes trades in-memory, delegating to the existing `SimulatedMt5TradeService` to ensure perfect state parity across components during evaluation or paper trading.
- **Real Bridge Adapter**: `RealMt5TradingService` communicates via JSON over TCP using `IMt5BridgeClient` to interact with the MQL5 EA on the MT5 terminal side.
- **Dynamic Routing**: `RoutingMt5TradingService` acts as a dynamic delegation router that selects either the real or simulated implementation at runtime depending on the configuration parameter `Mt5Mode` ("Simulated" vs "Real" / "RealBridge").
- **Close Position Optimization**: Following architectural feedback, `ClosePositionAsync` includes an explicit `symbol` parameter. This prevents inefficient nested network round-trips to list active positions to resolve symbols, resulting in ultra-low-latency and robust risk-execution.
