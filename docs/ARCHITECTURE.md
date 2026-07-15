# Nexus Trading Engine - System Architecture

This document describes the high-level system architecture of the **Nexus Trading Engine (NTE)** platform. Built with enterprise-grade standards, the platform uses Clean Architecture, Hexagonal (Ports & Adapters) design patterns, and C++20 high-performance interoperability to establish a rock-solid trading intelligence platform.

---

## 1. Structural Overview

The core principles of the Nexus Trading Engine architecture are **strict layer separation**, **unidirectional dependency flow**, and the **isolation of business rules from external frameworks**. This ensures long-term testability, modularity, and high-performance execution.

```text
       ┌────────────────────────────────────────────────────────┐
       │                  Presentation Layer                    │
       │           (Nexus.Desktop / Nexus.WpfUi)                │
       └──────────────────────────┬─────────────────────────────┘
                                  │ (Uses DI Host, Swaps Themes)
                                  ▼
       ┌────────────────────────────────────────────────────────┐
       │                  Application Services                  │
       │                  (Nexus.Application)                   │
       └──────────────────────────┬─────────────────────────────┘
             ┌────────────────────┼────────────────────┐
             ▼                    ▼                    ▼
   ┌───────────────────┐┌───────────────────┐┌───────────────────┐
   │    Nexus.Core     ││     Nexus.AI      ││Nexus.Infrastructure│
   │  (Domain Core)    ││ (Neural Inference)││ (Adapters / Svc)  │
   └───────────────────┘└───────────────────┘└─────────┬─────────┘
                                                       │ (P/Invoke Bridge)
                                                       ▼
                                             ┌───────────────────┐
                                             │ Nexus.Native.Core │
                                             │ (C++20 High-Perf) │
                                             └───────────────────┘
```

---

## 2. Layer Definitions

The platform is strictly organized into six decoupled layers, each with clearly defined responsibilities.

### A. Core / Domain Layer (`Nexus.Core`)
* **Purpose**: Houses the pure business domain logic, representing the absolute foundation of the system.
* **Dependencies**: **Zero external dependencies**. It does not reference databases, presentation libraries, AI frameworks, or external infrastructure.
* **Key Components**:
  * **Value Objects**: Structurally immutable components featuring custom mathematical behavior (`Symbol`, `Money`, `LotSize`).
  * **Readonly Structs**: High-performance, zero-allocation data structures designed for processing high-frequency live streams (`Tick`).
  * **Domain Entities**: Core business models managing live mutable state (`Order`, `Position`, `Account`, `AccumulatorState`, `MarketVector`).
  * **Domain Events**: Events generated when critical domain actions occur (`OrderExecutedEvent`, `MarginCallEvent`).
  * **Abstractions**: Core interfaces for domain services (`INativeCoreService`, `IDecisionEngine`, `ICurrencyStrengthEngine`).

### B. Neural Layer (`Nexus.AI`)
* **Purpose**: Performs high-speed runtime neural inference calculations on structured market data.
* **Dependencies**: Depends strictly on `Nexus.Core` for domain definitions and utilizes the managed `Microsoft.ML.OnnxRuntime` package.
* **Key Components**:
  * **ONNX Neural Evaluator**: Orchestrates feature transformation and real-time execution of trained deep neural network models with zero Python dependencies.
  * **Managed Fallback Engine**: A robust fallback model designed to continue evaluation if ONNX model artifacts are missing.

### C. Application Layer (`Nexus.Application`)
* **Purpose**: Coordinates application workflows, manages system orchestrations, and implements ports (abstractions) to satisfy user-driven and automated transactions.
* **Dependencies**: Depends strictly on `Nexus.Core`. It contains **no UI logic** and **no database or infrastructure implementation reference**.
* **Key Components**:
  * **Ports (Interfaces)**: Decoupled abstractions for database repositories (`IAccountRepository`, `IOrderRepository`), external brokers (`IMt5TradingService`), and environment configurations.
  * **Strategy Hosts & Supervisors**: Runtime containers (`StrategyHost`, `StrategySupervisor`) designed to route incoming tick streams to sandboxed strategies.
  * **Execution Pipeline**: Decoupled order evaluation chains (`ExecutionCoordinator`, `SignalRouter`, `PreTradeRiskEvaluator`) executing strict pre-trade risk checks.
  * **Intelligence Engines**: Stockfish-inspired components like `ScenarioSearchEngine` executing Monte Carlo simulations to calculate expected values.

