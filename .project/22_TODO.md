# 22_TODO – MT5 Bridge Backlog & Roadmap

## [ ] Stage 2: MQL5 Expert Advisor Handlers
- **[ ] Handler: `PlaceOrder`**
  - Extract and validate `Symbol`, `Side`, `Volume`, `StopLoss`, `TakeProfit`, and `Comment` properties from JSON payload.
  - Formulate and dispatch an `MqlTradeRequest` to `OrderSend()`.
  - Format result into a valid `PlaceOrderResponse` envelope with matching `requestId`.
- **[ ] Handler: `ClosePosition`**
  - Extract `Ticket` and `Volume` (if partial close).
  - Select active position using `PositionSelectByTicket()`.
  - Execute close deal (full or partial) using MT5 APIs.
  - Return `ClosePositionResponse` over TCP stream.
- **[ ] Handler: `GetOpenPositions`**
  - Enumerate active terminal positions using `PositionsTotal()`.
  - Loop over positions to extract ticket, symbol, volume, side, entry price, SL/TP, and running profit.
  - Construct a valid JSON array of `BridgePositionDto` items and serialize into `GetOpenPositionsResponse`.
- **[ ] Verification**
  - Locally test end-to-end communication between C# client in Real mode and MT5 terminal running `NexusBridge.mq5`.

---

## [ ] Stage 3: WPF Desktop UI Trading Panels
- **[ ] Position Table Panel**
  - Design visual datagrid in WPF MainWindow mapped to `MainViewModel.OpenPositions`.
  - Format ticket, symbol, volume, buy/sell badge, and real-time floating profit with clean, production-grade styling.
- **[ ] Trading Ticket Entry**
  - Construct a classic Buy/Sell execution ticket component.
  - Bind inputs to `TradeSymbol`, `TradeVolume`, `SelectedSide`, SL/TP, and wire up `PlaceOrderUICommand`.
- **[ ] Close Action Button**
  - Add contextual "Close" action in the position row binding to `ClosePositionUICommand` with safety confirmation popup.
