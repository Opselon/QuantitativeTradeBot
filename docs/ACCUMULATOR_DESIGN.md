# Incremental Accumulator Architecture Design

NTE uses a rolling accumulator design to avoid costly full recalculations of feature matrices on every market tick.

## Mathematical Formulation

Rather than re-evaluating the full historical dataset, indicators are updated using the prior state and a newly incoming market delta.

$$\mu_n = \mu_{n-1} + \frac{x_n - \mu_{n-1}}{n}$$

$$S_n = S_{n-1} + (x_n - \mu_{n-1})(x_n - \mu_n)$$

$$\sigma^2 = \frac{S_n}{n}$$

This formulation guarantees $O(1)$ constant time complexity and $O(1)$ space complexity, making the processing path exceptionally fast.

## Deployed Services

- **FeatureDelta**: Struct containing symbol, timestamp, priceChange, and volumeDelta.
- **AccumulatorState**: Running counts, means, sums, and squared standard deviations.
- **AccumulatorService**: Manages state dictionaries and applies deltas.
