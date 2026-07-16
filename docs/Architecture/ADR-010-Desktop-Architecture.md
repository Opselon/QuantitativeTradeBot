# ADR-010: Institutional Trading Workstation Architecture

## Status
Approved

## Context
Phase 10 requires the creation of an advanced WPF Desktop Workstation that provides high-performance presentation, diagnostics, monitoring, and control capabilities for the underlying autonomous trading system. The UI must remain strictly a presentation surface without housing business, trading, data persistence, or MT5 communication logic. It must also introduce production-grade diagnostics, timeline explainability, reasoning replays, and strict live permission guards.

## Decision
We establish a Clean MVVM & Decoupled Dashboard Services Architecture to structure the presentation and control layer.

### 1. Architectural Layers & Flow
```
Views (XAML)
   ↓ (Bindings, Commands, Behaviors)
ViewModels (C# Presentation State)
   ↓ (Clean Contracts)
Dashboard Application Services (IMarketDashboardService, IDecisionDashboardService, IExecutionDashboardService, ITrainingDashboardService, ISystemHealthMonitorService)
   ↓ (Underlying Engines)
Core Trading Engines & Domain (Nexus.Core, Nexus.Application, Nexus.Execution, Nexus.Training)
```

- **Views**: Written strictly in XAML and standard code-behind to instantiate events. It renders responsive 4K-optimized panels with high-fidelity vector graphics and DynamicResources for visual themes.
- **ViewModels**: Translates application and diagnostic data into bindable properties, notifying the UI thread safely.
- **Dashboard Application Services**: Decoupled, dedicated services that wrap core domain logic, managing observable streams, historical event lists, state transitions, and system health status maps.

### 2. Explainability & Replay Architecture
To provide production-grade transparency, we implement:
- **Explainability Timeline**: Tracks sequential system actions (WAIT, BUY, MOVE_STOP, PARTIAL_CLOSE, CLOSE). Each transition records the timestamp, confidence probability, risk levels, triggering models, and reasoning.
- **Deterministic Decision Replay**: A master-detail historical analysis system. Selecting any historic decision reconstructs the complete reasoning process (snapshot, features, hypotheses, scenario search utilities, model weights, and execution outcomes) in a read-only, deterministic manner.

### 3. System Health Monitor Architecture
A centralized health tracker (`ISystemHealthMonitorService`) queries and aggregates status values across major subsystems (Native C++, Decision Engine, Market Intelligence, Training Engine, Execution Engine, Database, MT5 Bridge). It computes resource utilization (CPU, memory, thread pool) and high-precision latencies (tick processing, decision reasoning, order execution), presenting them via color-coded diagnostic indicator lamps.

### 4. Security Policies & Live Mode Guardrails
To prevent accidental live order execution under production workloads:
1. **Separate Profiles**: Simulation, Paper, and Live modes are kept isolated.
2. **Explicit User Confirmations**: Activating Live Permission requires interacting with a high-contrast switch that triggers a modal confirmation dialog window. This dialog must be explicitly accepted by the operator.
3. **Auto-Revocation**: Switching the active profile away from Live automatically revokes live execution permission.
4. **Traceable Audit Log**: All profile changes, permission toggles, and user confirmations write persistent entries to a monospace Security Audit Trail.

## Consequences
- Maintains a clean, framework-independent trading backend.
- Simplifies headless unit testing by allowing ViewModels to be verified completely offline.
- Guarantees airtight risk and execution safety under live trading environments.
