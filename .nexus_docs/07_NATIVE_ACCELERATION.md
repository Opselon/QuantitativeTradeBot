# Native C++ Acceleration Layer

## 1. Why Native Acceleration?
To achieve sub-microsecond computation speeds on complex quantitative indicator math and matrix scaling, NTE offloads performance-critical mathematical operations to a native C++ module (`native/Nexus.Native`), keeping system coordination and application logic inside .NET 10.

## 2. Interop Boundary & Memory Guidelines
The C# interop boundary is designed for minimum copying and predictability:
- **P/Invoke**: Uses standard `DllImport` with `Cdecl` calling conventions.
- **Dynamic Resolvers**: Implements standard `.NET 10` `NativeLibrary.SetDllImportResolver` to search for `libnexus_native.so` (Linux) or `nexus_native.dll` (Windows) relative to execution environments and walk up the repository directories, guaranteeing seamless test run executions.
- **Memory Safety**: No unsafe pointer code is exposed to C# clients. Array parameters are pinned and passed as standard references, writing directly into pre-allocated memory results to achieve zero C# garbage collector overhead.

## 3. Real Accelerated Component: Exponential Moving Average (EMA)
The first implemented component is a high-performance rolling EMA calculator:
- **Math**: $\alpha = 2.0 / (period + 1.0)$. Succeeding index: $EMA_t = (values[t] * \alpha) + (EMA_{t-1} * (1.0 - \alpha))$.
- **Cost**: Local loopback JIT compiled operations can add microscopic overhead. Running vectorized C++ loops guarantees deterministic speed.

## 4. Managed Fallback
If the native shared library binary is missing or not supported on the target platform:
- The `NativeIndicatorEngine` detects this on startup (`IsAvailable == false`).
- It seamlessly and transparently delegates all calculations to the pure managed **`ManagedIndicatorEngine`** fallback, logging a diagnostic warning.
- This ensures absolute resilience and compatibility across developer machines, containers, and CI pipelines.
