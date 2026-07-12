# 10. Architectural Decisions

This document logs significant architectural design records for NTE.

## ADR 01: Host DI WPF Client
We chose to bootstrap the WPF application using `Microsoft.Extensions.Hosting` to align WPF views and ViewModels with Dependency Injection, logging, and application configuration stacks.

## ADR 02: ONNX Runtime Engine
We chose Microsoft ONNX Runtime over Python or C++ bindings to execute deep learning inference natively in C#. This facilitates stand-alone deployment and avoids the complexity of installing Python on production machines.

## ADR 03: Fallback Mode
We implemented a robust deterministic fallback mode inside `NeuralModelService` to guarantee continuous, zero-downtime execution in local dev/simulation environments if physical `.onnx` models are missing.

## ADR 04: C++20 Shared Library Core
We introduced a high-performance C++20 shared library core (`Nexus.Native.Core`) for low-level quantitative and indicator calculations. Managed C# is insufficient for ultra-low microsecond executions because of garbage collection jitter and JIT-compilation overheads.

## ADR 05: Stockfish Incremental Accumulator
To prevent linear $O(n)$ indicator evaluation times during M1 scalping, we introduced a Stockfish NNUE-style incremental accumulator. The current state is updated strictly in $O(1)$ constant time from feature deltas, entirely avoiding heap allocations on ticks.
