# 05. Neural Engine Design

The platform's future neural evaluation architecture is structured to support real-time predictions without Python.

## Core Predictors

- **Trend Direction**: Predicts bullish vs. bearish momentum regimes.
- **Volatility Forecasting**: Predicts upcoming market expansion periods.
- **Liquidity Failure Risk**: Analyzes depth changes to evaluate spread widening risks.

## Vector Specifications

Neural models accept a fixed-layout floating point array matching the `MarketVector` layout:

1. `PriceStructure`: Normalized support and resistance boundaries.
2. `TrendState`: Vector direction metric (-1 to +1).
3. `Momentum`: Relative strength index/rate of change (-1 to +1).
4. `Volatility`: Standard deviation of prices normalized.
5. `VolumePressure`: Buying pressure relative to selling pressure.
6. `Liquidity`: Average bid-ask spreads and depth ratios.
7. `UsdStrength`: Relative ecosystem strength of USD.
8. `SessionState`: Mapped integer representing active trading hours.
9. `MarketRegime`: Numerical classification of ranging vs. trending environments.
10. `RiskState`: Aggregate exposure and drawdown metric.
