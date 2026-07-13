# 23_NEXT_SESSION – Stage 4 Planning & Roadmap

## 1. Overview
The goal of the next session (Stage 4) is to transition the manual trading platform from a polling-based architecture to a **high-frequency, real-time push-streaming architecture** for trade transactions and execute advanced local pre-trade risk validations.

## 2. Core Roadmap Items

### 2.1. Real-Time Streaming Trade Transactions Push Architecture
- **Objective**: Replace the current manual trading positions polling with an event-driven trade transaction broadcast channel.
- **MQL5 EA Enhancement**: Implement `OnTradeTransaction` and `OnTrade` native event handlers inside `NexusBridge.mq5` to immediately broadcast account, deal, and position changes as they occur.
- **TCP Bridge Extension**: Support full-duplex persistent connections where the MT5 bridge pushes real-time transactional event payloads to C# without waiting for request triggers.

### 2.2. Pre-Trade Local Risk Rules
- **Objective**: Integrate strict local pre-trade risk validation rules on the client workstation to prevent fat-finger or over-exposure trade submissions.
- **WPF Warnings**: Instantly warn the operator when order parameters exceed account capabilities.

### 2.3. Native C++ Indicator Optimizations
- **Objective**: Maximize throughput of technical analytical indicators.
- **Indicator Engine Bypass**: Transition high-frequency technical indicator calculations directly to the high-performance C++ analytics layer (`native/Nexus.Native`).
