# 25_DECISIONS – Architectural Decision Records (ADR)

## ADR-001: Separation of Bridge Contracts and Application-Level Port (IMt5TradingService)

### Status
Approved (Stage 1)

### Context
MetaTrader 5 bridge uses high-performance, raw, and low-level JSON serialization structures over raw TCP sockets. Exposing these low-level JSON-friendly bridge DTOs (e.g. `PlaceOrderRequest`, `BridgePositionDto`) directly to strategies or UI layers introduces tight coupling to a single broker's bridging transport format. Furthermore, managing the active `IMt5Session` in the strategy or UI layer adds unnecessary state management overhead to components that only care about initiating trades.

### Decision
1. **Introduce clean interface port `IMt5TradingService`** in the application core layer (`src/Nexus.Application/Mt5/`).
2. **Abstract execution methods** so they do not accept session parameters or leak raw transport/serializability decorators.
3. **Establish clean, app-facing DTOs** (`PlaceOrderResult`, `ClosePositionResult`, `OpenPositionDto`) that carry decimal precision fields and broker-agnostic enums or string representations.
4. **Enforce mapping logic inside the Infrastructure layer** inside `RealMt5TradingService` and `SimulatedMt5TradingService`.
5. **Decouple session lifecycle**: Real/Simulated trading services can operate without passing active session parameters explicitly since the `IMt5BridgeClient` and underlying TCP socket maintain stable socket connections independently.

---

## ADR-002: Optimizing Position Closure with Explicit Symbol Parameter

### Status
Approved (Stage 1)

### Context
Under the standard MetaTrader 5 bridge protocol, position closure requires both a target position ticket and its matching instrument symbol. However, initial drafts of the application trading interface (`IMt5TradingService.ClosePositionAsync`) only supplied the position ticket. Resolving the symbol on the infrastructure side would require either caching positions or making sequential nested network queries to `GetOpenPositions` before issuing the closure—introducing unacceptable latency and complexity on high-performance execution paths.

### Decision
1. Add an explicit `symbol` parameter to `IMt5TradingService.ClosePositionAsync()`.
2. Map this parameter directly into the `ClosePositionRequest` bridge payload, allowing for immediate serialization and dispatch.
3. Completely eliminate the need for nested, high-latency network queries in the execution gateway adapter.

### Consequences
- **Pros**:
  - Direct, single-pass serialization of position close requests.
  - Sub-millisecond latency on close executions (critical during risk-driven liquidations).
- **Cons**:
  - The application caller must supply the symbol, which is readily available inside position rows or event contexts.

---

## ADR-003: MQL5 Trade Request Mapping, Robustness Normalization, and Ticket Tracking

### Status
Approved (Stage 2)

### Context
Executing market deals and closing positions inside MetaTrader 5 (MQL5) requires strict compatibility with broker-specific rules. For example, broker servers frequently reject orders with lot sizes that do not match the exact `SYMBOL_VOLUME_STEP`, or if the execution filling type is hardcoded to a mode (e.g., `ORDER_FILLING_FOK`) that the account type or symbol doesn't support. Similarly, price precision can be rejected if SL/TP points are not normalized to the symbol's exact `SYMBOL_DIGITS`.

### Decision
1. **Dynamic Volume Normalization**: Calculate valid volumes rounded to the symbol's `SYMBOL_VOLUME_STEP` and clamp the lot size strictly between `SYMBOL_VOLUME_MIN` and `SYMBOL_VOLUME_MAX`.
2. **Dynamic Filling Mode Selector**: Query symbol properties via bitmask `SymbolInfoInteger(symbol, SYMBOL_FILLING_MODE)` and dynamically cascade filling mode as `ORDER_FILLING_FOK` -> `ORDER_FILLING_IOC` -> `ORDER_FILLING_RETURN`.
3. **Precision Price Extraction**: Fetch exact current Ask/Bid prices using native high-precision `SymbolInfoTick` (falling back to `SymbolInfoDouble` if a tick is not yet cached). Normalize stop-loss and take-profit prices using the symbol's `SYMBOL_DIGITS` precision scale.
4. **Ticket Tracking**: Propagate the order ticket returned on native `OrderSend()` back to the C# brain as the trade ticket identifier for clean correlation.

### Consequences
- **Pros**:
  - Eliminates trade rejections caused by common broker integration mismatches (retcodes 10013, 10030, etc.).
  - Works out-of-the-box across a wide variety of MT5 brokers, symbols (forex, indices, crypto), and account types (hedging/netting).
  - Maximizes execution speed with pre-normalized, zero-overhead client-side parameters.
- **Cons**:
  - Volume rounding or price clamping could slightly modify the user's intent, though they remain within allowable broker limits.

---

### Consequences of ADR-001, ADR-002, & ADR-003
- **Pros**:
  - Code inside strategies or WPF viewmodels is incredibly clean and doesn't know anything about raw bridge envelopes or stringified payloads.
  - Easier to swap out MetaTrader 5 in the future for FIX Protocol, Interactive Brokers, or Binance with zero modifications to strategies or the UI.
  - Simplifies testing since the trading core can be thoroughly verified using standard mocks or in-memory simulated classes without networking side-effects.
  - Extremely robust trade dispatch pipeline with automated client-side adjustments.
- **Cons**:
  - Requires writing mapping code (e.g., `BridgePositionDto` to `OpenPositionDto`) which introduces minor boilerplate, fully compensated by separation of concerns.
