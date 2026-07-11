# End-to-End Test Plan and Verification Matrix

## 1. E2E Test Flows

This document details the purpose, setup, trigger, expected outcomes, and resilience patterns for the nine E2E workflow tests implemented inside `tests/Nexus.Tests.EndToEnd`.

---

### Flow 1: Market Data Intake Flow
- **Purpose**: Verifies that streaming ticks are successfully received, validated, persisted, and routed to subscribed strategies.
- **Setup**: `E2ETestHost` initialized with a registered `MockE2EStrategy` subscribed to `EURUSD`.
- **Trigger**: Simulated market feed pushes a new tick `(EURUSD, bid=1.0850, ask=1.0851)`.
- **Expected Persistence**: The tick is appended to `IMarketDataRepository`.
- **Expected Execution**: The strategy receives the tick callback and records its execution parameters.
- **Resilience/Security**: Uses `InputValidator` to filter out invalid symbol names or prices before routing.

---

### Flow 2: Strategy Signal to Order Flow
- **Purpose**: Verifies that a generated trade signal transitions seamlessly through risk checks, order creation, gateway execution, and persistence.
- **Setup**: Real `ExecutionCoordinator` with a simulated broker gateway.
- **Trigger**: Strategy emits a `TradeSignal` for `EURUSD` Buy.
- **Expected Persistence**: A new Domain `Order` is saved in `IOrderRepository` with status set to `Filled`.
- **Expected Execution**: Returns successful fill result with unique TicketId.
- **Resilience/Security**: Detailed transaction trace written to `ExecutionAuditService` with matching correlation ID.

---

### Flow 3: Position Lifecycle Flow
- **Purpose**: Verifies that position state transitions, unrealized PnL updates, and trailing stop rules are evaluated and recorded accurately.
- **Setup**: Filled order creates an open `Position`.
- **Trigger**: Update price of position from 1.08500 to 1.08600 (favorable 10 pip movement).
- **Expected Persistence**: Position state modified inside DB.
- **Expected Execution**: Unrealized PnL is updated (+100.00 USD), and trailing stop rule triggers StopLoss modification to 1.08500 (breakeven).
- **Resilience/Security**: Prevents negative or overflow floating values using decimal precision conversions.

---

### Flow 4: Risk Rejection Flow
- **Purpose**: Verifies that risk rejections cleanly block unauthorized order routing and preserve platform health.
- **Setup**: Active account set with 30% drawdown, exceeding the risk rule maximum drawdown threshold of 20%.
- **Trigger**: Strategy attempts to route a Buy order signal.
- **Expected Persistence**: Order saved in DB with status set to `Rejected` and the exact drawdown limit reason recorded.
- **Expected Execution**: Coordinator blocks order execution. No gateway dispatch occurs.
- **Resilience/Security**: Rejection outcome is fully audit-logged; strategy execution remains active and healthy.

---

### Flow 5: Recovery and Restart Flow
- **Purpose**: Verifies that platform state (orders, positions, accounts) is successfully restored from database persistence after a simulated host crash.
- **Setup**: Active state is written to PostgreSQL. Host is completely disposed.
- **Trigger**: Recreate a new `E2ETestHost` and run state restoration checks.
- **Expected Persistence**: Restored orders and positions match previous state.
- **Expected Execution**: Resumes processing without any data loss or duplicate execution.
- **Resilience/Security**: Validates data integrity of datetime UTC boundaries on startup.

---

### Flow 6: Multi-Strategy Concurrency Flow
- **Purpose**: Verifies isolation and subscription boundaries for multiple strategies running concurrently.
- **Setup**: Two strategies registered: StratA (subscribes to `EURUSD`) and StratB (subscribes to `GBPUSD`).
- **Trigger**: Feed incoming ticks for `EURUSD` and `GBPUSD`.
- **Expected Persistence**: Separate tick timeseries written to DB.
- **Expected Execution**: StratA only receives `EURUSD` ticks; StratB only receives `GBPUSD` ticks. No cross-strategy state leakage occurs.
- **Resilience/Security**: Enforces strict domain symbol subscription rules.

---

### Flow 7: Large Batch Performance Sanity Flow
- **Purpose**: Ensures that high-speed time-series bulk persistence handles peak tick bursts without deadlocks or memory exhaustion.
- **Setup**: Prepare a synthetic batch of 1,000 sequential market ticks.
- **Trigger**: Run `AppendTicksAsync`.
- **Expected Persistence**: All 1,000 ticks successfully written and queryable.
- **Expected Execution**: Execution duration completed within a CI-safe threshold (broad bound of 5 seconds).
- **Resilience/Security**: Employs lock-free channels and backpressure limits.

---

### Flow 8: Graceful Native Fallback Flow
- **Purpose**: Verifies that the platform remains fully functional when native C++ binaries are missing or not supported by the OS.
- **Setup**: `INativeAnalyticsEngine` simulated as unavailable (`IsAvailable = false`).
- **Trigger**: Calculate rolling EMA.
- **Expected Persistence**: None.
- **Expected Execution**: Seamlessly routes calculations to `ManagedIndicatorEngine` fallback and returns mathematically identical values.
- **Resilience/Security**: Fault-containment pattern.

---

### Flow 9: Strategy Fault Containment Flow
- **Purpose**: Verifies that a crash in one strategy does not crash the platform or interfere with other healthy strategies.
- **Setup**: Two strategies registered: FailingStrategy (configured to throw) and HealthyStrategy.
- **Trigger**: Route a tick to both.
- **Expected Persistence**: None.
- **Expected Execution**: HealthyStrategy runs successfully; supervisor catches and logs FailingStrategy's error. Both hosts remain fully alive.
- **Resilience/Security**: Complete runtime exception isolation.

---

## 2. Test Verification Matrix

The following matrix displays which acceptance criteria and system layers are validated by which test suite:

| Acceptance Criteria / Layer | Unit Tests | Integration Tests | E2E Tests |
| :--- | :---: | :---: | :---: |
| High-Performance `Tick` allocation | **Yes** | - | - |
| High-Precision math & Value Objects | **Yes** | - | - |
| Monthly PostgreSQL Market Partitions | - | **Yes** | - |
| Optimistic Concurrency (`xmin` row version) | - | **Yes** | - |
| Binary COPY bulk writers | - | **Yes** | **Yes** |
| Native C++ EMA correctness | **Yes** | - | - |
| Native Graceful Fallback | **Yes** | - | **Yes** |
| Strategy Lifecycle Controls | - | - | **Yes** |
| Fault Containment & Crash Isolation | - | - | **Yes** |
| Pre-Trade Risk Rejections | - | - | **Yes** |
| Trailing Stop position lifecycles | - | - | **Yes** |
| State Recovery & Startup validations | - | - | **Yes** |
| Multi-Strategy isolation | - | - | **Yes** |
| Input payload validations | - | - | **Yes** |
| Secrets masking | **Yes** | - | - |
