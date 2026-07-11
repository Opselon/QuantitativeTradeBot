# Nexus Trading Engine - Next Steps

## Immediate Next Steps (Phase 3: Inter-Process Communication & MetaTrader 5 Bridge)
With Phase 2's high-speed database layers successfully running and verified, the next phase focuses on low-latency, bidirectional bridging with MetaTrader 5 (MT5).

### 1. MT5 Bridge Protocol & Tech Stack Selection
* **gRPC / Protocol Buffers (Protobuf)**: Standardized, robust contract definition and cross-language runtime (MT5 runs C++, NTE runs .NET 10 C#). Extremely fast over local loopback sockets.
* **Shared Memory / Named Pipes**: Alternative ultra-low-latency IPC mechanism.

### 2. Protobuf Contract Design
Define the message boundaries and contracts in `.proto` files:
* `market_data.proto`: For streaming Tick and Bar data from MT5 to NTE.
* `execution.proto`: For sending orders, updates, modifications, and cancellations from NTE to MT5, and receiving tickets.

### 3. Tick Stream Adapter
Create the adapter implementing a gRPC streaming service or Named Pipe listener:
* Consumes incoming tick streams at minimal allocation rates.
* Forwards ticks directly to the `MarketDataRepository` and notifies active strategy triggers.

### 4. Execution Gateway Adapter
Create the execution client inside `Nexus.Infrastructure` that:
* Connects to MT5's trade socket.
* Sends trade requests asynchronously.
* Waits for ticket IDs and execution confirmation, translating responses back into Domain order updates.

### 5. Connection Resilience & Auto-Reconnect
* Build background worker services managing socket lifecycles.
* Implement exponential backoff reconnection behavior during network dropouts or terminal restarts.
