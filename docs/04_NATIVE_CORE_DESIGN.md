# 04. Native Core Design & Acceleration

The high-performance core compiles down to a shared native C++20 library (`nexus_native.dll` on Windows, `libnexus_native.so` on Linux) to execute intensive quantitative computations.

## Responsibilities

- **Rolling Statistics**: Accumulating high-frequency tick data vectors and computing fast metrics.
- **Pattern Processing**: Performing vector comparisons using similarity metrics.
- **Pre-Compiled Math**: Compiling indicators like EMA, SMA, and Volatility bands inside CPU-optimized instructions.

## Interop Bridge

NTE integrates the native shared library inside the C# CLR using .NET 10's native P/Invoke. It features an advanced automatic fallback: if the compiled binary is absent, the engine falls back to high-fidelity, managed C# code. This ensures portable cross-platform compilation and simplifies debugging in diverse development environments.
