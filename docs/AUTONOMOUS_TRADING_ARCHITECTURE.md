# Autonomous Trading Architecture

## Operational boundary

Nexus only creates a trade decision after it receives an authenticated, real MT5 bridge message. Missing bridge, market, account, position, model, or news inputs are represented as **UNKNOWN** and cause a non-executable `WAIT`; they are never replaced with seeded prices, fabricated confidence, or simulated fills.

## Traceable production data flow

```text
MetaTrader 5 terminal
  ├─ every chart tick + subscribed Market Watch tick
  ├─ account / open positions
  └─ M1 M5 M15 M30 H1 H4 D1 OHLCV + ATR + candle structure
          │  (requestId is the trace ID)
          ▼
NexusBridge.mq5 ──HTTP JSON──► /api/v1/bridge/{tick,telemetry}
          │                                │
          ▼                                ▼
  command responses                 MarketDataPipeline
                                           │
                                           ▼
 Native market state → feature extraction → model consensus
                                           │
                                           ▼
 Scenario search → risk gate → MT5 execution response
                                           │
                                           ▼
 experience record → reward evaluation → training → immutable model version
```

Every bridge envelope contains a request ID, command, payload, error, and protocol version. The tick payload carries symbol, UTC millisecond timestamp, bid, ask, spread, and actual tick/real volume. The terminal-intelligence payload carries account balance/equity/margin/free-margin/leverage/currency/drawdown, each open position, and the latest bar for all required timeframes.

## Bridge protocol

* `ReceiveTickStream` is pushed to `POST /api/v1/bridge/tick` for every chart tick and for changed subscribed Market Watch ticks.
* `ReceiveTerminalIntelligence` is pushed to `POST /api/v1/bridge/telemetry` at the configured cadence. It includes only values read from terminal APIs (`AccountInfo*`, `PositionGet*`, `SymbolInfoTick`, and `CopyRates`).
* The EA automatically subscribes all broker-selected Market Watch symbols when `InpStreamAllMarketWatch=true`; it also retains explicit `SubscribeSymbol` and `UnsubscribeSymbol` commands.
* Order and position-management responses retain broker retcode/error information. Execution timestamps and request IDs are the correlation boundary for persistence.

## Decision and risk controls

The decision pipeline evaluates the available actions (`BUY`, `SELL`, `WAIT`, `ADD`, `REDUCE`, and `CLOSE`) from the current market state, model consensus, historical match rate, uncertainty, and risk state. The risk gate blocks execution when risk limits are exceeded. Position size must derive from current account equity and stop distance; no fixed-lot decision is valid.

The EA returns real broker feedback from `OrderSend`, including rejection retcodes and errors. The receiving execution layer is responsible for persisting fill price, slippage, and latency with the originating trace ID.

## Learning loop and model weights

```text
closed real position → experience + realized P/L/drawdown
  → reward evaluator → timeframe dataset → validation gates
  → outcome-weighted policy derivation → immutable artifact
  → ModelRegistry registration → approval/promotion decision
```

Training derives every coefficient deterministically from recorded experience feature/outcome covariance. It does not random-initialize weights, synthesize a loss curve, or seed a training data set. Model artifacts are stored under a new version and `ModelRegistry` refuses duplicate versions, preserving prior models. Model performance includes validation score, realized win rate, average reward, loss, and drawdown.

## News and fundamentals

News and macro inputs are optional upstream providers. A provider failure or missing feed remains explicitly `UNKNOWN` with source/timestamp/trace information and has no invented sentiment or impact. Such missing input lowers confidence or produces `WAIT`; it never generates a placeholder trade signal.

## Current completion

| Capability | Completion | Production status |
| --- | ---: | --- |
| MT5 tick / multi-symbol transport | 100% | Bridge emits real terminal ticks, including all selected Market Watch symbols. |
| Account, position, and multi-timeframe transport | 100% | Bridge emits account, positions, and M1–D1 terminal snapshots. |
| Intelligence, decision, risk, execution trace | 100% | Existing pipeline and bridge request IDs provide the trace boundary. |
| Experience and immutable model registry | 100% | Recorded outcomes train deterministic versioned artifacts. |
| External news / macro connectivity | Provider dependent | No provider is treated as `UNKNOWN`, never as data. |

Completion describes implemented system capabilities, not broker connectivity at a particular deployment. A production installation still requires a running MT5 terminal, the EA attached and WebRequest-whitelisted, configured real providers, and validated risk limits before execution can be enabled.
