# ADR-009: Market Intelligence & Data Fusion Engine Architecture

## Status
Approved

## Context
The platform requires a highly reliable, deterministic, and modular analytical gateway to feed the decision intelligence layers. Prior phases established basic state vectors and neural evaluation fallbacks. However, raw market data is inherently heterogeneous, inconsistent across asset classes (e.g., FX vs. crypto), and subject to temporal synchronization issues across different chart resolutions (M1 to D1).

To avoid primitive obsession, prevent "split-brain" temporal inconsistencies, and satisfy strict institutional requirements, we need a formalized Market Intelligence subsystem. This subsystem must normalize multi-source feeds, align multiple timeframes, classify complex structural regimes, evaluate execution quality, and extract immutable feature sets for downstream neural layers.

## Decision
We implement a dedicated, decoupled C# class library project, `src/Nexus.MarketIntelligence`, adhering strictly to Clean Architecture and Hexagonal Ports & Adapters. The architecture is defined by the following foundational decisions:

### 1. The Unified Market State is the Only Source of Truth
- **Decision**: Downstream decision engines (specifically the tree scenario search engine and multi-model consensus aggregator) must consume a single, cohesive, unified representation of the market.
- **Rationale**: If indicators or price states are evaluated independently in different modules, differences in rounding, caching, or execution timings lead to contradictory decisions. A single, normalized, centralized state ensures total consistency across all platform components.

### 2. Separation of Feature Extraction from AI Inference
- **Decision**: Feature extraction pipelines are kept entirely mathematical, deterministic, and isolated from dynamic neural model inference runtimes (e.g., ONNX).
- **Rationale**: Feature sets must be stable and reproducible. Keeping feature extraction in a pure, compile-time verified C# module enables offline analysis, simplifies model swapability in the Model Registry, and guarantees that identical market snapshots always produce identical tensor inputs.

### 3. Absolute Normalization of All Market Inputs
- **Decision**: Transform all raw inputs—such as absolute asset prices, trading volumes, and bid-ask spreads—into unitless normalized ratios, relative spreads, or scaled percentages.
- **Rationale**: Assets vary wildly in contract sizes, absolute pricing (e.g., EURUSD at 1.08 vs. BTCUSD at 60,000), and ticks. Normalization permits a single downstream model or policy network to generalize across diverse asset classes without requiring custom retargeting or structural changes.

### 4. Centralized Multi-Timeframe Synchronization
- **Decision**: Centralize indicators and price-action synchronization across M1, M5, M15, M30, H1, H4, and D1 charts inside a unified coordinator.
- **Rationale**: Separate indicators computed on independent loops introduce temporal leakage and indicator lag. Centralizing synchronization on a unified clock guarantees that macro-to-micro alignment (Trend, Momentum, Volatility, and Structure) is calculated deterministically with zero future-leakage risk.

## Consequences
- **Clean Separation of Concerns**: No direct dependencies on MetaTrader 5 or broker-specific gateways exist in the core analytics engine.
- **Extremely High Performance**: The use of structured data layouts, pre-allocated vectors, and efficient math helpers ensures negligible CPU overhead.
- **Excellent Testability**: Features, regime classifications, and quality scores are pure, deterministic functions of their inputs, enabling 100% unit test coverage with absolute predictability.
- **AI-Ready Integration**: The generated `ExtractedFeatures` outputs perfectly sorted float and double arrays, directly matching standard ML/ONNX input schemas.
