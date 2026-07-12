# 00_MASTER_PLAN – Nexus Trading Engine Roadmap

## 1. Executive Summary

The Nexus Trading Engine (NTE) is a high-performance, low-latency algorithmic trading platform built on Decoupled Hexagonal/Clean Architecture, DDD, and async-first patterns on .NET 10. The master plan focuses on delivering institutional-grade reliability, ultra-low slippage, rigorous pre-trade risk management, and flawless multi-strategy execution.

---

## 2. Milestone Phases

### Phase 1: Core Domain Foundations (Completed)
- Standardize stack-allocated, zero-allocation `Tick` structures.
- Value objects for price-scale safety: `Symbol`, `Money`, `LotSize`.
- Pluggable abstract boundaries (`IStrategy`, `IRiskManager`).

### Phase 2: Dual-Mode Persistence & Ingestion Substrate (Completed)
- In-memory SQLite for local quickstart & fast-running tests.
- Monthly-partitioned PostgreSQL utilizing ADO.NET binary copy streaming for ultra-high throughput.
- Ephemeral test integration via Testcontainers.

### Phase 3: MetaTrader 5 (MT5) Bridge & Integration (In Progress)
- **Stage 1: C# Contracts & App Services (Completed)**
  - Establish IMt5TradingService application port and clean DTOs.
  - Implement Simulated, Real (JSON-over-TCP via IMt5BridgeClient), and Routing service decorators.
  - Eliminate sequential network round-trips by passing the symbol directly into ClosePositionAsync.
  - Comprehensive unit testing of serialization, mapping, and routing.
- **Stage 2: MQL5 Bridge Handlers (Next Step)**
  - Implement JSON payload parsing inside MQL5 Expert Advisor `NexusBridge.mq5`.
  - Wire up native terminal trading commands (`OrderSend`, `PositionsTotal`) for market deals and position synchronization.
  - Maintain requestId correlation back over TCP socket loop.
- **Stage 3: Operator UI Panel**
  - Design real-time position dashboard, execution logs, and manual trade ticket in WPF.

### Phase 4: Observability & Recovery Hardening (Completed)
- Contextual structured logging featuring stable EventIds.
- Correlating workflow actions with `CorrelationId` and `OperationId` spans.
- Sensitive data filtering via `LogSanitizer`.

---

## 3. Scope of Stage 1 Execution

Stage 1 is fully completed on the C# .NET side. All contracts, ports, routing, and adapter mapping patterns have been successfully designed, implemented, and verified with 100% test coverage. No MQL5 or UI changes were made in this stage, keeping boundaries clean and allowing for isolated testing before proceeding to Stage 2.
