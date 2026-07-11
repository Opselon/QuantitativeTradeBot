# MetaTrader 5 Integration and Bot Development Guide
A Practical Manual for Robot Deployment, Terminal Configuration, and C# Connectivity.

---

## Part 1: Logging into MetaTrader 5 and Working with the Terminal

### 1) Understanding the MT5 Architecture

MetaTrader 5 (MT5) is a trading terminal designed for both manual and automated trading. The platform includes technical analysis tools, one-click trading, algorithmic trading capabilities, and a built-in Strategy Tester. MQL5 is the native automation language of MT5, used to build Expert Advisors (EAs), indicators, scripts, and libraries.

An essential architectural concept is that account connectivity in MT5 is managed directly by the terminal application, rather than through a public C# API that connects directly to the broker's servers. Well-known open-source projects (such as MtApi) function as bridges between the MT terminal and the .NET application, rather than as direct APIs to the broker's backend.

---

### 2) Terminal Account Login: Methods and Parameters

To establish a connection to a trading account, the terminal requires the login (account number), password, and server name. Two levels of access are supported:
*   **Master Password:** Grants full trading and account management privileges.
*   **Investor Password:** Grants read-only access, allowing you to view account status, analyze prices, and run custom EAs without execution rights.

#### Graphical User Interface (GUI) Method
1. Navigate to **File** or the **Navigator** panel.
2. Click **Login to Trade Account**.
3. Input the **Login**, **Password**, and **Server** (the server address can also be entered manually in an `IP:Port` format).
4. Enabling **Save password** allows the terminal to connect automatically on subsequent startups. The option **Keep personal settings and data at startup** maintains this behavior in the terminal configuration.

#### Command-Line Parameters
To launch the terminal with specific configurations from the command line, MT5 provides several startup parameters:
*   `/login:login number` – Launches the platform with the specified account.
*   `/config:path` – Specifies the path to an alternative configuration file.
*   `/profile:name` – Loads a specific profile.
*   `/portable` – Launches the terminal in Portable Mode.

If incorrect parameters are provided, the terminal falls back to default values.

---

### 3) Master vs. Investor Login Permissions

Understanding the distinction between login levels is important for designing automated systems:

| Feature | Master Password | Investor Password |
| :--- | :--- | :--- |
| **View Balance & Equity** | Yes | Yes |
| **Monitor Open Positions**| Yes | Yes |
| **Run EAs / Read Data**  | Yes | Yes |
| **Submit / Modify Orders**| Yes | No (Execution blocked) |

If your EA needs to execute trades, you must log in using the Master Password. For monitoring, data collection, or performance sharing without execution risk, the Investor Password is appropriate.

---

### 4) Core Terminal Components

For automation purposes, three primary components of the MT5 interface are critical:

1.  **Market Watch:** Provides real-time price quotes, tick charts, contract specifications, and one-click trading options.
2.  **Navigator:** Used to switch between accounts and deploy scripts, indicators, or Expert Advisors.
3.  **Chart:** The workspace where EAs are attached to monitor the ticks of a specific symbol and manage timeframes.

In practice, your EA is typically deployed from the Navigator or the chart's context menu and runs on the active symbol's incoming ticks.

---

### 5) Configuring AutoTrading

Even with a correctly written EA, automated trading must be permitted at both the platform and individual EA levels:

*   **Platform Level:** The **Algo Trading** (or AutoTrading) button in the main toolbar, along with the setting in **Tools → Options → Expert Advisors**, controls automated trading globally. If disabled, no EA can execute trades.
*   **EA Level:** Individual trade permissions must be allowed in the properties of each deployed EA (accessible via the Navigator or chart settings).

Both configurations must be enabled for successful execution.

---

### 6) Accessing Data in MQL5

MQL5 provides standard functions to query account, market, and historical data:

*   **Account Information:** `AccountInfoDouble()`, `AccountInfoInteger()`, and `AccountInfoString()` provide access to balance, equity, margin, leverage, and other account metrics.
*   **Real-time Prices:** `SymbolInfoTick()` retrieves the latest bid, ask, last price, volume, and tick time into an `MqlTick` structure.
*   **Historical Data:** `CopyRates()`, `CopyClose()`, and `CopyOpen()` copy bar data into `MqlRates` structures for historical analysis.
*   **Symbol Selection:** `SymbolSelect()` ensures a symbol is active in the Market Watch window, which is necessary before querying its data.

