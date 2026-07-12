# QuantitativeTradeBot
C# Algorithmic Trading, MT5 Low-Latency Bot, Quantitative Trading Platform, WPF Trading Terminal.


<!-- NEXUS_AUTO_DOC_START -->

## 🏛️ Nexus Trading Engine (NTE) Architecture Summary
**Style:** Decoupled Hexagonal / Clean Architecture
- **Nexus.Core:** Zero external dependencies. Uses *Zero-Allocation Tick Path*. Contains value objects (`Symbol`, `Money`, `LotSize`) and core interfaces (`IStrategy`, `IRiskManager`).
- **Nexus.Application:** Implements execution logic, `IExecutionGateway`, `ExecutionCoordinator`, and the `IMt5TradingService` with Simulated vs Real Routing adapters.
- **Nexus.Infrastructure:** Adapters (EF Core, Background Workers, Time-Series tick copy).
- **Native C++:** High-performance quantitative engine (EMA calculations) via P/Invoke to bypass JIT.
- **Nexus.WpfUi (WPF Layer):** Rich Desktop UI designed in WPF on .NET 10.

### 📊 Latest Build & Commit Metadata
| Field | Value |
| --- | --- |
| **Commit Message** | Merge pull request #12 from Opselon/feat/stage3-mt5-operator-panel-14977305014805486120 |
| **Author** | Capsizer |
| **Branch** | $env:GITHUB_REF_NAME |
| **Run Number** | $env:GITHUB_RUN_NUMBER |
| **Commit SHA** | $env:GITHUB_SHA |
| **Generated At** | `
2026-07-12 21:53:25 UTC
` |

---
### 📂 Interactive Project Structure Tree
<details>
<summary><b>Click to expand Project Tree (Filtered with WPF, .NET & C++ files)</b></summary>

