# 23_NEXT_SESSION – Stage 4 Planning & Roadmap

## 1. Overview
The goal of the next session (Stage 4) is to transition the manual trading platform from a polling-based architecture to a **high-frequency, real-time push-streaming architecture**. This will significantly reduce network latency and eliminate CPU overhead caused by redundant polling loops.

## 2. Core Roadmap Items

### 2.1. Real-Time Streaming Push Architecture
- **Objective**: Replace the current 5-second polling loop with an event-driven broadcast channel.
- **MQL5 EA Enhancement**: Implement `OnTradeTransaction` and `OnTrade` native event handlers inside `NexusBridge.mq5` to immediately broadcast account, deal, and position changes as they occur.
- **TCP Bridge Extension**: Support full-duplex persistent connections where the MT5 bridge pushes real-time transactional event payloads to C# without waiting for request triggers.

### 2.2. Event-Based Position & Account Updates
- **Objective**: Reflect active terminal positions instantly on the WPF operator dashboard.
- **C# Dispatcher Integration**: Update the `Mt5TradingViewModel` collection immediately upon receiving push-streaming socket packages.

### 2.3. Latency Monitoring & Instrumentation
- **Objective**: Capture and measure system propagation speed.
- **Metrics Dashboard**: Display live round-trip latency statistics (handshake, order execution, position synchronization) on the top Connection Panel.

### 2.4. Native C++ Indicator Optimizations
- **Objective**: Maximize throughput of technical analytical indicators.
- **Indicator Engine Bypass**: Transition high-frequency technical indicator calculations directly to the high-performance C++ analytics layer (`native/Nexus.Native`).
