# 22_TODO – MT5 Bridge Backlog & Roadmap

## [x] Stage 2: MQL5 Expert Advisor Handlers (Completed)
- **[x] Handler: `PlaceOrder`**
  - Extract and validate `Symbol`, `Side`, `Volume`, `StopLoss`, `TakeProfit`, and `Comment` properties from JSON payload.
  - Formulate and dispatch an `MqlTradeRequest` to `OrderSend()`.
  - Format result into a valid `PlaceOrderResponse` envelope with matching `requestId`.
  - Added robust dynamic volume step-rounding and price digits normalizations.
  - Added dynamic supported filling mode detection (`FOK`, `IOC`, `RETURN`) on symbol.
- **[x] Handler: `ClosePosition`**
  - Extract `Ticket` and `Volume` (if partial close).
  - Select active position using `PositionSelectByTicket()`.
  - Execute close deal (full or partial) using MT5 APIs.
  - Return `ClosePositionResponse` over TCP stream.
- **[x] Handler: `GetOpenPositions`**
  - Enumerate active terminal positions using `PositionsTotal()`.
  - Loop over positions to extract ticket, symbol, volume, side, entry price, SL/TP, and running profit.
  - Construct a valid JSON array of `BridgePositionDto` items and serialize into `GetOpenPositionsResponse`.
- **[x] Verification**
  - Locally verified end-to-end communication between C# client in Real mode and MT5 terminal running `NexusBridge.mq5` via manual testing procedures.

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

---

## [ ] Stage 4 & Future Backlog
- **[ ] Rich MT5 Test Harness Script**: Create a fully automated, standalone MQL5 testing script or harness in the repo to programmatically stress-test EA handlers under simulated or historical broker ticks.
- **[ ] Pre-Trade Risk Engine Integration**: Route real MT5 trade commands through local risk buffers and pre-trade limits before dispatching over bridge socket.
- **[ ] Real-time Streaming Push**: Replace polling loops with TCP push broadcasts from MT5 terminal on trade/position events (`OnTradeTransaction` native handlers).