```text
├── .github/
│   └── workflows/
│       ├── dotnet-build.yml
│       └── release.yml
├── .nexus_docs/
│   ├── 01_ARCHITECTURE.md
│   ├── 02_DATABASE_SCHEMA.md
│   ├── 03_PROGRESS.md
│   ├── 04_NEXT_STEPS.md
│   ├── 05_EXECUTION_PIPELINE.md
│   ├── 06_STRATEGY_RUNTIME.md
│   ├── 07_NATIVE_ACCELERATION.md
│   ├── 08_MT5_PROTOCOL.md
│   ├── 08_SECURITY_MODEL.md
│   ├── 09_E2E_TEST_PLAN.md
│   ├── 10_OBSERVABILITY.md
│   ├── 11_LOCAL_VALIDATION.md
│   ├── 12_DESKTOP_CLIENT.md
│   ├── 13_RELEASE_ENGINEERING.md
│   └── MetaTrade5.md
├── .project/
│   ├── 00_MASTER_PLAN.md
│   ├── 01_ARCHITECTURE.md
│   ├── 08_MT5_PROTOCOL.md
│   ├── 13_EXECUTION_ENGINE.md
│   ├── 21_PROGRESS.md
│   ├── 22_TODO.md
│   ├── 23_NEXT_SESSION.md
│   ├── 25_DECISIONS.md
│   ├── 26_CHANGELOG.md
│   └── 30_PROJECT_STATE.md
├── native/
│   ├── Nexus.Native/
│   │   ├── NexusNative.cpp
│   │   └── NexusNative.h
│   └── build.sh
├── src/
│   ├── Nexus.Application/
│   │   ├── Analytics/
│   │   │   ├── IIndicatorEngine.cs
│   │   │   ├── INativeAnalyticsEngine.cs
│   │   │   ├── ManagedIndicatorEngine.cs
│   │   │   ├── NativeAnalyticsEngine.cs
│   │   │   └── NativeIndicatorEngine.cs
│   │   ├── Mt5/
│   │   │   ├── ClosePositionResult.cs
│   │   │   ├── IMt5TradingService.cs
│   │   │   ├── OpenPositionDto.cs
│   │   │   └── PlaceOrderResult.cs
│   │   ├── Mt5Bridge/
│   │   │   └── Contracts/
│   │   │       ├── BridgeError.cs
│   │   │       ├── BridgeMessageEnvelope.cs
│   │   │       ├── BridgeOrderExecutionStatus.cs
│   │   │       ├── BridgeOrderSide.cs
│   │   │       ├── BridgePositionDto.cs
│   │   │       ├── BridgePositionSide.cs
│   │   │       ├── ClosePositionRequest.cs
│   │   │       ├── ClosePositionResponse.cs
│   │   │       ├── GetAccountSnapshotRequest.cs
│   │   │       ├── GetAccountSnapshotResponse.cs
│   │   │       ├── GetOpenPositionsRequest.cs
│   │   │       ├── GetOpenPositionsResponse.cs
│   │   │       ├── PingRequest.cs
│   │   │       ├── PingResponse.cs
│   │   │       ├── PlaceOrderRequest.cs
│   │   │       └── PlaceOrderResponse.cs
│   │   ├── Observability/
│   │   │   ├── LogEventIds.cs
│   │   │   ├── LoggingExtensions.cs
│   │   │   ├── LogSanitizer.cs
│   │   │   └── WorkflowContext.cs
│   │   ├── Pipeline/
│   │   │   ├── DefaultRiskManager.cs
│   │   │   ├── ExecutionAuditService.cs
│   │   │   ├── ExecutionCoordinator.cs
│   │   │   ├── ExecutionRequest.cs
│   │   │   ├── ExecutionResult.cs
│   │   │   ├── OrderIntent.cs
│   │   │   ├── OrderIntentFactory.cs
│   │   │   ├── PipelineContext.cs
│   │   │   ├── PreTradeRiskEvaluator.cs
│   │   │   ├── RiskDecision.cs
│   │   │   ├── SignalRouter.cs
│   │   │   └── TradeSignal.cs
│   │   ├── Ports/
│   │   │   ├── ExecutionCommand.cs
│   │   │   ├── ExecutionReport.cs
│   │   │   ├── GatewayConnectionStatus.cs
│   │   │   ├── IAccountRepository.cs
│   │   │   ├── IAppConfigurationService.cs
│   │   │   ├── IConnectionHealthMonitor.cs
│   │   │   ├── IDatabaseBootstrapper.cs
│   │   │   ├── IExecutionGateway.cs
│   │   │   ├── IGatewaySession.cs
│   │   │   ├── IGatewaySessionFactory.cs
│   │   │   ├── IMarketDataFeed.cs
│   │   │   ├── IMarketDataRepository.cs
│   │   │   ├── IMt5AccountService.cs
│   │   │   ├── IMt5BridgeClient.cs
│   │   │   ├── IMt5ConnectionService.cs
│   │   │   ├── IMt5Session.cs
│   │   │   ├── IMt5TradeService.cs
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IPositionRepository.cs
│   │   │   ├── ITradingPlatformConnector.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── PriceTickEnvelope.cs
│   │   ├── Security/
│   │   │   ├── InputValidator.cs
│   │   │   ├── ISecretStore.cs
│   │   │   └── SecurityConfiguration.cs
│   │   ├── Strategies/
│   │   │   ├── InMemoryStrategyStateStore.cs
│   │   │   ├── IStrategyHost.cs
│   │   │   ├── IStrategyRegistry.cs
│   │   │   ├── IStrategyStateStore.cs
│   │   │   ├── StrategyDescriptor.cs
│   │   │   ├── StrategyExecutionContext.cs
│   │   │   ├── StrategyHost.cs
│   │   │   ├── StrategyRegistry.cs
│   │   │   └── StrategySupervisor.cs
│   │   ├── Workflows/
│   │   │   ├── DTOs/
│   │   │   │   ├── AccountSnapshotDto.cs
│   │   │   │   ├── ConnectionProfileDto.cs
│   │   │   │   └── ConnectionTestResultDto.cs
│   │   │   ├── ClosePositionCommand.cs
│   │   │   ├── CreateConnectionProfileCommand.cs
│   │   │   ├── DeleteConnectionProfileCommand.cs
│   │   │   ├── GetAccountSnapshotQuery.cs
│   │   │   ├── GetOpenPositionsQuery.cs
│   │   │   ├── GetPersistenceOptionsQuery.cs
│   │   │   ├── InitializeDatabaseCommand.cs
│   │   │   ├── LaunchWorkspaceCommand.cs
│   │   │   ├── MigrateDatabaseCommand.cs
│   │   │   ├── PlaceOrderCommand.cs
│   │   │   ├── SelectPersistenceProviderCommand.cs
│   │   │   ├── TestMt5ConnectionCommand.cs
│   │   │   └── UpdateConnectionProfileCommand.cs
│   │   └── Nexus.Application.csproj
│   ├── Nexus.Core/
│   │   ├── DomainEvents/
│   │   │   ├── MarginCallEvent.cs
│   │   │   └── OrderExecutedEvent.cs
│   │   ├── Entities/
│   │   │   ├── Account.cs
│   │   │   ├── Bar.cs
│   │   │   ├── Order.cs
│   │   │   ├── Position.cs
│   │   │   └── Tick.cs
│   │   ├── Interfaces/
│   │   │   ├── IRiskManager.cs
│   │   │   ├── IStrategy.cs
│   │   │   └── ITrailingManager.cs
│   │   ├── ValueObjects/
│   │   │   ├── LotSize.cs
│   │   │   ├── Money.cs
│   │   │   └── Symbol.cs
│   │   └── Nexus.Core.csproj
│   ├── Nexus.Desktop/
│   │   ├── Converters/
│   │   │   └── EqualityToBooleanConverter.cs
│   │   ├── Models/
│   │   │   ├── DesktopOrderSide.cs
│   │   │   ├── DesktopPositionDto.cs
│   │   │   └── DesktopTradeResult.cs
│   │   ├── Services/
│   │   │   ├── DiagnosticService.cs
│   │   │   ├── IDiagnosticService.cs
│   │   │   ├── IMt5OperatorService.cs
│   │   │   └── Mt5OperatorService.cs
│   │   ├── ViewModels/
│   │   │   ├── AsyncRelayCommand.cs
│   │   │   ├── DesktopPositionViewModel.cs
│   │   │   ├── MainViewModel.cs
│   │   │   ├── Mt5TradingViewModel.cs
│   │   │   ├── RelayCommand.cs
│   │   │   └── ViewModelBase.cs
│   │   ├── Views/
│   │   │   ├── Mt5TradingPanel.xaml
│   │   │   └── Mt5TradingPanel.xaml.cs
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Nexus.Desktop.csproj
│   ├── Nexus.Infrastructure/
│   │   ├── Adapters/
│   │   │   └── Mt5/
│   │   │       ├── RealMt5BridgeAdapter.cs
│   │   │       ├── RealMt5BridgeConnectionService.cs
│   │   │       ├── RealMt5BridgeSession.cs
│   │   │       ├── RealMt5TradingService.cs
│   │   │       ├── RoutingMt5AccountService.cs
│   │   │       ├── RoutingMt5ConnectionService.cs
│   │   │       ├── RoutingMt5TradeService.cs
│   │   │       ├── RoutingMt5TradingService.cs
│   │   │       ├── SimulatedConnectionHealthMonitor.cs
│   │   │       ├── SimulatedMt5AccountService.cs
│   │   │       ├── SimulatedMt5ConnectionService.cs
│   │   │       ├── SimulatedMt5Session.cs
│   │   │       ├── SimulatedMt5TradeService.cs
│   │   │       ├── SimulatedMt5TradingService.cs
│   │   │       └── SimulatedTradingPlatformConnector.cs
│   │   ├── Mt5Bridge/
│   │   │   └── TcpMt5BridgeClient.cs
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   ├── AccountConfiguration.cs
│   │   │   │   ├── OrderConfiguration.cs
│   │   │   │   ├── PositionConfiguration.cs
│   │   │   │   └── TradeConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   │   └── 20260101000000_InitialTradingState.cs
│   │   │   ├── Models/
│   │   │   │   ├── AccountDbModel.cs
│   │   │   │   ├── OrderDbModel.cs
│   │   │   │   ├── PositionDbModel.cs
│   │   │   │   └── TradeDbModel.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── AccountRepository.cs
│   │   │   │   ├── MarketDataRepository.cs
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── PositionRepository.cs
│   │   │   │   └── UnitOfWork.cs
│   │   │   ├── AppConfigurationService.cs
│   │   │   ├── DependencyInjection.cs
│   │   │   ├── DesignTimeNexusDbContextFactory.cs
│   │   │   ├── NexusDbContext.cs
│   │   │   ├── PostgreSqlDatabaseBootstrapper.cs
│   │   │   └── SqliteDatabaseBootstrapper.cs
│   │   ├── Security/
│   │   │   └── WindowsSecretStore.cs
│   │   ├── Workers/
│   │   │   ├── ExecutionWorker.cs
│   │   │   ├── MarketDataIngestionWorker.cs
│   │   │   ├── RecoveryStartupService.cs
│   │   │   └── StrategyDispatchWorker.cs
│   │   └── Nexus.Infrastructure.csproj
│   └── Nexus.WpfUi/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── AssemblyInfo.cs
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       └── Nexus.WpfUi.csproj
├── tests/
│   ├── Nexus.Tests.EndToEnd/
│   │   ├── Fixture/
│   │   │   ├── E2ETestHost.cs
│   │   │   └── TestOutputLogger.cs
│   │   ├── Mocks/
│   │   │   ├── MockE2EStrategy.cs
│   │   │   ├── SimulatedExecutionGateway.cs
│   │   │   └── SimulatedMarketDataFeed.cs
│   │   ├── E2EWorkflowTests.cs
│   │   └── Nexus.Tests.EndToEnd.csproj
│   ├── Nexus.Tests.Integration/
│   │   ├── GlobalUsings.cs
│   │   ├── Nexus.Tests.Integration.csproj
│   │   └── PersistenceIntegrationTests.cs
│   └── Nexus.Tests.Unit/
│       ├── Desktop/
│       │   ├── DesktopTests.cs
│       │   ├── Mt5BridgeTests.cs
│       │   └── Mt5TradingViewModelTests.cs
│       ├── Entities/
│       │   ├── AccountTests.cs
│       │   ├── OrderAndPositionTests.cs
│       │   └── TickAndBarTests.cs
│       ├── ValueObjects/
│       │   ├── MoneyAndLotSizeTests.cs
│       │   └── SymbolTests.cs
│       ├── GlobalUsings.cs
│       ├── IndicatorEngineTests.cs
│       └── Nexus.Tests.Unit.csproj
├── NexusTradingEngine.sln
└── README.md
```
</details>

