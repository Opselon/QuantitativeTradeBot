# Project Next Steps

## Upcoming Milestones

With the completion of **Phase 2.5: Hardened Execution Platform Foundation** and **Phase 2.9: Production-Grade Observability & Recovery Hardening**, the platform has been completely secured and hardened. The system is fully prepared to run multiple strategies concurrently, execute risk-managed orders, handle background worker loops, recover gracefully from host failures, fall back safely to managed engines, and output production-grade telemetry.

The immediate next milestones are:

### 1. MT5 Bridge Stage 3: WPF Operator UI Panels
- **Position Table Panel**: Design a clean WPF datagrid bound to `MainViewModel.OpenPositions` representing tickets, symbols, volumes, sides, and running profits with production-grade styles.
- **Trading Ticket Entry**: Construct classic Buy/Sell execution ticket components bound to `TradeSymbol`, `TradeVolume`, `SelectedSide`, SL/TP, and wire up `PlaceOrderUICommand`.
- **Close Action Button**: Add row-level context actions that invoke `ClosePositionAsync` with a safety confirmation prompt.
- Plan UX for showing active execution status, validation warnings, errors, and live logs.

### 2. MT5 Bridge Stage 4: Risk Integration & Advanced Order Types
- Integrate pre-trade risk checks with live MT5 execution.
- Add support for Limit, Stop, and Trailing orders.
- Handle partial closes and position netting/hedging differences seamlessly.
- Implement streaming real-time position and margin updates without full poll round-trips.
