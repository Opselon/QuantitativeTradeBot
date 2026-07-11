# Desktop Client & Onboarding Architecture

This document describes the design, architecture, and behavior of the `Nexus.Desktop` WPF application.

## 1. Architectural Style: Decoupled MVVM with Host Dependency Injection
The desktop application strictly follows **Model-View-ViewModel (MVVM)** clean boundaries using `Microsoft.Extensions.Hosting` and dependency injection:
- **Views**: Defined entirely in XAML (`MainWindow.xaml`) with zero business or adapter logic in code-behind.
- **ViewModels**: Standard C# ViewModels implementing `INotifyPropertyChanged` and executing commands (`MainViewModel.cs`).
- **Use Case Orchestration**: ViewModels only communicate with the application layer via explicit Commands and Queries (e.g. `InitializeDatabaseCommand`, `TestMt5ConnectionCommand`). Direct UI-to-infrastructure couplings are strictly prohibited.

## 2. Dynamic Persistence Strategy
The platform supports dual-mode database persistence configurable during onboarding:
- **SQLite**: Designed for instant local quick start, local evaluation, and UI/dev/testing setups. Automatically runs `Database.EnsureCreatedAsync()` on the database context.
- **PostgreSQL**: Recommended for enterprise production environments. Automatically sets up schema structure and declaratory partitioning via raw SQL scripts.

Database configuration settings are persisted in a local `nexus_config.json` profile managed by `AppConfigurationService`.

## 3. Secure Secret Storage Abstraction
To comply with strict security standards, account credentials and sensitive passwords must never be logged or saved in plain text:
- **`ISecretStore`**: Abstracted secret store defining save, get, and delete operations.
- **`WindowsSecretStore`**: Uses standard **Windows DPAPI** (`ProtectedData.Protect`) with standard currentUser scope to encrypt password payloads.
- **Platform-Agnostic Fallback**: Automatically falls back to a machine-bound, system-metadata-hashed symmetric XOR algorithm if executed on Linux or macOS environments to ensure seamless, 100% reliable local testability and CI verification.

## 4. MT5 Session & Connection Heartbeat
The system operates through clean platform communication sessions:
- **`IMt5Session` / `IMt5ConnectionService`**: Manages connection lifecycles, test connection handshakes, and account metrics snapshot loading (balance, equity, margins, leverage, and currency).
- **Simulated Fallback**: In the absence of a live MT5 terminal bridge gateway, a high-fidelity simulated adapter generates realistic real-time financial snapshots and transition states.

## 5. Structured Diagnostics & Log Panel
The desktop shell contains a persistent, real-time structured logging panel. Payload properties are automatically sanitized against credentials before being logged, and are rendered in a categorized grid format:
- **Timestamp**: High-precision local times.
- **Subsystem**: Component identifier (e.g., `Persistence`, `Gateway`, `Security`).
- **Level**: Diagnostic severity (e.g., `INFO`, `WARN`, `ERROR`).
- **Message**: Sanitized and redacted log templates.
