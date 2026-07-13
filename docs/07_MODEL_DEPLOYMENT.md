# 07. Model Deployment

This document describes standard procedures for deploying compiled quantitative neural models into NTE production.

## Step 1. Validation
Check the model version inside ONNX metadata properties using the `onnx` Python library.

## Step 2. Directory Placement
Copy the output ONNX model into the workstation configuration folder (e.g., `models/NexusScalarNet_v1.0.onnx`).

## Step 3. Verification
Run the platform, verify that the loaded status badge in the workstation UI transitions to **ONNX_MODEL**, and audit inference latency in the Diagnostics logs.
