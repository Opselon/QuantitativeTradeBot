# 08. Latency Performance Benchmarks

NTE features highly-optimized execution pipelines designed to keep microsecond-level latencies across high-frequency tick streams.

## Deployed Latency Metrics (With C++20 Native Core Active)

- **C++ Tick Ingestion Processing**: `~0.005 ms` (5 microseconds)
- **C++ Market State Calculations**: `~0.012 ms` (12 microseconds)
- **C++ Vector Generation Time**: `~0.008 ms` (8 microseconds)
- **P/Invoke Interop Overhead**: `~0.010 ms` (10 microseconds)
- **Neural Model Inference (ONNX)**: `~1.100 ms`
- **Aggregate Platform Decision Time**: `~1.135 ms` (Down from 1.30 ms managed)

## Optimization Guidelines

1. **Blittable Structures**: Keep P/Invoke structures 100% blittable using fixed buffers (`fixed byte` or `fixed float`) to eliminate CLR marshalling copy allocations.
2. **ReadOnly Structs**: Use structs like `Tick` and `MarketVector` inside hot processing paths to prevent GC overhead.
3. **Channel-Based Ingestion**: Maintain non-blocking single-writer thread pool queues using C#'s `System.Threading.Channels`.
4. **Incremental updates**: Leverage the C++ Stockfish-style incremental evaluation to ensure constant latency regardless of history length.
