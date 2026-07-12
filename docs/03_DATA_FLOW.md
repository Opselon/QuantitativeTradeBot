# 03. NTE Deployed Data Flow

This document details how real-time market ticks flow through the system layers to yield visual telemetry and quantitative decisions.

```text
  [MetaTrader 5 Client / Terminal]
                 │
                 │  (JSON-over-TCP sockets)
                 ▼
  [RealMt5BridgeAdapter / TCP Client]
                 │
                 │  (In-Process Channel Ingestion)
                 ▼
  [MarketDataIngestionWorker]
                 │
                 │  (Increment Feature Delta)
                 ▼
  [AccumulatorService / Feature Generation]
                 │
                 │  (Generates MarketVector)
                 ▼
  [NeuralModelService / ONNX Runtime]
                 │
                 │  (Yields EvaluationResult)
                 ▼
  [DecisionEngine / Pre-Trade Risk Rules]
                 │
                 │  (Yields TradeDecision & UI Telemetry)
                 ▼
  [NexusIntelligenceDashboard / WPF UI]
```

## Step 1. Ingestion
Ticks are pulled over high-speed TCP sockets from the MT5 bridge EA client and parsed into domain `Tick` models.

## Step 2. Feature Accumulation
The `AccumulatorService` calculates rolling means and standard deviations incrementally using `FeatureDelta` payloads.

## Step 3. Prediction & Evaluation
The resulting `MarketVector` is passed into `NeuralModelService` to run ONNX model inference.

## Step 4. Rendering
Results (evaluation confidences, latencies, USD strength, and execution counts) are bound instantly to the MVVM presentation layer in the WPF dashboard workstation.
