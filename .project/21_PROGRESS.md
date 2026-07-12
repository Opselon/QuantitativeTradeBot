# 21_PROGRESS – Nexus MT5 Integration Progress

## Stage 1 – C# Contracts & Bridge Commands (Completed on 2025-07-12)

We have successfully completed Stage 1 of the MT5 trade execution and position synchronization substrate:

### What was Done
1. **C# MT5 Bridge Contracts**: Added/extended clean contracts under `src/Nexus.Application/Mt5Bridge/Contracts/`:
   - `PlaceOrderRequest`, `PlaceOrderResponse`
   - `ClosePositionRequest`, `ClosePositionResponse`
   - `GetOpenPositionsRequest`, `GetOpenPositionsResponse`
   - `BridgePositionDto`, `BridgeOrderSide`, `BridgePositionSide`, `BridgeOrderExecutionStatus`, `BridgeError`
2. **Application-Level Interface**: Introduced `IMt5TradingService` inside the application core (`src/Nexus.Application/Mt5/`):
   - Decoupled from transport mechanism.
   - Uses clean, broker-agnostic Application DTOs: `PlaceOrderResult`, `ClosePositionResult`, `OpenPositionDto`.
3. **Simulated Implementation**: Implemented `SimulatedMt5TradingService` to map execution commands to the existing in-memory simulated trading state, ensuring flawless offline execution during testing.
4. **Real Implementation**: Implemented `RealMt5TradingService` which maps application-level commands to bridge TCP envelopes and sends them over the wire via `IMt5BridgeClient`.
5. **Routing & DI**: Configured `RoutingMt5TradingService` to select Simulated vs Real implementations based on `Mt5Mode`. Registered all dependencies inside `App.xaml.cs`.
6. **Unit Tests**: Added robust tests inside `Mt5BridgeTests.cs` validating:
   - Request/response serialization.
   - Property mapping correctness between bridge DTOs and application DTOs.
   - Dynamic routing switching.

---

## Stage 2 – MT5 MQL5 Bridge Handlers & Real Execution (Completed on 2025-07-13)

We have successfully completed Stage 2 of the MT5 Terminal integration, establishing robust end-to-end real trading capabilities:

### What was Done
1. **MQL5 EA Bridge Handlers**: Implemented complete and robust message handlers in `MQL5/Experts/Nexus/NexusBridge.mq5` for:
   - `PlaceOrder`: Parses parameters, normalizes volume to the symbol's step size, selects appropriate Ask/Bid prices via `SymbolInfoTick` (falling back to `SymbolInfoDouble`), detects supported filling modes (FOK vs IOC vs RETURN), normalizing SL/TP to standard symbol digits, and dispatches via native `OrderSend`.
   - `ClosePosition`: Selecting active position by ticket, resolving dynamic filling modes, executing matching close market orders, and mapping MT5 retcodes back to C#.
   - `GetOpenPositions`: Enumerating active open terminal positions, formatting details (including ISO 8601 UTC times) into clean JSON responses.
2. **Robustness Safeguards**: Added validation checks (e.g., verifying symbol is selectable/exists, ensuring volume is greater than 0, normalizing SL/TP prices, wrapping all transactions with structured error codes like `INVALID_PAYLOAD`, `POSITION_NOT_FOUND`, `TRADE_REJECTED`).
3. **C# Service Diagnostics**: Added minimal metadata console logging in `RealMt5TradingService.cs` around command submissions to provide seamless traceability without exposing sensitive connection credentials.

---

## Stage 2 – MT5 MQL5 Bridge E2E Checklist (Manual E2E Procedure)

The following procedure enables developers and QA engineers to validate end-to-end execution through the MetaTrader 5 terminal:

### 1. Pre-requisites
- **MT5 Terminal**: Install MetaTrader 5 on a Windows client workstation.
- **Broker Account**: Ensure you are logged into a demo or live broker account.
- **Expert Advisor Placement**: Copy `MQL5/Experts/Nexus/NexusBridge.mq5` to your MT5 terminal data directory (`MQL5/Experts/Nexus/`).
- **Configuration Settings**:
  - In MT5: Go to `Tools -> Options -> Expert Advisors`.
  - Check "Allow algorithmic trading" and "Allow DLL imports".
- **Bridge Config**: Make sure `InpBridgeHost` is `"127.0.0.1"` and `InpBridgePort` is `5000`.

### 2. Step-by-Step Validation Steps
1. Set `"Mt5Mode": "Real"`, `"Mt5BridgeHost": "127.0.0.1"`, and `"Mt5BridgePort": 5000` in the application configurations.
2. Start the C# Nexus Trading Engine application. This spins up the TCP listening socket.
3. Drag and attach `NexusBridge.mq5` to any chart (e.g., EURUSD) in the MT5 Terminal.
4. **Verify Handshake**:
   - Observe in the MT5 "Experts" log tab: `NexusBridge: Successfully connected to Nexus Bridge Server at 127.0.0.1:5000`
   - Observe in C# output: `[RealMt5BridgeConnectionService] Handshake with MT5 completed successfully.`
5. **Verify PlaceOrder**:
   - Issue a market buy command via C# `IMt5TradingService.PlaceMarketOrderAsync` (e.g. EURUSD, Buy, 0.10 lots).
   - Observe in C# console: `[RealMt5TradingService] Sending PlaceOrder command. Request ID: <guid>, Symbol: EURUSD, Side: Buy, Volume: 0.10`
   - Observe in MT5 log tab: `NexusBridge: Received command 'PlaceOrder' with Request ID: <guid>`
   - Observe in MT5 log tab: `NexusBridge: PlaceOrder - Succeeded. Ticket: <ticket>, Retcode: 10009`
   - Confirm a new Buy position has been opened in the MT5 terminal Trade panel with the given ticket.
6. **Verify GetOpenPositions**:
   - Call `IMt5TradingService.GetOpenPositionsAsync` from C#.
   - Observe in C# console: `[RealMt5TradingService] Sending GetOpenPositions command. Request ID: <guid>`
   - Observe in MT5 log tab: `NexusBridge: Received command 'GetOpenPositions' with Request ID: <guid>`
   - Confirm that C# returns a collection of `OpenPositionDto` reflecting the EURUSD position ticket, side ("Buy"), volume (0.10), open price, and current prices.
7. **Verify ClosePosition**:
   - Call `IMt5TradingService.ClosePositionAsync` with the EURUSD position ticket and symbol.
   - Observe in C# console: `[RealMt5TradingService] Sending ClosePosition command. Request ID: <guid>, Ticket: <ticket>, Symbol: EURUSD, Volume: 0.10`
   - Observe in MT5 log tab: `NexusBridge: Received command 'ClosePosition' with Request ID: <guid>`
   - Observe in MT5 log tab: `NexusBridge: ClosePosition - Succeeded. Ticket: <ticket>, Retcode: 10009`
   - Confirm that the position has been closed and removed from the MT5 terminal Trade panel.

---

## Next Steps: Stage 3 – WPF UI Integration

In Stage 3, the focus will shift to user-interface elements:
- Build a dedicated MT5 Trading & Terminal Control Dashboard.
- Support real-time visualization of active terminal positions.
- Provide manual order entry / position closing UI capabilities leveraging `IMt5TradingService`.
