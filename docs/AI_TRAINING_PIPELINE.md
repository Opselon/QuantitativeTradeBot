# AI Training Pipeline Design

This document details the offline model training pipeline design for producing quantitative neural network models.

## Pipeline Architecture

```text
  Historical Postgres Data
             │
             ▼
  Feature Extraction (Python)
             │
             ▼
  Market Vector Dataset Creator
             │
             ▼
  PyTorch / TensorFlow Training
             │
             ▼
  Evaluation & Validation Backtest
             │
             ▼
  Export to ONNX Model
             │
             ▼
  Production Deployment (NTE)
```

## Detailed Processing Steps

### 1. Extraction
High-frequency tick histories are queried from partitioned PostgreSQL databases.

### 2. Feature Vector Generation
Ticks are aligned into uniform windows. Calculations generate normalized indicators (Symmetric Liquidity, price structure offsets, trend vectors, volatility spreads).

### 3. Training
Deep network architectures (CNN/Transformer/GRU) are trained in Python to predict future momentum and direction ratios.

### 4. ONNX Conversion
Models are exported to `.onnx` files, retaining meta version tags inside CustomMetadataMap blocks.
