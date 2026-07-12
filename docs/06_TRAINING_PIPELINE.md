# 06. Training Pipeline

This document serves as an overview of the quantitative machine learning training pipeline for producing NTE ONNX models.

## Training Pipeline Overview

1. **Extract Historical Data**: Pull tick and bar series from partitioned PostgreSQL database tables.
2. **Offline Processing**: Parse ticks into standard sliding window features using Python tools.
3. **Train Models**: Train deep convolutional or transformer neural networks inside PyTorch.
4. **Validation**: Test expected slippage and drawdown constraints inside realistic Python backtesters.
5. **ONNX Export**: Export model parameters to a structured `.onnx` protobuf file.
6. **Integration**: Deploy the model into the live C# execution path.
