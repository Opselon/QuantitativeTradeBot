# Phase 10 Reality Migration

## Completion assessment

| Area | Completion | Evidence |
|---|---:|---|
| Bridge integration | 80% | Real ticks, terminal account/position snapshots, and M1–D1 bars are emitted by the EA. |
| Market intelligence | 55% | Tick pipeline is connected; order-book and persisted multi-timeframe ingestion are not complete. |
| Decision pipeline | 45% | Native/ONNX pipeline is required for dashboard decisions; scenario evaluation still has synthetic implementations elsewhere. |
| Execution | 50% | Real bridge commands exist; autonomous sizing and management orchestration are incomplete. |
| Training | 50% | Experience-backed deterministic artifact generation exists; scheduled production training/model comparison is incomplete. |
| UI real-data connection | 60% | Dashboard no longer transforms candidate scores through what-if controls or displays seeded training values. |
| **Overall** | **55%** | Production deployment is not yet ready. |

## Removed from the dashboard path

- Interactive what-if volatility/momentum controls and their altered candidate utilities.
- Seeded neural fallback identities and deterministic fallback inference.
- Dashboard defaults for neural confidences, feature vectors, expected values, training win rate, reward, drawdown, loss, execution balance, and equity.
- Simulation execution profile as the dashboard service default.

## Connected real sources

- MT5 `SymbolInfoTick`, `AccountInfo*`, `PositionGet*`, `CopyRates`, and `iATR` via `NexusBridge.mq5`.
- MT5 bridge HTTP tick and terminal-telemetry envelopes.
- Persisted `ExperienceRecords` and MT5 account/position command snapshots.
- A loaded ONNX model and native market evaluator are now required before the dashboard publishes a decision.

## Outstanding diagnostic report

| Dashboard field | Missing source/path | Required solution | Priority |
|---|---|---|---|
| Order-book liquidity | No `MarketBookGet` transmission or consumer | Add order-book DTO, gateway ingestion, and depth imbalance feature. | P0 |
| Multi-timeframe panel | Telemetry envelopes are not persisted into a candle store | Add typed terminal-intelligence consumer, candle repository, and MTF engine update. | P0 |
| Fundamentals/news | Provider interfaces have no configured production implementations | Implement credentialed providers, source timestamps, and symbol impact mapping. | P0 |
| Position management | No autonomous position-management worker invokes MT5 modify/partial-close commands | Add a risk worker with idempotent ticket/trace persistence. | P0 |
| Dynamic sizing | No account/tick-value/stop-distance sizing service feeds execution | Implement broker-symbol specification sizing and pre-trade exposure checks. | P0 |
| Scenario scores | Existing scenario engines include synthetic paths | Replace with recorded-distribution and deployed-model conditional evaluation. | P0 |
| Scheduled learning | Training is request-driven | Add daily worker, out-of-sample comparison, and promotion guard. | P1 |

## Validation rules

A UI decision may be rendered only with a live MT5 snapshot, native evaluator state, and loaded ONNX model. Absent prerequisites must keep the decision non-executable. A model may be promoted only after recorded experience, validation, and comparison with the active model. No numeric fallback may stand in for an unavailable upstream source.
