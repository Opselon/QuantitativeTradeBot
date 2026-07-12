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

### Consequences of ADR-001 & ADR-002
- **Pros**:
  - Code inside strategies or WPF viewmodels is incredibly clean and doesn't know anything about raw bridge envelopes or stringified payloads.
  - Easier to swap out MetaTrader 5 in the future for FIX Protocol, Interactive Brokers, or Binance with zero modifications to strategies or the UI.
  - Simplifies testing since the trading core can be thoroughly verified using standard mocks or in-memory simulated classes without networking side-effects.
- **Cons**:
  - Requires writing mapping code (e.g., `BridgePositionDto` to `OpenPositionDto`) which introduces minor boilerplate, fully compensated by separation of concerns.