### 📈 Source File Counts

| File Type | Count |
| --- | ---: |
| C# (.cs) | 178 |
| WPF (.xaml) | 5 |
| C/C++ Source | 2 |
| Projects (.sln, .csproj) | 9 |

### 🐞 Pipeline Diagnostics (CI Stage - Ubuntu)
- **Job Status:** success
#### 🔴 Errors
```text
No explicit C# errors.
```
#### 🟡 Warnings
```text
     7>/home/runner/work/QuantitativeTradeBot/QuantitativeTradeBot/tests/Nexus.Tests.Unit/Desktop/Mt5BridgeTests.cs(533,59): warning CS0067: The event 'Mt5BridgeTests.StubSession.OnStatusChanged' is never used [/home/runner/work/QuantitativeTradeBot/QuantitativeTradeBot/tests/Nexus.Tests.Unit/Nexus.Tests.Unit.csproj]
         /home/runner/work/QuantitativeTradeBot/QuantitativeTradeBot/tests/Nexus.Tests.Unit/Desktop/Mt5BridgeTests.cs(533,59): warning CS0067: The event 'Mt5BridgeTests.StubSession.OnStatusChanged' is never used [/home/runner/work/QuantitativeTradeBot/QuantitativeTradeBot/tests/Nexus.Tests.Unit/Nexus.Tests.Unit.csproj]
```

### 🚀 Pipeline Diagnostics (Release Stage - Windows)
- **Job Status:** success

#### 🔴 Errors
```text
No C# errors.
```
#### 🟡 Warnings
```text
6>D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5BridgeTests.cs(533,59): warning CS0067: The event 'Mt5BridgeTests.StubSession.OnStatusChanged' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Desktop\Mt5BridgeTests.cs(533,59): warning CS0067: The event 'Mt5BridgeTests.StubSession.OnStatusChanged' is never used [D:\a\QuantitativeTradeBot\QuantitativeTradeBot\tests\Nexus.Tests.Unit\Nexus.Tests.Unit.csproj]
```

<!-- NEXUS_AUTO_DOC_END -->
