# ADR 008: Decision Intelligence Architecture

## Context & Problem Statement

Historically, quantitative trading agents have relied on simple point-prediction neural networks (e.g., predicting if the next candle will be green or red) to initiate orders. In real-world institutional markets, this monolithic approach fails because:
1. It ignores alternative possibilities, assuming a single deterministic future.
2. Monolithic networks are unexplainable black boxes, hiding risk and contribution layers.
3. Simple entry/exit triggers do not model mid-trade risk adjustments (like partial close, adding size, or holding state).
4. Unclear handling of uncertainty leads to overtrading in erratic regimes.

We need a central quantitative reasoning engine that decouples market evaluation from execution, structures decision-making through competing hypotheses, aggregates diverse specialized models, and enforces strict explainability and uncertainty gates.

## Design Decisions

We have established a dedicated domain assembly, `src/Nexus.DecisionEngine`, containing the core reasoning subsystems.

### 1. Decoupling Logic from Execution
* **Decision**: Calculated strictly in terms of Expected Value (EV), Risk-Adjusted Returns, and Uncertainty.
* **Execution**: Solved independently by Phase 07 (`Nexus.Execution`), which consumes the decision's final payload.
This clean split allows us to backtest, mock, or dry-run the entire reasoning layer without ever communicating with brokers or modifying trade registers.

### 2. Stockfish-Inspired Tree Search
Instead of predicting a single outcome, the `DecisionScenarioSearchEngine` evaluates a tree of possible candidate actions:
* `BUY` / `SELL` / `WAIT`
* `CLOSE` (Full position exit)
* `REDUCE` (Partial close / size reduction)
* `ADD` (Adding size)

For each candidate, the search engine simulates 5 distinct future scenario paths (Continuation, Reversal, Volatility Expansion, Slippage/Liquidity Failure, and Sideways Consolidation) and computes an Expected Utility Score:
$$\text{Score} = \text{EV} \times (1.0 - P(\text{StopLoss})) - \text{RiskPenalty}$$

### 3. Competing Market Hypotheses
The `MarketHypothesisEngine` explicitly generates and evaluates distinct, parallel market scenarios (e.g. Trend Continuation vs Trend Reversal vs Sideways Mean Reversion). It evaluates each under their relative probabilities and expected returns, ensuring the engine "never assumes one future."

### 4. Modular Multi-Model Consensus
Rather than a single monolithic model, the engine aggregates specialized, independent evaluators:
* `TrendModel`: Identifies directional bias.
* `MomentumModel`: Analyzes RSI/speed levels.
* `VolatilityModel`: Scores range-bound vs breakout.
* `LiquidityModel`: Gauges spread and depth.
* `PatternRecognitionModel`, `OrderFlowModel`, `MacroModel`.

Each returns a normalized `Score`, `Confidence`, and `Explanation`. The consensus aggregator weighs each model's score by its current confidence level.

### 5. Uncertainty-Driven Wait Selection
The `UncertaintyEngine` tracks system-wide uncertainty (derived from high volatility, low neural confidence, or high model divergence). Under high uncertainty, the orchestrator overrides aggressive candidates and selects `WAIT`. The system understands that "no trade is often the best decision."

## Status

**Accepted** & Fully Implemented in Phase 08.
