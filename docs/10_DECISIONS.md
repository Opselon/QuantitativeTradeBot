# 10. Architectural Decisions

This document logs significant architectural design records for NTE.

## ADR 01: Host DI WPF Client
We chose to bootstrap the WPF application using `Microsoft.Extensions.Hosting` to align WPF views and ViewModels with Dependency Injection, logging, and application configuration stacks.

## ADR 02: ONNX Runtime Engine
We chose Microsoft ONNX Runtime over Python or C++ bindings to execute deep learning inference natively in C#. This facilitates stand-alone deployment and avoids the complexity of installing Python on production machines.

## ADR 03: Fallback Mode
We implemented a robust deterministic fallback mode inside `NeuralModelService` to guarantee continuous, zero-downtime execution in local dev/simulation environments if physical `.onnx` models are missing.
