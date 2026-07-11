# Strategy Hosting & Runtime Abstraction

## 1. Abstraction and Registry
To support hosting dozens of algorithms concurrently and loading them dynamically, NTE decouples the strategy logic from the engine container:
- **`IStrategy`**: The pure business-facing interface (`OnInitializeAsync`, `OnTickAsync`, `OnBarAsync`, `OnStopAsync`).
- **`StrategyDescriptor`**: Metadata including subscribed symbols, lookback parameters, and identifier.
- **`IStrategyRegistry`**: Thread-safe directory mapping running strategy instances to their descriptors.

## 2. Host Lifecycle & Fault Isolation
Each strategy runs inside its own **`IStrategyHost`** container. This isolates executing algorithms:
- **Subscriptions**: The host filters incoming ticks/bars, ensuring a strategy only receives data for symbols it subscribed to.
- **Fault Containment**: If a hosted strategy throws an unhandled exception inside `OnTickAsync` or `OnBarAsync`, the exception is caught by the `StrategyHost` using `try-catch` blocks, logged with its unique `CorrelationId`, and the host container remains healthy. A single strategy crash will **never** bring down the execution platform.
- **Runtime Controls**: Supports Pause, Resume, and Stop controls at both the supervisor level (`StrategySupervisor`) and individual strategy host level.
