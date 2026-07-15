# ADR-002: Domain Model Design

## Status
Accepted

## Context
In professional quantitative trading engines, business concepts are highly sensitive to precision errors, state contamination, and logical coupling. Traditional systems suffer from "primitive obsession" (e.g., passing raw `string` for symbols, raw `double`/`decimal` for prices, and raw `int` for timeframes or minutes) which scatters validation and mapping rules across application services, UI panels, and persistence adapters.

Furthermore, dependencies on database frameworks (such as Entity Framework Core), serialization adapters (like JSON converters), or third-party gateways (such as MetaTrader 5 wrappers) pollute the domain model, hindering performance tuning, compiler optimizations, and future expansion into native cross-platform execution (e.g., C++ performance overlays).

## Decisions
We decided to establish a highly pristine domain layer (`Nexus.Core`) reflecting pure domain-driven design (DDD) principles:

1. **Zero External Dependencies**:
   - The `Nexus.Core` project references no external NuGet packages, database libraries, serialization adapters, or communication components.
   - All external systems must adapt to the domain models defined in this layer through explicit ports (interfaces) defined in the domain and application layers. This isolates the business heart of the platform from technology-specific drift.

2. **Rich, Immutable, Self-Validating Value Objects**:
   - Concepts like `Symbol`, `Price`, `Volume`, `Percentage`, `Money`, `RiskAmount`, `Timeframe`, and `MarketSession` are modeled as Value Objects (primarily using C# `readonly struct` structures where possible to achieve zero-allocation high-frequency performance).
   - Every Value Object validates its own inputs during construction, throwing domain-specific exceptions (e.g., `InvalidPriceException`, `InvalidRiskException`) rather than generic exceptions. This guarantees that once a Value Object exists, it is in a guaranteed valid state.
   - Immutability guarantees that value objects are inherently thread-safe, removing lock contentions during multi-threaded strategy execution.

3. **Domain-Specific Enums**:
   - Developed discrete business classifications such as `OrderSide`, `PositionStatus`, `TradeAction`, `RiskLevel`, `MarketRegime`, and `TimeframeType` to enforce strict compiler-level safety and prevent invalid parameter values across boundaries.

4. **Self-Validating Business Entities**:
   - Entities like `Candle` manage price bar data under strict structural rules (e.g., validating that High is always greater than or equal to Low, Open, and Close).
   - Active state transitions (such as `Update` routines on Candle) trigger internal validation checks, ensuring that entities can never enter a logically corrupt or physically impossible state.

5. **Decoupled Domain Service Contracts**:
   - Ports such as `IMarketEvaluator`, `ITradingDecisionEngine`, `IPositionManager`, and `IExperienceRecorder` are defined purely as interfaces within the domain core.
   - This keeps the system highly adaptable, allowing developers to swap managed C# simulation models, native C++ high-frequency adapters, or neural inference engines seamlessly without editing domain logic.

## Consequences
- **Security & Stability**: The system is completely safe from illogical price distributions, negative volumes, and corrupted timeframe intervals.
- **Maintainability**: The business language of the trading platform is centralized in one place. New developers can understand the quantitative concepts without sorting through database schemas or UI binding triggers.
- **Portability**: Because the domain core is fully decoupled, it can be tested completely in memory with ultra-fast execution speeds (sub-millisecond) and could even be ported to other CLI-conforming frameworks or compiled natively.
- **Adapter Overhead**: External frameworks (like EF Core and MetaTrader 5) must map their native data transfer formats into domain concepts. This small performance/mapping overhead is heavily offset by the mathematical correctness and clean isolation of the engine.
