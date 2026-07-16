# Nexus Execution Engine Documentation

The **Nexus Execution Subsystem** (`Nexus.Execution`) provides a professional-grade, decoupled, risk-controlled, and multi-profile execution runtime for the trading engine. It acts as an airtight bridge connecting strategy intelligence outputs to live or simulated trading environments.

---

## 1. System Architecture Diagram

```text
       Decision (Strategy Signal)
                  │
                  ▼
         [OrderRequest Created]
                  │
                  ▼
       ┌──────────────────────┐
       │ IRiskExecutionGuard  │ (Enforces mandatory SL, Daily Loss,
       └──────────┬───────────┘  Max Size, Risk % of Equity, Regime suitability)
                  │
                  ▼
       ┌──────────────────────┐
       │ Execution Permission │ (Enforces Safe Mode & Live Explicit permission)
       └──────────┬───────────┘
                  │
                  ▼
         [Profile Selection]
         ├─── Simulation ──► [SimulationExecutionGateway] ──► In-Memory Execution
         ├─── Paper      ──► [SimulationExecutionGateway] ──► Virtual Paper Desk
         └─── Live       ──► [MT5ExecutionGateway]       ──► MetaTrader 5 Terminal
```

---

## 2. Sequential Order Flow Steps

The execution layer guarantees that every order follows four immutable steps:

1. **Decision**: The strategy compiles and emits a trading signal. The engine encapsulates this into an `OrderRequest` (holding Symbol, Side, Volume, Entry Price, Stop Loss, Take Profit, and Reason).
2. **Risk Validation**: The request is routed to `RiskExecutionGuard`. It executes six critical validation gates:
   - **Stop Loss Existence**: Rejects immediately if no SL is specified.
   - **Daily Loss Limits**: Blocks trade if the cumulative daily loss limit has been breached.
   - **Single Position Size**: Validates that order lots do not exceed maximum bounds.
   - **Cumulative Exposure**: Prevents opening trades that push aggregate exposure beyond configurable limits.
   - **Risk Percentage**: Computes price risk relative to equity using symbols' contract multipliers, blocking trades risking > 2% (or custom thresholds) per trade.
   - **Market Regime suitability**: Restricts entries during restricted conditions (such as extreme volatility).
3. **Execution Permission**: The engine checks the current profile. If `Live` is active, it blocks execution unless `IsLivePermissionGranted` is set to `true`.
4. **Order Routing**: Dispatches the order to the selected gateway interface (`IExecutionGateway`).

---

## 3. Explicit State Machine

To prevent concurrency problems and ambiguous states, the order lifecycle uses explicit states:

| State | Description |
| :--- | :--- |
| **Created** | Initial state of order request. |
| **Validated** | Passed all risk guards. |
| **Submitted** | Forwarded to the gateway. |
| **Accepted** | Confirmed received by the gateway. |
| **Rejected** | Blocked by risk guards or rejected by broker. |
| **Filled** | Fully executed at broker. |
| **PartiallyFilled** | Executed in portions. |
| **Closed** | Filled order/position closed. |

---

## 4. Integration with Experiential Learning (`Nexus.Training`)

Execution triggers public decoupled events:
- `OrderSubmittedEvent`
- `OrderFilledEvent`
- `OrderRejectedEvent`
- `PositionClosedEvent`

These events are designed to easily map into `ExperienceSample` objects inside `Nexus.Training` in future phases. No direct dependencies are introduced, maintaining strict Hexagonal separation.
