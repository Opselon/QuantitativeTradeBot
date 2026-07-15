# Nexus Trading Engine - Code Quality & Engineering Standards

This document establishes the official software quality principles, engineering patterns, naming conventions, and automated testing guidelines for the **Nexus Trading Engine (NTE)** codebase. All commits and reviews must conform to these requirements.

---

## 1. Core Architectural & Code Quality Principles

To keep the platform maintainable, performant, and clean, we strictly enforce these industry-standard principles:

* **SOLID Design**:
  * **S**ingle Responsibility: Each class must execute exactly one job.
  * **O**pen/Closed: Open for extension, closed for modification.
  * **L**iskov Substitution: Child classes must be completely swappable with parent definitions.
  * **I**nterface Segregation: Fine-grained interfaces preventing massive, bloated APIs.
  * **D**ependency Inversion: High-level application code depends only on abstractions (ports), never details (adapters).
* **Clean Code & Self-Documenting Structures**: Write expressive variable and method names. Short methods with clear outputs and descriptive exceptions.
* **DRY (Don't Repeat Yourself)**: Avoid duplicated logic. Encapsulate shared routines, but prioritize structural decoupling over premature, highly nested abstractions.
* **KISS (Keep It Simple, Stupid)**: Favor simple, clear, and readable implementations over complex design patterns or clever code tricks.
* **YAGNI (You Aren't Gonna Need It)**: Do not write feature extensions or speculative code for future steps. Build only what is required for the current phase.

---

## 2. Coding Conventions & Style Guide (C#)

### A. Naming Conventions
* **Namespaces**: Use PascalCase matching the folder hierarchy exactly (e.g., `Nexus.Infrastructure.Persistence`).
* **Classes / Interfaces / Structs**: Use PascalCase. Prefix interfaces with a capital `I` (e.g., `IMt5TradingService`).
* **Method Names**: Use PascalCase, verbs, or verb phrases. Use the `Async` suffix for task-returning methods (e.g., `PlaceOrderAsync`).
* **Variables / Parameters**: Use camelCase (e.g., `priceTick`).
* **Private Fields**: Use camelCase with an underscore prefix (e.g., `_connectionString`).
* **Constants**: Use PascalCase (e.g., `DefaultPort`).

### B. Folders & Files
* **Single Responsibility File Rule**: Place only one public class, interface, or struct in a single file. File names must match the class name exactly.
* **Directory Structure**: Folders must be named logically to isolate boundaries (e.g., `/Ports`, `/Adapters`, `/Workflows`, `/Entities`).

---

## 3. Comprehensive #region Usage Rules

While `#region` blocks are useful for organization, overusing them can hide bloated classes or messy code. To ensure clean files, the following strict boundaries are enforced:

### Approved #region Categories
Inside massive adapters or view models (e.g., MT5 trade clients or manual desks), `#region` blocks should be grouped *only* into the following predefined zones to maintain structural consistency:

1. **`#region Fields & Constants`**: Private fields, internal locks, static buffers, and constants.
2. **`#region Initialization & Constructor`**: DI dependency injection constructor, factory initializers, and component hooks.
3. **`#region Properties & Bindings`**: Public properties, MVVM bindable elements, and view status outputs.
4. **`#region Core Execution API`** (or appropriate functional domain name like **`MT5 Command Gateways`**): Main high-value interface methods.
5. **`#region Event Handlers & Subscriptions`**: Internal listener methods, message broker subscriptions, and event routines.
6. **`#region IDisposable Implementation`**: Standard garbage collection and unmanaged pointer release blocks.

* **Forbidden Region Use**: Never wrap an entire class or write regions inside individual method bodies. If a single region contains more than 300 lines, it is a code smell indicating that the component should be refactored into smaller, decoupled service classes.

---

## 4. Documentation & Comment Guidelines

* **Avoid Obvious Comments**: Do not add comments describing *what* simple lines do. Code should be self-documenting.
* **Document the "Why", Not the "How"**: Use comments to explain non-obvious business logic decisions, performance considerations, thread-safety strategies, or complex math logic.
* **XML Comments**: Public API methods, ports, and core domain interfaces must have detailed XML comments (`/// <summary>`) detailing parameters, expected returns, and exceptions thrown.

---

## 5. Automated Testing Architecture

Testing is not an afterthought; it is an active design tool.

```text
       ┌────────────────────────────────────────────────────────┐
       │                 End-to-End Workflows                   │ (Scenario-focused)
       │           (Nexus.Tests.EndToEnd - xUnit)               │
       └──────────────────────────┬─────────────────────────────┘
                                  ▼
       ┌────────────────────────────────────────────────────────┐
       │                  Integration Tests                     │ (Provider DB / Sockets)
       │          (Nexus.Tests.Integration - xUnit)              │
       └──────────────────────────┬─────────────────────────────┘
                                  ▼
       ┌────────────────────────────────────────────────────────┐
       │                     Unit Tests                         │ (Domain Math / State)
       │              (Nexus.Tests.Unit - xUnit)                │
       └────────────────────────────────────────────────────────┘
```

### A. Unit Testing Rules
* **Target**: Test pure calculations, domain entity status changes, and application workflow inputs in isolation.
* **Rules**: Must execute completely in memory. Never access databases, make network requests, or invoke native binaries.
* **Mocks**: Utilize mock systems or lightweight in-memory stubs (e.g., `StubOperatorService`) to isolate the class under test.

### B. Integration Testing Rules
* **Target**: Validate persistence database mappings, optimistic concurrency tokens, and MT5 TCP socket serialization contracts.
* **Rules**: Execute against real database instances (such as PostgreSQL launched via Testcontainers or SQLite local files).

### C. End-to-End (E2E) Workflow Testing Rules
* **Target**: Orchestrate full execution cycles: boot systems, run background streams, trigger margin calls, and simulate system recovery.
* **Rules**: Leverage test fixtures like `E2ETestHost` to preserve state, routing logs to test outputs via specialized log providers (`TestOutputLoggerProvider`).
