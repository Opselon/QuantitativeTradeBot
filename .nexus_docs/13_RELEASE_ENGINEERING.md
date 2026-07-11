# Release Engineering and Distribution Readiness

This document outlines the versioning, distribution, packaging, database migration, and verification architecture for the Nexus Trading Engine (NTE) platform.

---

## 1. Automated Versioning Architecture

NTE implements a single source of truth for solution-wide versioning using **`Directory.Build.props`** located at the repository root.

- **Central Versioning System**:
  ```xml
  <Project>
    <PropertyGroup>
      <Version Condition="'$(VERSION)' == ''">0.64.0</Version>
      <Version Condition="'$(VERSION)' != ''">$(VERSION)</Version>
    </PropertyGroup>
  </Project>
  ```
- **MSBuild Bumping**: Any build/publish command can dynamically override the assembly version, file version, and informational version using the `-p:VERSION=[value]` build property.
- **Dynamic UI Bindings**: The WPF desktop application extracts version metadata at runtime from the active assembly:
  ```csharp
  var assembly = typeof(MainViewModel).Assembly;
  var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
  var versionStr = attribute?.InformationalVersion ?? assembly.GetName().Version?.ToString(3);
  ```
  The Window Title (`Title`) and the left sidebar footer text blocks are dynamically bound to this version property, ensuring that the visual interface is always synchronized with the distributed binary.

---

## 2. GitHub Actions Deployment Pipelines

We define two robust workflows inside `.github/workflows/`:

### 2.1 Continuous Integration (`dotnet-build.yml`)
Runs on every push and pull request targeting integration branches.
- Restores all dependencies.
- Compiles the managed C# solution and native indicators.
- Executes the entire unit, integration, and scenario-based end-to-end (E2E) test suite.
- Captures test results for continuous evaluation.

### 2.2 Release Pipeline (`release.yml`)
Triggers via `workflow_dispatch` (manual input parameter for version) or tag pushes (`v*`, `release-*`).
- Determines version based on dispatch parameter or git tag name (with automatic stripping of `v` prefixes and support for `release-[num]` formatting).
- Executes verification checks (restores, compiles, and tests).
- Installs Entity Framework CLI tools.
- Produces two separate distribution packages (self-contained and framework-dependent).
- Generates a standalone Entity Framework **Migration Bundle** (`efbundle.exe`).
- Packages default configuration files.
- Generates zipped portable application assets.
- Automatically creates a GitHub Release draft or published release, uploading all produced zips.

---

## 3. Self-Contained Desktop Packaging Strategy

The desktop WPF client is packaged in two formats targeting **Windows x64**:

1. **Self-Contained Portable App (`Nexus.Desktop-win-x64-self-contained-[version].zip`)**:
   - Contains its own standalone .NET 10 CLR runtime and native dependencies.
   - Requires zero local .NET installations, allowing the app to run on any clean Windows client machine instantly.
2. **Framework-Dependent Portable App (`Nexus.Desktop-win-x64-framework-dependent-[version].zip`)**:
   - Lightweight package requiring .NET 10 runtime to be pre-installed on the host system.

Trimming is disabled (`-p:PublishTrimmed=false`) and standard directory packaging is preferred over Single-File execution to ensure robust XAML/WPF dependency injection, native indicators path resolution, and seamless localization behavior.

---

## 4. Configuration Readiness & Default Settings

To minimize manual user friction, a pre-packaged **`nexus_config.json`** is injected directly into each zipped release:

```json
{
  "SelectedProvider": "SQLite",
  "ConnectionString": "Data Source=nexus.db",
  "IsOnboarded": false,
  "ProfileName": "DefaultMT5",
  "BrokerServer": "ICMarkets-Demo",
  "LoginAccountId": "7820491",
  "TerminalPath": "C:\\Program Files\\MetaTrader 5\\terminal64.exe",
  "TimeoutSeconds": 30,
  "AutoReconnect": true,
  "Mt5Mode": "Simulated",
  "Mt5BridgeHost": "127.0.0.1",
  "Mt5BridgePort": 5000,
  "Mt5BridgeUseSsl": false
}
```

- **Out-of-the-Box Local Testing**:
  - `SQLite` is configured as the default database provider.
  - `Simulated` is configured as the default MetaTrader 5 mode.
  - This allows a tester or auditor to download the app, run it, and validate the complete workflow of the onboarding wizard and workspace panels without installing PostgreSQL or MetaTrader 5 locally.

---

## 5. Database & Migration Readiness

NTE utilizes two database persistence adapters: SQLite (for rapid local evaluation/E2E testing) and PostgreSQL (for high-throughput multi-partition enterprise execution).

- **SQLite Auto-Creation**:
  When SQLite is selected, the application calls `EnsureCreatedAsync()`, which automatically scaffolds the DB schema file (`nexus.db`) on startup without requiring any EF migrations.
- **PostgreSQL Script Execution**:
  When PostgreSQL is selected, the application boots up and offers an interactive **Initialize Database** option. This utilizes SQL schema partition scripts (`001_create_schema.sql`, `002_create_market_partitions.sql`, `003_create_indexes.sql`) which are copied to the build output under `Persistence/Scripts/` automatically.
- **EF Core Migration Bundle**:
  The standalone binary **`efbundle.exe`** is generated during release compilation and is packaged next to the executable in the zip files. This executable applies migrations directly to target databases using:
  ```bash
  efbundle.exe --connection "Host=your-server;Database=your-db;Username=postgres;Password=password"
  ```
  This eliminates the need for installers or developers to have the full dotnet SDK/source code to configure and prepare database instances.

---

## 6. Local Validation & Verification Steps

A tester can easily validate release distribution quality using the following checklist:

1. **Download & Extract**: Download and extract the self-contained `.zip` package.
2. **Execute**: Launch `Nexus.Desktop.exe`.
3. **Verify Version**: Ensure the window title bar and left sidebar display the current release tag (e.g., `0.64.0`).
4. **Onboarding Wizard**:
   - Choose **SQLite** on Step 1 (Default).
   - Click **Initialize Database** on Step 2.
   - Click **Next Step** through Step 3 (Simulated mode active).
   - Click **Test MT5 Connection** on Step 4; verify it performs Simulated handshake and succeeds immediately.
   - Check the account snapshot metrics on Step 5.
   - Click **Launch Professional Workspace** on Step 6.
5. **Trading Station**:
   - Submit Buy/Sell market orders and verify active position tables are refreshed in real-time.
   - Observe live logs rendering inside the structured diagnostic panel, checking that credentials are fully redacted and sanitized.