The official Python integration uses analogous functions (`account_info`, `terminal_info`, `positions_get`, `orders_get`, `order_send`), illustrating the consistent data model used by MT5.

---

### 7) C# Integration via the Bridge Pattern

Because MT5 does not provide a direct, external C# API, the standard approach is to use a bridge architecture where the MT5 terminal runs an EA that communicates with an external C# application.

```
+------------------+                   +--------------------+
|  C# Application  | <=== Network ===> |   MQL5 EA on Chart |
| (Logic & Engine) |       Bridge      | (Execution Gateway)|
+------------------+                   +--------------------+
```

*   **MQL5 Network APIs:** MQL5 supports `WebRequest()` for HTTP/HTTPS requests and the `Socket*` family for TCP/TLS connections.
*   **External Libraries (e.g., MtApi):** These open-source projects establish a bridge between the MT terminal and the .NET application. The EA is attached to an MT5 chart and executes incoming commands sent by the C# application.
*   **Alternative Bridges (e.g., VeriEasi MetaTrader API):** This approach utilizes a proxy layer between MQL and C#. To deploy it, you build the custom installer and place the compiled EA in the terminal's `Experts` folder.

---

### 8) File Structure and Directories

MT5 organizes its operational files into specific directories. You can access the active data folder by selecting **File → Open Data Folder** in the terminal.

Key directories include:
*   `Bases` – Stores historical data and tick history.
*   `Config` – Terminal configuration files.
*   `Logs` – Terminal, Expert Advisor, and Strategy Tester logs.
*   `MQL5` – Contains source code and compiled binaries (`Experts`, `Indicators`, `Scripts`, `Include`, `Libraries`).
*   `Profiles` – Chart profiles and templates.

When using bridges like MtApi, the MQL files of the EA must be placed within the `MQL5\Experts` directory of this folder and compiled using MetaEditor.

---

### 9) Portable Mode

Portable Mode forces the terminal to store all its data, configurations, and history within its installation directory rather than the default user `AppData` folder.

*   **Execution:** Run the terminal via the command line with the `/portable` flag.
*   **Permissions:** If installed in `Program Files`, administrative privileges or appropriate folder write permissions are required. UAC prompts may affect this behavior depending on system security settings.
*   **Use Case:** Portable Mode simplifies environment migration (e.g., moving from a local test environment to a VPS) by keeping the entire directory self-contained.

---

### 10) Step-by-Step Deployment Workflow

The following sequence describes the deployment process from terminal installation to automated execution:

```
[1. Install Terminal & Select Mode]
                │
                ▼
[2. Log in with Credentials (Master/Investor)]
                │
                ▼
[3. Enable Global & Local AutoTrading Settings]
                │
                ▼
[4. Place EA in MQL5\Experts & Attach to Chart]
                │
                ▼
[5. Establish Connection to C# Application]
                │
                ▼
[6. Retrieve Initial Account Status & Market Data]
                │
                ▼
[7. Process Signals & Execute Orders via OrderSend]
                │
                ▼
[8. Monitor Trade Transaction Results & Manage Positions]
```

---

### 11) Strategy Tester Fundamentals

The MT5 Strategy Tester supports backtesting and optimization of EAs before live deployment:
*   **Multi-Symbol & Multi-Threaded:** Supports simultaneous testing of multiple instruments and parallel optimization runs.
*   **Distributed Agents:** Can utilize local CPU threads, remote network agents, or the MQL5 Cloud Network.
*   **Code Design:** To ensure compatibility, EAs should be structured so that their core logic can run within the Strategy Tester environment, noting that network-dependent functions (`WebRequest` or external sockets) may be restricted or simulated during backtests.

---

### 12) Relevant Open-Source Examples

*   **MtApi:** A .NET API bridge designed for MetaTrader 4/5, which maps MQL functions to .NET equivalents and uses a chart-attached EA as an execution proxy.
*   **VeriEasi MetaTraderAPI:** A .NET Framework bridge utilizing an MQL proxy layer. It demonstrates how to handle local compilations and map native data structures to C# classes.

These repositories are best analyzed as architectural design patterns rather than off-the-shelf production systems.

---

### 13) Summary Architecture Diagram

