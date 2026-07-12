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

## [x] Stage 3: WPF Desktop UI Trading Panels (Completed)
- **[x] Position Table Panel**
  - Design visual datagrid in WPF MainWindow mapped to `MainViewModel.OpenPositions`.
  - Format ticket, symbol, volume, buy/sell badge, and real-time floating profit with clean, production-grade styling.
- **[x] Trading Ticket Entry**
  - Construct a classic Buy/Sell execution ticket component.
  - Bind inputs to `TradeSymbol`, `TradeVolume`, `SelectedSide`, SL/TP, and wire up `PlaceOrderUICommand`.
- **[x] Close Action Button**
  - Add contextual "Close" action in the position row binding to `ClosePositionUICommand` with safety confirmation popup.

---

## [ ] Stage 4: Real-Time Push Streaming & Technical Indicator Optimizations (Backlog)
- **[ ] Real-time Streaming Push**: Replace polling loops with TCP push broadcasts from MT5 terminal on trade/position events (`OnTradeTransaction` native handlers).
- **[ ] Event-Based Position Updates**: Update client collection using real-time pushed streams.
- **[ ] Latency Monitoring**: Capture, measure, and display granular socket round-trip propagation times for every transaction.
- **[ ] C++ Core Connector / DLL Optimizations**: Optimize rolling technical indicator computation times via native C++ analytics engine bypasses.
