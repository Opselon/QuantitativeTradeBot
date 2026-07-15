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
│       ├── market_state_native.h # Phase 04 C-ABI aligned market state representation
│       ├── market_evaluator.h    # Heuristic market evaluation engine
│       ├── memory_pool.h         # Preallocated lock-free memory allocation pool
│       ├── threading_foundation.h# Task and Event queues with custom ThreadPool
│       ├── lock_free_foundation.h# Abstract and thread-safe lock-free queues
│       └── market_vector.h       # Feature matrix structures bound to ONNX
├── src/
│   ├── accumulator.cpp           # Accumulator implementations (O(1) updates)
│   ├── core_runtime.cpp          # Engine lifetime controls and C-ABI exports
│   ├── market_state.cpp          # Market intelligence calculations
│   └── market_vector.cpp         # Feature extraction routines
├── CMakeLists.txt                 # Multi-platform CMake build configuration
└── tests/
    └── native_tests.cpp          # Automated C++20 unit tests & performance benchmarks
```

---

## 2. Architecture Diagram

The native engine runs on a multi-threaded, pre-allocated, pipeline-driven model to ensure zero-allocation hot-path processing.

```text
                  Incoming Tick Stream (MQL5 / TCP)
                                  │
                                  ▼
                    ┌───────────────────────────┐
                    │     Lock-Free Queue       │ (MarketDataQueue)
                    └─────────────┬─────────────┘
                                  │
                                  ▼
                    ┌───────────────────────────┐
                    │      Memory Pool          │ (Preallocated blocks)
                    └─────────────┬─────────────┘
                                  │
                                  ▼
                    ┌───────────────────────────┐
                    │  Incremental Accumulator  │ (O(1) feature additions)
                    └─────────────┬─────────────┘
                                  │
                                  ▼
                    ┌───────────────────────────┐
                    │     Evaluation Cache      │ (Nano-second hit / miss)
                    └─────────────┬─────────────┘
                                  │ (Cache Miss)
                                  ▼
                    ┌───────────────────────────┐
                    │     Market Evaluator      │ (Calculates EvaluationResult)
                    └─────────────┬─────────────┘
                                  │
                                  ▼
                  C# Managed Boundary via [LibraryImport]
```

---

## 3. Compiler Settings & Optimization Strategies

To achieve sub-microsecond tick processing latencies, the native shared library targets advanced CPU instructions and executes deep optimization flags.

### Multi-Platform Flags (`CMakeLists.txt`)
* **Standard**: C++20 (strictly enforced).
* **Optimization (Release)**: `-O3` (GCC/Clang) or `/O2` (MSVC) to execute complete function inlining, loop unrolling, and advanced compiler analysis.
* **Vectorization**: `-mavx2` or `/arch:AVX2` enabling 256-bit SIMD execution models for feature matrix computation.
* **Frame Pointers**: `-fomit-frame-pointer` on GCC/Clang to release an additional register for calculations.
* **LTO (Link-Time Optimization)**: Enabled via `CMAKE_INTERPROCEDURAL_OPTIMIZATION` to perform cross-file optimizations across translations.

---

## 4. Hot Path Memory Design

The engine strictly enforces a **Zero Dynamic Heap Allocation Policy** on the hot path.

### Preallocated Storage
Through **`MemoryPool<T, Capacity>`**, blocks of `MarketStateNative` or `EvaluationResult` objects are pre-allocated in contiguous arrays during engine startup. Allocations and deallocations utilize constant-time pointer swaps, completely bypassing global allocator locking mechanisms and dynamic fragmentation.

### Cache line alignment
Structures are padded and aligned (`alignas(32)`) to ensure that multiple variables do not split CPU cache lines, preventing cache thrashing and allowing the compiler to auto-vectorize execution loops into SIMD instructions.

---

## 5. Threading & Queue Foundations

The engine includes lightweight abstractions supporting non-blocking concurrent evaluations.
* **`ThreadPool`**: Coordinates parallel evaluations and asynchronous analytical runs using a lock-free queue or lightweight task schedule.
* **`TaskQueue` / `EventQueue`**: Safe communication channels transferring structured tick events between ingestion threads and executor routines.
* **`MarketDataQueue` / `EvaluationQueue`**: Explicit queue structures designed to absorb high-frequency trading bursts without risking packet loss or pipeline stalls.

---

## 6. The Interop Boundary (C-ABI)

To bridge the .NET runtime with native assemblies without introducing garbage-collection overhead, the interop layer uses a standard C-linkage API (`extern "C"`) in `interop_abi.h`:

### Safe Handles & Marshalling
All structures are padded and packed with explicit memory alignment constraints:

```cpp
#pragma pack(push, 1)

// Phase 04 C-ABI aligned structure
struct alignas(32) MarketStateNative {
    char symbol[32]{};
    int64_t timestamp = 0;
    int32_t timeframe = 0;
    double open_price = 0.0;
    double high_price = 0.0;
    double low_price = 0.0;
    double close_price = 0.0;
    double last_price = 0.0;
    double volume = 0.0;
    double tick_volume = 0.0;
    double volatility = 0.0;
    double trend = 0.0;
    double momentum = 0.0;
};

struct alignas(32) EvaluationResult {
    float overall_score = 0.0f;
    float confidence = 0.0f;
    float trend_score = 0.0f;
    float momentum_score = 0.0f;
    float liquidity_score = 0.0f;
    float risk_score = 0.0f;
};

#pragma pack(pop)
```

---

## 7. Future Roadmap

1. **Phase 05 (Strategy Integrations)**:
   * Implement custom MCTS (Monte Carlo Tree Search) algorithms inside `ThreadPool` to pre-calculate trading scenario values.
   * Expand the `EvaluationCache` to support distributed synchronization.
2. **Phase 06 (Hardware-Specific Accelerations)**:
   * Target AVX-512 vector math for high-frequency matrix scaling.
   * Introduce hardware-pinned lock-free ring-buffers for zero-latency cross-thread tick routing.
