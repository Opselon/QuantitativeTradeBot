# 13_EXECUTION_ENGINE – MT5 Trade Execution Pipeline

## 1. Overview

The Nexus Trading Engine (NTE) trade execution flows through a clean, broker-agnostic, application-level port (`IMt5TradingService`). This isolates higher-level strategy execution and workspace UI components from the underlying transport protocol.

```text
[UI / Algorithmic Strategy]
             |
             v
     IMt5TradingService (Application Port)
             |
             +-----------------------+-----------------------+
             |                                               |
             v                                               v
SimulatedMt5TradingService                      RealMt5TradingService (Infrastructure Adapter)
 (In-Memory positions & mocks)                               |
                                                             |-- Create JSON Envelope
                                                             |-- Send JSON-over-TCP
                                                             v
                                                   IMt5BridgeClient
                                                             |
                                                             v
                                                    MQL5 NexusBridge EA
                                                      (Stage 2 Handlers)
```

---

## 2. Execution Flow Sequence

For the `RealMt5TradingService` implementation, trade execution and position synchronization follow a strict async-first sequence:

1. **Invocation**: A component (such as the WPF trading desk or an automated strategy) calls a method on `IMt5TradingService` (e.g., `PlaceMarketOrderAsync`).
2. **Validation**: The service performs local validation on parameters (volume > 0, non-empty symbol) to fail fast.
3. **Bridge Request Generation**: A correlation-tracked `requestId` is generated. A `PlaceOrderRequest` is wrapped inside a `BridgeMessageEnvelope` with a request marker and command name.
4. **Transport**: The message is serialized to JSON and sent over TCP via `IMt5BridgeClient.SendAsync()`.
5. **Awaiting Correlated Response**: The calling task is kept suspended while mapping the request-ID to a `TaskCompletionSource`.
6. **MQL5 Execution**: The `NexusBridge.mq5` EA receives the request, parses the JSON, calls `OrderSend()` (or queries active positions), and returns a serialized JSON response back over the open TCP channel.
7. **Resolution**: The TCP receiver thread parses the response envelope, resolves the matching `TaskCompletionSource`, maps the `PlaceOrderResponse` / `ClosePositionResponse` / `BridgePositionDto` back into the application DTOs (`PlaceOrderResult` / `ClosePositionResult` / `OpenPositionDto`), and returns.

---

## 3. Key Design Decisions

### 3.1. Strict Separation of Concerns (Port vs Adapter DTOs)
To maintain Clean Architecture boundaries, we enforce a strict separation between Application-level and Bridge-level Data Transfer Objects:
- **Application Core DTOs**: `PlaceOrderResult`, `ClosePositionResult`, and `OpenPositionDto` reside in `src/Nexus.Application/Mt5/`. These are broker-agnostic, immutable (get-only), scale-safe records containing pristine types (e.g. `decimal`, basic strings, standard Datetime structures).
- **Bridge Contracts**: `PlaceOrderRequest`, `PlaceOrderResponse`, `ClosePositionRequest`, `ClosePositionResponse`, and `BridgePositionDto` reside in `src/Nexus.Application/Mt5Bridge/Contracts/`. These carry system-specific serialization decorators (e.g. `System.Text.Json` properties) and are tailored to match the MQL5 JSON payload structure exactly.
- **Infrastructure Mapping**: Mappings between Bridge DTOs and Application DTOs are isolated entirely within the Adapters inside `src/Nexus.Infrastructure/Adapters/Mt5/`.

### 3.2. Testability & Flexibility via Dynamic Mode Routing
The `RoutingMt5TradingService` acts as an active interceptor that queries configuration parameters. When `Mt5Mode` is configured as `"Simulated"`, calls are instantly routed to `SimulatedMt5TradingService` (which operates on stack-allocated and in-memory mock position maps). This enables seamless local, offline strategy development, while setting `Mt5Mode` to `"Real"` or `"RealBridge"` binds live execution directly to TCP transport adapters—making system components fully decoupled and highly testable.

---

## 4. Status and Roadmap

- **Stage 1 (Done)**: C# contracts, app-facing `IMt5TradingService`, real/simulated implementations, routing by configuration, and unit tests.
- **Stage 2 (Done)**: Implementation of native MQL5 `NexusBridge.mq5` handlers to execute `PlaceOrder`, `ClosePosition`, and `GetOpenPositions` on the MT5 terminal.
- **Stage 3 (Planned)**: UI workspace panels in WPF targeting these interfaces to allow real-time position monitoring and trading.
