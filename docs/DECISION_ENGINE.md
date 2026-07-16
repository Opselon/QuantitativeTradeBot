# Autonomous Decision Intelligence Engine (`Nexus.DecisionEngine`)

The **Nexus Decision Intelligence Engine** is the central quantitative reasoning layer of the Nexus Trading Engine. It is responsible for transforming real-time market snapshots and neural network evaluations into explainable, risk-aware, and highly-validated trading decisions.

Inspired by chess search engines like **Stockfish**, it models decision-making as an evaluation of a tree of candidate actions under multiple competing future hypotheses, rather than a simplistic point prediction of price direction.

---

## 1. Key Responsibilities

1. **Decision Orchestration**: Coordinates the 9-stage Decision Pipeline under strict real-time performance constraints.
2. **Scenario Search**: Performs tree-based search over an expanded action candidate set (BUY, SELL, WAIT, PARTIAL CLOSE, FULL CLOSE, ADD POSITION, REDUCE POSITION).
3. **Market Hypothesis Generation**: Evaluates and compares competing, probabilistic market hypotheses (Trend Continuation, Trend Reversal, Sideways Consolidation).
4. **Multi-Model Consensus**: Modular aggregator combining scores and confidence levels from independent evaluators (Trend, Volatility, Momentum, Liquidity, Pattern, Order Flow, Macro).
5. **Explainability**: Outputs self-contained, descriptive telemetry containing the primary action, alternate rankings, risk summaries, and supporting/contributing model metrics.
6. **Uncertainty Evaluation**: Explicitly models system uncertainty levels to intelligently force a `WAIT` state (no trade) under highly erratic or contradictory signals.

---

## 2. The 9-Stage Decision Pipeline

To ensure complete traceability and testability, decision-making strictly follows a sequential processing pipeline:

```text
    Market Snapshot
          │
          ▼
  Feature Evaluation
          │
          ▼
   Model Evaluation
          │
          ▼
  Scenario Generation
          │
          ▼
   Scenario Scoring
          │
          ▼
   Risk Evaluation
          │
          ▼
   Decision Ranking
          │
          ▼
    Final Decision
          │
          ▼
  Execution Request
```

1. **Market Snapshot**: Ingests live prices, structural indicators, and active account/position states.
2. **Feature Evaluation**: Translates the state into a 64-element floating point feature vector.
3. **Model Evaluation**: Runs the multi-model consensus aggregator across registered models.
4. **Scenario Generation**: Simulates multiple future price paths (Continuation, Reversal, Expansion, Slippage, and Consolidation).
5. **Scenario Scoring**: Evaluates the mathematical expected utility and stop loss probabilities for each action.
6. **Risk Evaluation**: Validates against daily drawdown thresholds, cumulative exposure limits, and pre-trade rules.
7. **Decision Ranking**: Ranks candidate actions by expected value (EV) while penalizing risk drawdowns.
8. **Final Decision**: Overrides rankings based on the system uncertainty engine (e.g., forcing `WAIT`).
9. **Execution Request**: Package the selected strategy payload into an immutable, structured `DecisionPackage` for down-stream automated execution.
