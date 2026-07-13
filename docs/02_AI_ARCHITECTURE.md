# 02. AI Architecture & Neural Evaluation Engine

NTE employs a high-performance neural evaluation architecture that enables real-time probability prediction directly inside the fast trading execution path.

## Deployed Components

- **MarketVector**: Normalized 10-feature numerical representation of symbol micro-structure.
- **NeuralModelService**: High-performance ONNX Runtime evaluator that loads models, handles inference sessions, and computes output probabilities.

## Deployed Flow

```text
  Market Tick / Bar
         │
         ▼
  Incremental Feature Generation
         │
         ▼
  MarketVector Creation (10 float attributes)
         │
         ▼
  InferenceSession.Run() via ONNX Runtime
    (or mathematical fallback on offline/local dev machines)
         │
         ▼
  EvaluationResult (BUY / WAIT / SELL Confidences)
         │
         ▼
  Pre-Trade Risk & Decision Evaluation
```

## Fallback Strategy

To guarantee continuous offline testability, local UI exploration, and zero-downtime operations, `NeuralModelService` features an automatic high-fidelity **FALLBACK_MODE** that is activated on-demand if no physical `.onnx` file is supplied.

The fallback mathematically computes expected momentum and trend indicators directly from input vector properties to generate realistic, logical trading evaluations in milliseconds.

## Model Lifecycle

1. **Generation**: Historical tick and bar histories are extracted via PostgreSQL partition datasets.
2. **Offline Training**: Features are generated in Python (PyTorch/Scikit-Learn) and converted to `.onnx` format.
3. **Validation**: Version markers are saved inside the ONNX model's metadata maps.
4. **Loading**: Deployed standalone `.onnx` files are loaded at platform startup by `NeuralModelService` inside C#.
