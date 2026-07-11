# Nexus Trading Engine - Progress Tracker

## Phase Checklist

### Phase 1: Initialization, Core Domain, & Handoff Documentation
- [x] Create `.nexus_docs/` directory and populate documentation files:
  - [x] `01_ARCHITECTURE.md`
  - [x] `02_DATABASE_SCHEMA.md`
  - [x] `03_PROGRESS.md`
  - [x] `04_NEXT_STEPS.md`
- [x] Initialize C# Solution (`NexusTradingEngine.sln`) and projects:
  - [x] `src/Nexus.Core/` (Class Library)
  - [x] `src/Nexus.Application/` (Class Library)
  - [x] `src/Nexus.Infrastructure/` (Class Library)
  - [x] `src/Nexus.WpfUi/` (WPF App)
  - [x] `tests/Nexus.Tests.Unit/` (xUnit Test)
  - [x] `tests/Nexus.Tests.Integration/` (xUnit Test)
- [x] Implement pure Domain Layer in `Nexus.Core`:
  - [x] `Symbol` Value Object
  - [x] `Tick` Readonly Struct
  - [x] `Bar` Candlestick Struct/Entity
  - [x] `Order` Domain Entity
  - [x] `Position` Domain Entity
  - [x] `Account` Domain Entity
  - [x] Define core interfaces: `IStrategy`, `IRiskManager`, `ITrailingManager`
- [x] Verify compilation and execution of Domain tests:
  - [x] Create `Nexus.Tests.Unit` target and write test cases for core models (`Tick`, `Bar`, etc.)
  - [x] Compile and verify all unit tests pass

### Phase 2: Database Layer (PostgreSQL & Persistence)
- [ ] Write DB scripts and EF Core configurations
- [ ] Implement Dapper/Binary COPY writer for market tick ingestion

### Phase 3: Inter-Process Communication (MT5 <-> C# Bridge)
- [ ] Define Protobuf and gRPC / Named Pipe client connections
- [ ] Integrate MT5 bridge in Infrastructure

### Phase 4: Strategy Engine & Dynamic Trailing Stop
- [ ] Implement `GoldScalperM1` Strategy
- [ ] Implement `EmaCrossover` Strategy
- [ ] Implement trailing stop calculations with entry break-even logic

### Phase 5: Modern WPF MVVM User Interface
- [ ] Configure WPF UI project with Dependency Injection
- [ ] Create Dashboard, Positions Grid, Log, and Chart views using CommunityToolkit.Mvvm

### Phase 6: Rigorous Testing Integration
- [ ] Verify end-to-end strategy, trailing stop, and risk triggers in `Nexus.Tests.Unit`
- [ ] Set up Testcontainers for integration tests in `Nexus.Tests.Integration`
