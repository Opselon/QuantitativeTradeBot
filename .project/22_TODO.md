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

---

## [ ] Phase C: Multi-Strategy Portfolio Optimization & AI/ONNX Live Execution (Active Roadmap)

### Phase C.1: Quantitative Performance & Streaming Optimizations
- **[ ] Real-time Streaming Push**: Optimize MT5 Terminal with `OnTradeTransaction` native handlers for real-time pushed event-based updates on trades and positions, eliminating polling.
- **[ ] High-Speed Ingestion Pipeline**: Optimize the `MarketDataPipeline` push paths to C++ native core libraries, achieving sub-microsecond rolling indicator updates.
- **[ ] Latency Monitoring**: Measure and display granular TCP socket round-trip and native DLL execution times inside the Workstation view.

### Phase C.2: Strategy Supervisor & Execution Orchestration
- **[ ] Dynamic Strategy Sandbox**: Implement thread-safe multi-strategy containers within `StrategySupervisor` for hot-reloading custom strategy scripts.
- **[ ] Signal Router Concurrency**: Optimize the `SignalRouter` and `ExecutionCoordinator` to route high-frequency signals with lock-free concurrent queues.
- **[ ] Risk Control Bounds**: Implement post-handshake, pre-execution risk limits verification on real trade lots allocation.

### Phase C.3: AI Neural Evaluator Integration
- **[ ] ONNX Model Evaluator**: Streamline the `Nexus.AI` module utilizing ONNX Runtime to evaluate rolling technical matrices using neural model checkpoints.
- **[ ] Deterministic Fallback Math**: Align ONNX prediction boundaries with mathematically exact managed fallbacks under restricted execution profiles.
