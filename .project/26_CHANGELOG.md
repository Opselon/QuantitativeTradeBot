# 26_CHANGELOG – Nexus Trading Engine Change Log

## [Completed] - Stage 3 (WPF UI Integration & MT5 Operator Panel) - 2025-07-14

### Added
- **Desktop Models**:
  - `DesktopOrderSide` (enum: Buy/Sell).
  - `DesktopPositionDto` carrying mapped positional metrics (Ticket, Symbol, Side, Volume, StopLoss, TakeProfit, Profit, Swap, Commission, OpenTime, Status).
  - `DesktopTradeResult` carrying manual order feedback details.
- **Operator Facade Service**: Created `IMt5OperatorService` and `Mt5OperatorService` wrapping the core `IMt5TradingService` with elegant DTO mappings and exception translation (e.g. SocketExceptions mapped to "Connection lost", TimeoutException to "Bridge timeout").
- **Operator ViewModels**:
  - `DesktopPositionViewModel` exposing calculated metrics (like position `Duration`).
  - `Mt5TradingViewModel` managing states (Connected, Busy, Refreshing, Executing), validating inputs (volume within [0.01, 100]), executing operator logs via `IDiagnosticService`, and running a safe, cancellable background polling loop.
- **WPF UI Operator Dashboard Control**:
  - `Mt5TradingPanel.xaml` and its code-behind `Mt5TradingPanel.xaml.cs` containing Top Connection metrics panel, Middle Manual Trade Entry ticket, and Bottom Positions DataGrid with contextual actions.
- **Integration & DI Registry**: Registered new operator facade and viewmodel in `App.xaml.cs`, injected `Mt5TradingViewModel` as a property in `MainViewModel`, and embedded `Mt5TradingPanel` in the workstation view in `MainWindow.xaml`.
- **Deterministic Unit Tests**: Added a comprehensive suite of unit tests in `tests/Nexus.Tests.Unit/Desktop/Mt5TradingViewModelTests.cs` validating refresh, buy, sell, close, busy states, inputs validation, and logging, with target-OS framework compilation conditional support for headless Linux platforms.

---

## [Completed] - Stage 2 (MT5 MQL5 Bridge Handlers & Real Execution) - 2025-07-13

### Added
- **MQL5 EA Bridge Handlers**: Implemented command dispatchers inside `NexusBridge.mq5` for `PlaceOrder`, `ClosePosition`, and `GetOpenPositions` commands.
- **Robustness Normalizations**:
  - Added dynamic volume rounding to `SYMBOL_VOLUME_STEP` bounded by min/max allowable lot size.
  - Added dynamic filling mode detection (`FOK`, `IOC`, `RETURN`) based on Symbol properties.
  - Added Symbol existence validation via `SymbolSelect()`.
  - Added precision price extraction using native high-precision `SymbolInfoTick` with fallback to `SymbolInfoDouble`, normalizing stops/take-profits via symbol's `SYMBOL_DIGITS`.
- **C# Bridge Diagnostics**: Added metadata console logs inside `RealMt5TradingService.cs` when commands are dispatched over TCP.

---

## [Completed] - Stage 1 (C# Contracts & MT5 Bridge Commands) - 2025-07-12

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
