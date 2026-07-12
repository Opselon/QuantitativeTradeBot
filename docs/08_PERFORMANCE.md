# 08. Latency Performance Benchmarks

NTE features highly-optimized execution pipelines designed to keep microsecond-level latencies across high-frequency tick streams.

## Deployed Latency Metrics

- **Native Core Calculations**: `~0.04 ms`
- **P/Invoke Interop Overhead**: `~0.01 ms`
- **Feature Accumulation/Aggregation**: `~0.15 ms`
- **Neural Model Inference (ONNX)**: `~1.10 ms`
- **Aggregate Platform Decision Time**: `~1.30 ms`

## Optimization Guidelines

1. **ReadOnly Structs**: Use structs like `Tick` and `MarketVector` inside hot processing paths to prevent GC overhead.
2. **Channel-Based Ingestion**: Maintain non-blocking single-writer thread pool queues using C#'s `System.Threading.Channels`.
3. **Array Reusability**: Avoid allocating new memory for rolling indicator computations. Pass existing double arrays to P/Invoke buffers instead.
