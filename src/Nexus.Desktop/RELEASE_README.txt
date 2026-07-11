========================================================================
⚡ NEXUS TRADING ENGINE - PORTABLE DESKTOP WORKSTATION ⚡
========================================================================

Welcome to the Nexus Trading Engine (NTE) release candidate package!
This folder contains the pre-compiled portable distribution of the WPF Workstation.

------------------------------------------------------------------------
1. QUICK START VALIDATION (Simulated Offline Mode)
------------------------------------------------------------------------
No MetaTrader 5 or PostgreSQL installations are required to run this test!

Step A: Double-click 'Nexus.Desktop.exe' to launch the workstation app.
Step B: The interactive 6-step Onboarding Wizard will boot up.
Step C: On Step 1, select SQLite (Default is PostgreSQL).
Step D: On Step 2, click 'Initialize Database' (Schema tables and SQLite file are auto-created). Click 'Next Step'.
Step E: On Step 3, leave MT5 Mode set to 'Simulated' (Default). Click 'Next Step'.
Step F: On Step 4, click 'Test MT5 Connection'. The Simulated handshake performs connection diagnostics and succeeds immediately! Click 'Next Step'.
Step G: On Step 5, review the account equity, balance, and free margin metrics. Click 'Next Step'.
Step H: On Step 6, click 'Launch Professional Workspace'.
Step I: Enter the workspace! You can now place Buy/Sell orders, see open positions, and review real-time, sanitized structured diagnostic logs at the bottom panel!

------------------------------------------------------------------------
2. PERSISTENCE PERSISTENCE MODES
------------------------------------------------------------------------
- SQLite: Stores data locally inside 'nexus.db' next to the app. Perfect for quick local testing and evaluation.
- PostgreSQL: Highly recommended for production-grade workloads.
  To configure: Select PostgreSQL on Step 1, enter your Host connection string on Step 2, and click 'Initialize Database' to run partitioned SQL schemas.
  To apply Entity Framework Migrations, you can run the standalone migrations tool:

  $ ./efbundle.exe --connection "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres"

------------------------------------------------------------------------
3. METATRADER 5 BRIDGE INTEGRATION (Real Mode)
------------------------------------------------------------------------
To route real execution orders to your terminal broker account:
Step A: Install MetaTrader 5 Terminal on your client machine.
Step B: Choose 'Real MT5 Bridge' in Onboarding Step 3.
Step C: Run the NTE Expert Advisor (EA) inside your MetaTrader 5 platform ('MQL5/Experts/Nexus/NexusBridge.mq5').
Step D: Ensure the EA is running, and click 'Test MT5 Connection' on Step 4. Handshake communication will execute, and live trading will be activated.

------------------------------------------------------------------------
4. OBSERVABILITY & SECURITY POLICIES
------------------------------------------------------------------------
- Structured Diagnostics: Full logging correlation propagates through background channels.
- Privacy Hardening: Live passwords, database credentials, and API connection keys are fully masked and redacted across all diagnostics.
- Symbol Security: Symbols are validated against safe broker regex profiles (e.g. letters and numbers only) to reject execution injection.

Enjoy trading with Nexus!
========================================================================
