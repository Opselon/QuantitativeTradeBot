# Nexus Trading Engine - Native C++ Analytics Integration

This document describes the design, compilation configuration, C-ABI interop boundaries, memory ownership, and runtime integration of the native high-performance C++ quantitative execution engine.

---

## 1. Directory Structure

The native codebases are organized into decoupled public include headers, private implementation source files, and automated unit tests.

```text
src/Nexus.Native.Core/
├── include/
│   └── nexus_core/
│       ├── accumulator.h         # Incremental accumulator definitions
│       ├── core_runtime.h        # Runtime orchestrator interface
│       ├── interop_abi.h         # Shared C-ABI interop structure layout
│       ├── market_state.h        # Real-time quantitative indicators representation
│       └── market_vector.h       # Feature matrix structures bound to ONNX
├── src/
│   ├── accumulator.cpp           # Accumulator implementations (O(1) updates)
│   ├── core_runtime.cpp          # Engine lifetime controls
│   ├── market_state.cpp          # Market intelligence calculations
│   └── market_vector.cpp         # Feature extraction routines
├── CMakeLists.txt                 # Multi-platform CMake build configuration
└── tests/
    └── native_tests.cpp          # Automated C++20 unit tests
```

---

## 2. Compiler Settings & Optimization Strategies

To achieve sub-microsecond tick processing latencies, the native shared library targets advanced CPU instructions and executes deep optimization flags.

### Multi-Platform Flags (`CMakeLists.txt`)
* **Standard**: C++20 (strictly enforced).
* **Optimization (Release)**: `-O3` (GCC/Clang) or `/O2` (MSVC) to execute complete function inlining, loop unrolling, and advanced compiler analysis.
* **Vectorization**: `-mavx2` or `/arch:AVX2` enabling 256-bit SIMD execution models for feature matrix computation.
* **Frame Pointers**: `-fomit-frame-pointer` on GCC/Clang to release an additional register for calculations.
* **LTO (Link-Time Optimization)**: Enabled via `CMAKE_INTERPROCEDURAL_OPTIMIZATION` to perform cross-file optimizations across translations.

---

## 3. The Interop Boundary (C-ABI)

To bridge the .NET runtime with native assemblies without introducing garbage-collection overhead, the interop layer uses a standard C-linkage API (`extern "C"`) in `interop_abi.h`:

### Structures and Memory Alignments
All structures are padded and packed (`#pragma pack(push, 1)`) with explicit memory alignment constraints (`alignas(32)`) to ensure fast, vector-friendly CPU cache line copies:

```cpp
#pragma pack(push, 1)

// AVX2 alignment-compatible structure for streaming ticks
struct alignas(32) TickData {
    int64_t timestamp;
    char symbol_id[32];
    double bid;
    double ask;
    double volume;
    double spread;
};

// Fixed array for MarketVector features bound to ONNX evaluation inputs
struct alignas(32) MarketVectorBuffer {
    float features[64];
};

// Marshallable structure for current market state monitoring
struct alignas(32) MarketStateBuffer {
    char symbol[32];
    int64_t last_updated_utc;
    double volatility;
    double momentum;
    double liquidity;
    double price_structure;
    double probability;
    double risk;
    double currency_strength;
    char market_regime[32];
};

#pragma pack(pop)
```

---

## 4. Managed Gateway & Lifetime Management

On the C# side, `Nexus.Infrastructure.Native` handles the native engine life cycle safely and securely.

### A. Lifetime Control: `NativeCoreSafeHandle`
To avoid severe native memory leaks, raw pointers are never held directly in C# services. Instead, they are wrapped in a robust, custom `SafeHandle`:

```csharp
public sealed class NativeCoreSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public NativeCoreSafeHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            NativeCoreInterop.Destroy(handle);
            handle = IntPtr.Zero;
        }
        return true;
    }
}
```

### B. High-Speed Interop: `[LibraryImport]`
We utilize .NET 10 modern, source-generated interop libraries to compile fast, zero-allocation marshalling code at build-time:

```csharp
public static partial class NativeCoreInterop
{
    private const string LibName = "nexus_native_core";

    [LibraryImport(LibName, EntryPoint = "nexus_core_create")]
    public static partial IntPtr Create();

    [LibraryImport(LibName, EntryPoint = "nexus_core_destroy")]
    public static partial void Destroy(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "nexus_core_update_tick")]
    public static partial int UpdateTick(IntPtr handle, in TickData tick);
}
```

---

## 5. Architectural Resilience & Managed Fallbacks

The platform is designed to be highly portable and resilient. It implements a **Dual-Execution Pipeline**:

1. **Native Path (Default)**: If the compiled shared library (`nexus_native_core.dll` or `libnexus_native_core.so`) is present in the runtime directory, the `NativeMarketIntelligenceService` streams all ticks to the native C++ engine.
2. **Managed Fallback Path**: If the system detects that the native library is missing, compiled for a different OS architecture, or restricted by runtime permissions:
   * The platform raises a warning in system logs (Event ID: `NativeFallbackActivated`).
   * It gracefully and automatically redirects execution to C# high-fidelity managed implementations (e.g., `ManagedIndicatorEngine`, `AccumulatorService`).
   * **Zero Downtime**: The platform remains fully functional in simulated/managed mode with zero application crashes.
