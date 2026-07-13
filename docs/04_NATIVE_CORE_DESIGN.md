# 04. Native Core Design & Acceleration

The high-performance core compiles down to a shared native C++20 library (`nexus_native_core.dll` on Windows, `libnexus_native_core.so` on Linux) to execute intensive quantitative computations.

## Memory Layout and Alignments

To achieve low-latency execution and support AVX2 SIMD optimizations, structures are decorated with `alignas(32)` inside the C ABI boundary. Memory is pre-allocated and packed using `#pragma pack(push, 1)` to map perfectly to C# `[StructLayout(LayoutKind.Sequential, Pack = 1)]` without any marshaling or pointer-copy overhead.

Heap allocation is entirely avoided inside the tick processing loop, achieving ultra-low nanosecond processing times.

## Stockfish-Inspired Incremental Accumulator

NTE implements a Stockfish-inspired NNUE-style incremental accumulator. Rather than rebuilding indicator vectors from raw history every tick:
- Features are evaluated by adding the incoming `FeatureDelta` to the `Previous State` array.
- This bounds the time complexity to a deterministic constant $O(1)$ and space complexity to $O(1)$.
- Memory blocks are kept inside CPU cache-friendly L1/L2 zones, guaranteeing extremely predictable execution profiles under high-frequency tick storms.
