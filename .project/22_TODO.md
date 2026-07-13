# 22_TODO – MT5 Bridge Backlog & Roadmap

## [x] Stage 2: MQL5 Expert Advisor Handlers (Completed)
- **[x] Handler: `PlaceOrder`**
  - Extract and validate parameters.
  - Added robust dynamic volume step-rounding and price digits normalizations.
  - Added dynamic supported filling mode detection (`FOK`, `IOC`, `RETURN`) on symbol.
- **[x] Handler: `ClosePosition`**
  - Select active position, resolve filling modes, close deal and return response.
- **[x] Handler: `GetOpenPositions`**
  - Construct a valid JSON array of `BridgePositionDto` items and serialize into `GetOpenPositionsResponse`.
- **[x] Verification**
  - Locally verified end-to-end communication.

---

## [x] Stage 3: WPF Desktop UI Trading Panels (Completed)
- **[x] Position Table Panel**
  - Format ticket, symbol, volume, buy/sell badge, and real-time floating profit with clean, production-grade styling.
- **[x] Trading Ticket Entry**
  - Bind inputs and wire up `PlaceOrderUICommand`.
- **[x] Close Action Button**
  - Add contextual "Close" action in the position row.

---

## [x] Stage B: Real MT5 Localhost Bridge Integration Layer (Completed)
- **[x] Connect & Login Handshake**
  - Implement full TCP connection establishment, secure authorization credential submissions, and heartbeats.
- **[x] In-Terminal Tick Streaming**
  - Overwrite `NexusBridge.mq5` to support symbol subscriptions and stream high-precision bid/ask ticks under `OnTimer()`.
- **[x] Ingestion MarketDataPipeline**
  - Implement real tick normalization, consistency validation, and direct `INativeCoreService.UpdateTick` ingestion into `Nexus.Native.Core`.
- **[x] Bounded Diagnostics Logs**
  - Implement bounded logging storage to eliminate memory leaks under fast live traffic.
- **[x] Left Sidebar Workstation Navigation**
  - Design 9 modern, fully active panels (Dashboard, MT5 Bridge, Market Watch, Manual Desk, Account Metrics, Native Engine, Diagnostics, Settings, Test Console) switcher.
- **[x] Automated Smoke Test Verifier**
  - Provide a one-click automated verifier progress log running full end-to-end checks.

---

## [ ] Stage 4: Real-Time Push Streaming & Technical Indicator Optimizations (Backlog)
- **[ ] Real-time Streaming Push**: Replace polling loops with TCP push broadcasts from MT5 terminal on trade/position events (`OnTradeTransaction` native handlers).
- **[ ] Event-Based Position Updates**: Update client collection using real-time pushed streams.
- **[ ] Latency Monitoring**: Capture, measure, and display granular socket round-trip propagation times for every transaction.
- **[ ] C++ Core Connector / DLL Optimizations**: Optimize rolling technical indicator computation times via native C++ analytics engine bypasses.
