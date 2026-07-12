# Pattern Memory System Design

The Pattern Memory system stores high-dimensional `MarketVector` patterns and performs associative similarity searches to identify historical matching structures in real-time.

## Similarity Search Formulation

Similarity between an incoming query vector $\mathbf{q}$ and a stored pattern vector $\mathbf{p}$ is calculated using the Cosine Similarity metric:

$$\text{Similarity}(\mathbf{q}, \mathbf{p}) = \frac{\mathbf{q} \cdot \mathbf{p}}{\|\mathbf{q}\| \|\mathbf{p}\|} = \frac{\sum_{i=1}^{n} q_i p_i}{\sqrt{\sum_{i=1}^{n} q_i^2} \sqrt{\sum_{i=1}^{n} p_i^2}}$$

## Key Properties

- **Performance**: Normalized values between -1.0 and +1.0 indicating trade outcome returns.
- **Search Complexity**: Optimized linear scanning over thread-safe collections.
- **Filtering**: Filters results above similarity thresholds (e.g. `0.95`).