```
[Login to MT5 Terminal]
         │
         ▼
[Enable AutoTrading (Global & EA)]
         │
         ▼
[Attach EA to Target Chart]
         │
         ▼
[EA Connects to C# Bridge (Sockets/HTTP)]
         │
         ▼
[C# Reads Account, Price, & Position Data]
         │
         ▼
[Strategy Logic & Risk Evaluation]
         │
         ▼
[C# Sends Order Command to EA]
         │
         ▼
[EA Executes Order via OrderSend in MT5]
         │
         ▼
[Broker Processes Order]
         │
         ▼
[EA Monitors Execution & Reports Back]
```

---
---

## Part 2: Building a Trading Bot with C# and MetaTrader 5

### 1) System Architectural Separation

When designing a production-grade automated trading system, dividing responsibilities between the execution layer (MQL5) and the logical layer (C#) helps manage complexity and system stability.

```
┌─────────────────────────────────┐      ┌──────────────────────────────────┐
│             MQL5/EA             │      │             C# Engine            │
├─────────────────────────────────┤      ├──────────────────────────────────┤
│ • Order execution               │      │ • Quantitative strategy logic    │
│ • Real-time price feed (ticks)  │ <==> │ • Risk management & validation   │
│ • Local position monitoring     │      │ • Database storage & logging     │
│ • Connection health checks      │      │ • User interface / dashboard     │
└─────────────────────────────────┘      └──────────────────────────────────┘
```

---

### 2) Core Technical Constraints of MT5

#### Constraint 1: Terminal Dependence
The EA runs inside the MT5 terminal process. If the terminal is closed, execution stops. For continuous operation, run the terminal on a Virtual Private Server (VPS) or use a system watchdog to restart the terminal in case of unexpected termination.

#### Constraint 2: Event-Driven execution
MQL5 operates on specific event handlers:
*   `OnInit()`: Triggered when the EA is loaded.
*   `OnDeinit()`: Triggered when the EA is removed or the terminal shuts down.
*   `OnTick()`: Triggered when a new tick is received for the chart's symbol. (Note: Scripts and indicators do not support `OnTick` in the same manner as EAs).
*   `OnTimer()`: Triggered at user-defined intervals (useful for heartbeats).
*   `OnTradeTransaction()`: Triggered when there is a state change in orders, positions, or deals.

#### Constraint 3: Network Operations
*   `WebRequest()` is synchronous (blocking) and requires its destination URLs to be manually whitelisted in the terminal settings under **Tools → Options → Expert Advisors**. It cannot be called within indicators.
*   `Socket*` functions allow asynchronous TCP/TLS communication but require careful design to prevent thread blocking during execution delays.

#### Constraint 4: DLL Dependencies
Using external DLLs requires enabling the **Allow DLL imports** setting in the terminal. DLLs are blocked on cloud and remote optimization agents in the Strategy Tester for security reasons, though they can be run on local agents if permitted in the settings.

#### Constraint 5: Strategy Tester Limits
Backtesting occurs in a simulated environment using historical data. Real-world network latency, slippage, and socket-based communications with external programs may behave differently compared to live trading environments.

---

### 3) Retrieving Account and Market Data

Your integration layer should consistently poll or stream the following native variables:

#### Account Metrics
Using `AccountInfoDouble()`, `AccountInfoInteger()`, and `AccountInfoString()`:
*   `ACCOUNT_BALANCE` (Double) – Account balance.
*   `ACCOUNT_EQUITY` (Double) – Current equity.
*   `ACCOUNT_MARGIN_FREE` (Double) – Available margin.
*   `ACCOUNT_LEVERAGE` (Integer) – Account leverage.

#### Real-time Quotes
Using `SymbolInfoTick(string symbol, MqlTick& tick)`:
*   `tick.bid` – Current bid price.
*   `tick.ask` – Current ask price.
*   `tick.last` – Last traded price.
*   `tick.volume` – Volume of the last tick.

#### Bar History
Using `CopyRates()` to populate `MqlRates` arrays containing:
*   `open`, `high`, `low`, `close` prices.
*   `time` (datetime of the bar start).
*   `tick_volume` and `spread`.

---

### 4) Transaction Models: Orders, Positions, and Deals

MT5 uses distinct concepts to manage transactions:

*   **Order:** An instruction to execute a trade (pending or market).
*   **Deal:** The actual execution of an order on the broker's server.
*   **Position:** The financial contract resulting from one or more deals. An instrument can have at most one open position in Netting mode, or multiple independent positions in Hedging mode.

#### Execution Functions
*   `OrderSend(MqlTradeRequest& request, MqlTradeResult& result)` – Sends a synchronous trade request to the server.
*   `OrderSendAsync()` – Submits an asynchronous request, requiring the system to handle the response later via `OnTradeTransaction()`.
*   `CTrade` (MQL5 Standard Library) – A wrapper class providing simplified functions (`Buy()`, `Sell()`, `PositionClose()`, `PositionModify()`).

When using `CTrade` or raw `OrderSend`, you must verify the operation result using the return code (`result.retcode`) to ensure the request was accepted.

---

### 5) Selecting a Connection Protocol

| Protocol | Implementation Complexity | Latency Profile | Best Suited For | Key Considerations |
| :--- | :--- | :--- | :--- | :--- |
| **WebRequest (HTTP REST)** | Low | Medium | Low-frequency trading, balance updates | Synchronous blocking; requires whitelisting. |
| **Sockets (TCP/TLS)** | Medium-High | Low | Live streaming of ticks, active order management | Requires custom packet framing and reconnect logic. |
| **DLL Bridge** | High | Low | Low-latency local communication | Limited Strategy Tester support; security restrictions. |
| **Shared Files / SQLite** | Low | High | Prototyping, configuration sharing | Not suitable for execution systems due to disk I/O latency. |

---

### 6) Key Implementation Challenges

#### 1. Connection Monitoring and Heartbeats
To prevent the C# engine from losing synchronization with the terminal:
*   Implement a bidirectional heartbeat.
*   The EA can send a periodic ping message using the `OnTimer()` event.
*   If the C# backend fails to receive the ping within a specified window, it should flag the connection as offline and suspend trading signals.

#### 2. Preventing Duplicate Orders
In high-frequency environments or volatile markets:
*   Assign a unique identifier (such as a UUID generated by C#) to each trade request.
*   Pass this identifier through the `action_identifier` or `comment` field of the trade request.
*   Ensure both the C# logic and the EA verify this identifier against active orders before submitting new transactions.

#### 3. Analyzing Return Codes (`retcode`)
Never assume a trade succeeded because `OrderSend()` returned `true`. Always inspect the `MqlTradeResult.retcode`.
*   `10009` (TRADE_RETCODE_DONE) – Request completed.
*   `10018` (TRADE_RETCODE_MARKET_CLOSED) – Market is closed.
*   `10019` (TRADE_RETCODE_NO_MONEY) – Insufficient margin.
*   `10027` (TRADE_RETCODE_ENABLE_PREV) – AutoTrading is disabled in the terminal settings.

---

### 7) Standard Transaction Flow Diagram

```
[C# Strategy Engine]
         │ (Generates Signal)
         ▼
[C# Risk Manager] ──(Validates Equity, Margin, & Current Exposure)
         │
         │ (If Approved: Sends JSON Request over Socket)
         ▼
[MQL5 EA (Socket Listener)]
         │
         │ (Checks Symbol, Spread, & Market Status)
         ▼
[OrderCheck() Execution] ──(Ensures Adequate Funds)
         │
         │ (If Passed: Invokes OrderSend)
         ▼
[Broker Server Execution]
         │
         │ (Returns Transaction State)
         ▼
[MQL5 OnTradeTransaction()]
         │
         │ (Parses MqlTradeResult & Deals)
         ▼
[C# Engine Database] ──(Logs Execution, Slippage, & Position State)
```

---

### 8) Recommended Implementation Phases

If you are building your integration from scratch, a phased approach helps isolate bugs:

1.  **Phase 1: Basic EA Logging**
    Create a minimal EA that prints account status changes and incoming ticks to the terminal log using `Print()`.
2.  **Phase 2: Network Connectivity**
    Implement a simple socket or WebRequest inside the EA to transmit basic metrics (such as equity and balance) to a local C# listener.
3.  **Phase 3: Order Execution Gateway**
    Add order execution capabilities to the EA, allowing it to receive simple buy/sell string commands from your C# console app and execute them via `CTrade`.
4.  **Phase 4: Full State Sync**
    Implement structural synchronization: map open positions, parse `OnTradeTransaction()` events, and track active tickets in the C# database.
5.  **Phase 5: Risk Management & Robustness**
    Add connection watchdog timers, duplicate order prevention, and automated error handling.

---
