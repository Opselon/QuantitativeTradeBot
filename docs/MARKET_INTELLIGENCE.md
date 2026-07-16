# Market Intelligence & Data Fusion Engine (Phase 09)

The **Market Intelligence Subsystem** represents the core analytical gateway of the Nexus Trading Engine. It is responsible for transforming raw, heterogeneous market data feeds into a unified, normalized, deterministic, and explainable **Market State** snapshot, which acts as the single source of truth consumed by downstream reasoning and AI decision layers.

---

## Architecture Overview

The subsystem resides in a dedicated class library: `src/Nexus.MarketIntelligence` (referencing `Nexus.Core`). It is designed using Clean Architecture principles, completely isolated from external system dependencies, broker gateways, or specific terminal APIs (such as MetaTrader 5). All communication occurs through abstract interfaces, ensuring clean boundaries.

```text
       [Raw Tick / Candle Streams] -> Normalized Adapters
                                            │
                                            ▼
                              ┌───────────────────────────┐
                              │  MarketIntelligenceEngine │
                              └─────────────┬─────────────┘
                                            │
               ┌────────────────────────────┼────────────────────────────┐
               ▼                            ▼                            ▼
   ┌───────────────────────┐    ┌───────────────────────┐    ┌───────────────────────┐
   │ MultiTimeframeEngine  │    │ MarketRegimeDetector  │    │ MarketQualityEvaluator│
   └───────────┬───────────┘    └───────────┬───────────┘    └───────────┬───────────┘
               │                            │                            │
               └────────────────────────────┼────────────────────────────┘
                                            │
                                            ▼
                                ┌───────────────────────┐
                                │   FeatureExtractor    │
                                └───────────┬───────────┘
                                            │
                                            ▼
                            ┌───────────────────────────────┐
                            │  Unified MarketIntelligence   │
                            │           Snapshot            │
                            └───────────────────────────────┘
```

---

## Architectural Principles & Rationale

### 1. Unified Market State as the Single Source of Truth
Downstream reasoning layers (specifically the Stockfish-inspired Decision Engine) require highly stable and clean representations of market reality to calculate expected values (EV) and explore scenario trees. By routing all data feeds through a single fusion pipeline, we eliminate structural discrepancies, prevent "split-brain" indicator lag, and guarantee that AI and execution blocks evaluate the exact same state representation.

### 2. Separation of Feature Extraction from AI Inference
Feature extraction calculations are mathematical and deterministic. By separating feature extraction from ML model runtimes (e.g. ONNX Runtime), we achieve:
- **Zero inference dependency**: Feature sets can be compiled, stored, and analyzed completely offline or locally without loading neural runtimes.
- **Perfect determinism**: The same market conditions produce identical floating-point vectors, facilitating regression tests, reproducibility, and backtesting consistency.
- **Modular swapability**: AI neural weights can change or be updated in the Model Registry without modifying the underlying market feature engine.

### 3. Normalization of All Market Inputs
In financial markets, instruments possess disparate contract sizes, minimum volume tick steps, decimal digits, and price ranges.
- Transforming these into unitless normalized ratios (e.g., standard deviation relative to mean close, spread relative to average) ensures that machine learning models can generalize across EURUSD, BTCUSD, or Index CFDs without requiring architectural redesigns.

### 4. Centralized Multi-Timeframe Synchronization
Multi-timeframe synchronization is centralized in `MultiTimeframeEngine` to prevent temporal leakage. By synchronizing M1, M5, M15, M30, H1, H4, and D1 candles on a single chronological alignment clock, we guarantee that macro bias (D1/H4) and tactical momentum (M1/M5) are evaluated correctly without incorporating future data points.

---

## Core Components

### 1. Data Sources (`DataSources/`)
- All raw inputs (Tick, OHLC Bars, Volume, Spread, Order Book, Economic Calendar, News, Trading Sessions, and Broker Metadata) are defined as abstract interfaces.
- Zero dependency on MetaTrader 5 ensures the subsystem can run headlessly or with virtual mock systems.

### 2. Multi-Timeframe Engine (`MultiTimeframe/`)
- Synchronizes indicators and alignments across all 7 standard timeframes.
- Computes weighted `ConsensusScore` where higher timeframes carry greater trend weights, and smaller timeframes act as timing filters.

### 3. Market Regime Detector (`Regimes/`)
- Dynamically classifies active market structures into 9 specific regimes: **Trending, Range, Breakout, High Volatility, Low Volatility, Accumulation, Distribution, Manipulation Candidate,** and **Transition**.
- Outputs confidence scores, strengths, and written logical justifications.

### 4. Market Quality Score Generator (`Quality/`)
- Evaluates market suitability and liquidity depth using a normalized `MarketQualityScore` (0-100).
- Combines Liquidity, Spread, Price Noise, Trend Quality, Volatility Stability, and Execution Risk.

### 5. Feature Extraction Pipeline (`Features/`)
- Extracts deterministic float/double vectors covering Trend, Momentum, Volatility, Liquidity, Structure, Session, Time, and Cross-Timeframe features.
- Ensures alphabetical sorting of feature keys for absolute vector stability.

### 6. Decoupled Memory Contracts (`Memory/`)
- Provides `IMarketStateMemory` to enable future pattern matching and comparison with past situations using cosine similarity, completely isolated from learning or training pipeline assemblies.
