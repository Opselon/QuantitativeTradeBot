# 01. Nexus Trading Engine Architecture

The Nexus Trading Engine (NTE) is an enterprise-grade AI quantitative algorithmic trading platform structured using Hexagonal (Clean/Ports & Adapters) architecture.

## Platform Layers

```text
       ┌────────────────────────────────────────────────────────┐
       │                        WPF UI                          │
       │                   (Nexus.Desktop)                      │
       └──────────────────────────┬─────────────────────────────┘
                                  ▼
       ┌────────────────────────────────────────────────────────┐
       │                  Application Services                  │
       │                  (Nexus.Application)                   │
       └──────────────────────────┬─────────────────────────────┘
             ┌────────────────────┼────────────────────┐
             ▼                    ▼                    ▼
   ┌───────────────────┐┌───────────────────┐┌───────────────────┐
   │    Nexus.Core     ││     Nexus.AI      ││Nexus.Infrastructure│
   │  (Domain Logic)   ││ (Neural Inference)││    (Adapters)     │
   └───────────────────┘└───────────────────┘└─────────┬─────────┘
                                                       │
                                                       ▼
                                             ┌───────────────────┐
                                             │ Nexus.Native.Core │
                                             │ (C++20 High-Perf) │
                                             └───────────────────┘
```

### 1. Presentation Layer (WPF UI)
The desktop workstations (`Nexus.Desktop` and `Nexus.WpfUi`) provide real-time status visualizations, manual execution panels, configuration interfaces, and detailed metrics. Business logic or AI feature calculations must never leak into views.

### 2. Application Layer (Nexus.Application)
Orchestrates trading flows, coordinates background processing pipelines, manages the strategy registry/hosts, and evaluates pre-trade risk controls.

### 3. Domain Layer (Nexus.Core)
Represents the pure, dependency-free logical core containing core trading entities (`Order`, `Position`, `Tick`, `Bar`, `Account`, `MarketVector`), core business interfaces, and transactional domain events.

### 4. Neural Layer (Nexus.AI)
Encapsulates high-performance runtime inference components powered by ONNX Runtime (`Microsoft.ML.OnnxRuntime`), transforming input features into trade predictions without depending on Python.

### 5. Infrastructure Layer (Nexus.Infrastructure)
Implements all external adapters (PostgreSQL/SQLite EF Core configurations, MT5 socket client, DPAPI Secret Store, background ingestion workers, and native P/Invoke analytical binding resolvers).

### 6. Native Core Layer (Nexus.Native.Core)
Written in ultra-fast, zero-allocation C++20. Implements AVX2-aligned Stockfish-inspired incremental accumulator and market state engine. Connected to C# via a safe C-ABI boundary using fast, source-generated `LibraryImport`.
