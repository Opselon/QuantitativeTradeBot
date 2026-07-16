# ADR-006: Learning System Architecture

## Status
Accepted

## Context
As the Nexus Trading Engine (NTE) platform transitions toward autonomous execution, we need a learning foundation that can iteratively evaluate decision outcomes, learn from mistakes, and optimize future behavior without introducing destabilizing live-trading risks. This ADR outlines the core architectural patterns for the learning and training pipeline.

## Architectural Decisions

### 1. Offline-First Training Design
* **Decision**: All training and optimization loops operate strictly offline, decoupled from active live trading systems.
* **Rationales**:
  - **Platform Safety**: Direct online/in-flight weight modifications present severe risks of trading instability, flash crashes, or model hallucination during high-volatility events.
  - **Deterministic Verification**: Offline-first training allows models to be evaluated against identical validation partitions before they can be promoted.
  - **Performance**: Training requires substantial computational resources (gradient calculations, data transformations), which would interfere with the bare-metal low-latency requirements of the tick processing hot path if executed online.

### 2. Experience Replay Buffer with Multi-Strategy Sampling
* **Decision**: We implement an RL-inspired `ExperienceReplayBuffer` supporting random, time-based, and regime-based sampling.
* **Rationales**:
  - **Breaking Autocorrelation**: High-frequency tick data is highly auto-correlated. Sequential sampling leads to catastrophic forgetting and training divergence. Randomized replay breaks these correlations.
  - **Regime-Specific Training**: Market conditions are non-stationary (e.g. ranging vs trending). Regime-based sampling ensures that training sets have a balanced representation of diverse market states rather than being dominated by the most recent regime.

### 3. Multi-Gate Validation Pipeline
* **Decision**: Newly trained models must pass through a strict four-gate validation engine (Backtesting, Walk-Forward, Out-of-Sample, and Paper Trading) before promotion to `Approved` or `Active` status.
* **Rationales**:
  - **Overfitting Prevention**: Out-of-sample (OOS) testing isolates unseen data to verify generalization. Walk-forward testing verifies performance stability over sequential rolling periods.
  - **Risk Bounding**: Paper trading validation simulates execution safety under realistic execution parameters, ensuring the model never violates drawdown limits or risk management guardrails.

### 4. Timeframe Learning Path Separation
* **Decision**: Learning datasets, evaluation metrics, and models are strictly partitioned into separate learning categories: Scalping (M1, M5, M15), Intraday (M30, H1), and Swing (H4, D1).
* **Rationales**:
  - **Behavioral Divergence**: Scalping relies on high-speed flow and low-pip targets, while swing trading depends on high-pip trends and macroeconomic regimes. Mixing these features into a single model leads to conflicting gradients and poor performance.
  - **Isolated Evaluation**: Each category tracks its own metrics (e.g., Win Rate, Profit Factor, Max Drawdown) to permit specialized tuning.

## Consequences
* **Pros**:
  - High safety threshold with guaranteed non-active status for unvalidated models.
  - Clear structural separation prevents cross-timeframe data leakage.
  - Full traceability via detailed `RewardBreakdown` audits.
* **Cons**:
  - Higher disk/storage requirements due to separate timeframe datasets.
  - Offline-only learning means there is a feedback delay before real-world mistakes are corrected (non-real-time adaptation).
