# 26_CHANGELOG – Nexus Trading Engine Change Log

## [Unreleased] - Stage 1 (C# Contracts & MT5 Bridge Commands)

### Added
- **Application Port**: Created `IMt5TradingService` interface defining `PlaceMarketOrderAsync`, `ClosePositionAsync`, and `GetOpenPositionsAsync` as a clean, decoupled application port.
- **Application DTOs**: Added broker-agnostic, clean data carriers `PlaceOrderResult`, `ClosePositionResult`, and `OpenPositionDto` under `src/Nexus.Application/Mt5/`.
- **Infrastructure Adapters**:
  - `RealMt5TradingService`: Maps application-level calls to raw JSON-over-TCP bridge payloads and returns structured result states.
  - `SimulatedMt5TradingService`: Provides flawless offline/paper-trading mock actions by delegating to the existing in-memory simulated state.
  - `RoutingMt5TradingService`: Routes commands dynamically to Real vs Simulated services depending on the configuration parameter `Mt5Mode`.
- **Dependency Injection**: Registered all new interfaces and service mappings in `src/Nexus.Desktop/App.xaml.cs`.
- **Unit Tests**: Added 5 new robust, comprehensive unit tests under `tests/Nexus.Tests.Unit/Desktop/Mt5BridgeTests.cs` checking request mapping, response serialization, routing selection, and exception propagation.

---

### Changed
- Refactored `Mt5BridgeTests.cs` imports to use the correct namespaces and include mapping and routing tests.