### D. Native Interop Bridge (`Nexus.Infrastructure.Native`)
* **Purpose**: Exposes a safe, high-speed C-compatible ABI bridging the high-performance C++ quantitative execution engine with managed .NET code.
* **Dependencies**: Depends on `Nexus.Core` (for interface fulfillment) and incorporates P/Invoke configurations.
* **Key Components**:
  * **Source-Generated Interops**: Uses .NET 10 source-generated `[LibraryImport]` to eliminate pinvoke marshalling overhead.
  * **Pointer Wrapper**: Implements `NativeCoreSafeHandle` (derived from `SafeHandleZeroOrMinusOneIsInvalid`) for robust, leak-proof lifecycle management of C++ memory.

### E. Infrastructure Layer (`Nexus.Infrastructure`)
* **Purpose**: Acts as the system adapter layer, implementing all concrete out-of-core services.
* **Dependencies**: Depends on `Nexus.Application` and `Nexus.Infrastructure.Native`.
* **Key Components**:
  * **Database Adapters**: Dual-mode persistence adapters implementing `DbContext` for PostgreSQL (enterprise) and SQLite (local-first evaluation).
  * **IPC & TCP Bridges**: Stateful socket clients (`TcpMt5BridgeClient`) and hosting listeners routing messages to and from MQL5 Expert Advisors (`NexusBridge.mq5`).
  * **Background Workers**: High-performance hosted services (`ExecutionWorker`, `MarketDataIngestionWorker`) utilizing thread-safe `System.Threading.Channels` for zero-allocation queuing.
  * **Secure Stores**: Machine-bound encryption and Windows DPAPI implementations (`WindowsSecretStore`) to secure API and broker credentials.

### F. Presentation Layer (`Nexus.Desktop` / `Nexus.WpfUi`)
* **Purpose**: Delivers a highly interactive, responsive multi-tab WPF workstation UI for operators.
* **Dependencies**: Depends on `Nexus.Application` and bootstrapped using `Microsoft.Extensions.Hosting` (MVVM pattern).
* **Key Components**:
  * **Workspace ViewModels**: Thread-safe viewmodels bound to workspace views.
  * **Theme switching**: Swap active `ResourceDictionary` instances dynamically at runtime (supporting Dark and Light modes) using high-fidelity vector styling (`Zero Bitmap Policy`).
  * **Manual Trading Guardrails**: Strict UI state triggers disabling manual trading ticket execution unless connection health returns `BridgeLifecycleState.Authenticated`.

---

## 3. Hexagonal (Ports & Adapters) Boundaries

The platform guarantees strict framework independence by relying on Hexagonal patterns:

```text
              [External World] (Postgres / MT5 / WPF UI)
                      │
                      ▼
             ┌─────────────────┐
             │     Adapter     │  (e.g., RealMt5BridgeAdapter, DbContext)
             └────────┬────────┘
                      │
                      ▼
             ┌─────────────────┐
             │      Port       │  (e.g., IMt5TradingService, IAccountRepository)
             └────────┬────────┘
                      │
                      ▼
             ┌─────────────────┐
             │  Domain Core    │  (e.g., Order, Account, ExecutionCoordinator)
             └─────────────────┘
```

* **Ports**: Declared inside `Nexus.Core` or `Nexus.Application` as C# interfaces. They define *what* the system needs.
* **Adapters**: Implemented in `Nexus.Infrastructure` or `Nexus.Desktop` as concrete implementations. They define *how* those needs are fulfilled using third-party packages or system components.

---

## 4. Key Architectural Patterns

1. **High-Performance Streaming Channels**: Communication between background tasks is orchestrated via high-speed, thread-safe, bounded, zero-allocation `System.Threading.Channels`.
2. **Unified Observability Core**: Structured logging featuring standard `LogEventIds`, workflow scopes, and absolute token/secret scrubbing rules ensures safe cloud and local diagnostics.
3. **Managed & Native Dualism**: Critical operations support automatic fallback paths; if high-performance C++ native DLLs are missing, the application seamlessly delegates execution to managed high-fidelity C# simulation layers without interrupting processes.
