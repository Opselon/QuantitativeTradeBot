# Project Next Steps

## Upcoming Milestones

With the completion of **Phase 2.5: Hardened Execution Platform Foundation** and **Phase 2.9: Production-Grade Observability & Recovery Hardening**, the platform has been completely secured and hardened. The system is fully prepared to run multiple strategies concurrently, execute risk-managed orders, handle background worker loops, recover gracefully from host failures, fall back safely to managed engines, and output production-grade telemetry.

The immediate next milestones are:

### 1. MT5 Bridge Stage 2: MQL5 Expert Advisor Handlers
- **Handler: `PlaceOrder`**: Extract and validate `Symbol`, `Side`, `Volume`, `StopLoss`, `TakeProfit`, and `Comment` properties from JSON payload. Construct and dispatch an `MqlTradeRequest` to native `OrderSend()`. Return `PlaceOrderResponse` envelope with matching `requestId` over TCP socket.
- **Handler: `ClosePosition`**: Extract `Ticket` and `Volume`. Select active position using `PositionSelectByTicket()`, execute close deal using MT5 APIs, and return `ClosePositionResponse`.
- **Handler: `GetOpenPositions`**: Enumerate active terminal positions using `PositionsTotal()`. construct a valid JSON array of `BridgePositionDto` items and serialize into `GetOpenPositionsResponse`.
- Ensure all handlers are wired into the existing Ping/GetAccountSnapshot infrastructure and respect the protocol defined in `.project/08_MT5_PROTOCOL.md`.

### 2. MT5 Bridge Stage 3: WPF Operator UI Panels
- **Position Table Panel**: Design a clean WPF datagrid bound to `MainViewModel.OpenPositions` representing tickets, symbols, volumes, sides, and running profits with production-grade styles.
- **Trading Ticket Entry**: Construct classic Buy/Sell execution ticket components bound to `TradeSymbol`, `TradeVolume`, `SelectedSide`, SL/TP, and wire up `PlaceOrderUICommand`.
- **Close Action Button**: Add row-level context actions that invoke `ClosePositionAsync` with a safety confirmation prompt.
- Plan UX for showing active execution status, validation warnings, errors, and live logs.
