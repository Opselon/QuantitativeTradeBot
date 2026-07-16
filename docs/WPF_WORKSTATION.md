# Nexus Trading Workstation & AI Control Center

The **Nexus Institutional Workstation** provides a high-fidelity presentation and control layer for monitoring, controlling, and analyzing the autonomous quantitative trading system. It is designed to act strictly as a presentation layer, while all computational pipelines, neural models, and trading execution strategies reside inside the decoupled application and domain layers.

---

## 1. Workstation Design Guidelines

The workstation layout follows premium institutional desktop standards:
- **Strict MVVM Partitioning**: View controls never contain trading, persistence, or network business logic. ViewModels bind cleanly to decoupled Application Services.
- **Visual Dark Theme**: Custom dark palette utilizing standard WPF styles and dynamic resources to allow smooth theme-switching.
- **4K Display Optimization**: Utilizes relative row/column definitions, scale-independent vector icons (`materialDesign:PackIcon`), and flow grids to render cleanly at ultra-high definitions (UHD/4K).
- **Monospace Render**: Technical, numeric, and financial metrics are formatted using monospace trading data fonts to ensure perfect vertical column alignment.
- **Zero Accidental Executions**: Live trading controls are shielded behind multiple verification gates and require explicit user confirmations.

---

## 2. Advanced Workstation Panels

The workstation dashboard contains 10 responsive, grid-aligned monitors and panels:

### 1. Market Intelligence Panel
Exposes real-time quantitative context for the active asset:
- **Market State & Regime**: Displays the currently classified regime (e.g., "Trending Bullish", "High Volatility Range").
- **Quality Score**: A normalized 0-100 score assessing liquidity, spread tightness, noise, and execution risk.
- **Consensus Bias**: Consolidates multi-timeframe alignment across D1 Trend, H4 Momentum, and M15 Entry timing.

### 2. AI Decision Panel
Visualizes active model inferences and quantitative support:
- **Action & Confidence**: Highlights the current leading choice (e.g., BUY, SELL, WAIT) with its probability.
- **Supporting Evidence**: An enumerated list of positive evidence trails justifying the decision.
- **Rejected Alternatives**: Struck-through actions showing their relative confidence scores and rejection status.

### 3. Scenario Search Visualization
Details the reasoning behind action selection:
- **Expected Utility Bars**: Progress bars mapping the calculated expected value (EV) for alternative future candidates.
- **Selection Reasoning**: Text explainability explaining why the winning action maximizes expected utility while mitigating downside risk.

### 4. Execution Control Panel
Manages profile routing and permission status:
- **Active Profiles**: Easy toggling between Simulation, Paper, and Live modes.
- **Live Trading Permission Toggle**: A prominent Amber/Red-coded security switch requiring explicit, multi-stage user confirmation prior to activating order dispatching to MT5.
- **Real-Time Account Metrics**: Monaco-style indicators showing Balance, Equity, Margin, Exposure, and Drawdowns.
- **Security Audit Trail**: A scrolling monospace logger capturing all permission and profile transitions.

### 5. Training Intelligence Panel
Monitors offline-first learning, datasets, and model lifecycles:
- **Active Neural Artifacts**: Metadata details (Version, Status, Size) of the active model.
- **Replay Buffer**: Counter representing collected experience snapshots inside the training pool.
- **Performance Gates**: Historical test results (Win Rate, Profit Factor, Loss Convergence) from Walk-Forward and Out-of-Sample backtests.

### 6. Native Engine Monitor
Details the performance of bare-metal indicator libraries:
- **Native CPU & Latency**: Standard benchmarks tracking tick-ingress speeds and interop round-trips.
- **Evaluation Speed**: Visual throughput metrics showing evals/sec and features calculated/sec.

### 7. Logs and Explainability Event Viewer
An integrated scrolling terminal capturing system-wide diagnostic, decision, risk rejection, and gateway execution events.

### 8. System Health Monitor
A production-grade, real-time diagnostic ribbon tracking:
- **Subsystem Lamp Indicators**: Color-coded lamps representing status (Healthy, Warning, Critical) for Native C++, Decision Engine, Market Intelligence, Training Engine, Execution Engine, Database, and MT5 Bridge.
- **Monofont Resource Counters**: Monaco-style performance metrics tracking CPU Usage, RAM Heap Allocation (MB), Thread Pool Utilization, and queue lengths.
- **Sub-millisecond Latencies**: High-precision gauges reporting Tick Processing, Decision Inferences, and Order Execution latency.

### 9. Explainability Timeline
A chronological stream displaying how the AI's decision evolved over time. Transitions show sequential states (e.g. WAIT -> BUY -> MOVE_STOP -> PARTIAL_CLOSE -> CLOSE) complete with timestamps, confidence probabilities, triggering models, risk shifts, supporting evidence, and transaction reasons.

### 10. Deterministic Decision Replay
A master-detail reasoning reconstruction tool allowing operators to select any historical decision and review its full quantitative logic in a read-only, deterministic manner, displaying market snapshots, feature vectors, hypotheses, scenario expected utilities, model weights, and execution outcomes.

---

## 3. High Performance Highlights

- **GPU Acceleration**: Leverage WPF `RenderOptions.BitmapScalingMode="LowQuality"` and vector geometry compilation to ensure smooth 60fps telemetry refreshes.
- **Throttled Updates**: Ticks and telemetry parameters are throttled inside background worker threads to avoid UI thread dispatch locking during market bursts.
