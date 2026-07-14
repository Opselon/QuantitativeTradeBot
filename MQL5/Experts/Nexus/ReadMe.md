# NexusBridge MT5

## Pragmatic Execution & Telemetry Adapter for the Nexus Trading Engine (NTE)

NexusBridge is the specialized, low-latency MetaTrader 5 client gateway for the **Nexus Trading Engine (NTE)**. 

### The MQL5 Pragmatic Reality
While clean architecture principles dictate complete separation of concerns, executing this pattern inside the native MQL5 environment requires distinct pragmatic compromises. MQL5 lacks runtime reflection, true generic containers, dynamic DLL loading at runtime, and standard mock frameworks. Attempting to implement a heavy, .NET-style Dependency Injection (DI) container or dynamic service locator inside MQL5 introduces unnecessary complexity, runtime instability, and execution latency.

Therefore, NexusBridge implements **Compile-Time Dependency Inversion**. The bridge acts strictly as a **deterministic execution and telemetry adapter**. The complex analytical "brain" (strategy orchestration, neural network evaluations, global portfolio risk, search-space decision algorithms) is offloaded to the C# (.NET 10) and native C++ quantitative core of the NTE.

---

## Architectural Philosophy: The Lean Edge Adapter

```
┌─────────────────────────────────────────┐
│     NTE CORE ENGINE (C# / C++20)        │
│  "The Brain, Domain & Neural Engine"    │
└────────────────────┬────────────────────┘
                     │ Secure TCP Stream
                     ▼
┌─────────────────────────────────────────┐
│         NEXUSBRIDGE (MQL5 EA)           │
│  "The Deterministic Execution Adapter"   │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │    Application Router / Queue   │   │
│   └────────────────┬────────────────┘   │
│                    │ (Interfaces)       │
│                    ▼                    │
│   ┌─────────────────────────────────┐   │
│   │      Concrete MT5 Adapters      │   │
│   └────────────────┬────────────────┘   │
└────────────────────┼────────────────────┘
                     │ Native MQL5 API
                     ▼
┌─────────────────────────────────────────┐
│           METATRADER TERMINAL           │
└─────────────────────────────────────────┘
```

By constraining the MQL5 footprint to a lightweight execution adapter, the bridge achieves:
1. **Low Runtime Overhead:** Negligible execution path latency.
2. **Deterministic Behavior:** Simple static allocation patterns that minimize memory fragmentation.
3. **Resiliency:** Clean state reconciliation on socket dropouts or terminal restarts.

---

## Directory Structure

```text
MQL5/
└── Experts/
    └── Nexus/
        ├── NexusBridge.mq5              # EA Entry Point & Static Composition Root
        │
        ├── Core/                        # System Bootstrapping & Shared Context
        │   ├── Bootstrap.mqh            # Static object graph constructor
        │   ├── Constants.mqh            # Global system boundaries and error codes
        │   ├── AppContext.mqh           # Global access reference for core systems
        │   └── Exceptions.mqh           # Base structural error definitions
        │
        ├── Interfaces/                  # Abstract Contracts (Dependency Inversion Ports)
        │   ├── ITradeService.mqh        # Contract for placing and modifying positions
        │   ├── IMarketService.mqh       # Contract for accessing indicator & candle data
        │   └── IMessageTransport.mqh    # Contract for outbound TCP / frame transport
        │
        ├── Application/                 # Use Case Managers & Orchestrators
        │   ├── CommandHandler.mqh       # Parses and dispatches incoming system commands
        │   ├── EventHandler.mqh         # Marshals local trade/tick updates to transport
        │   └── MessageQueue.mqh         # Prioritized outbound messaging buffer
        │
        ├── Adapters/                    # Concrete Implementations of Interfaces (Adapters)
        │   ├── MT5TradeAdapter.mqh      # Wraps native OrderSend and OrderSendAsync API
        │   ├── MT5MarketAdapter.mqh     # Handles native tick capture and historic MqlRates
        │   └── TcpTransportAdapter.mqh  # Manages low-level WinAPI TCP socket connections
        │
        ├── Protocol/                    # Wire-Format Serialization & Validation
        │   ├── RequestModels.mqh        # Struct definitions for incoming action payloads
        │   ├── ResponseModels.mqh       # Struct definitions for outbound acknowledgements
        │   └── JsonSerializer.mqh       # Lightweight, manual string builder for JSON serialization
        │
        └── Security/                    # Inbound Command Verification
            ├── Authentication.mqh       # Cryptographic signature validation (SHA-256)
            └── InputValidator.mqh       # Range checks to prevent out-of-bounds orders
```

---

## Interface-Based Inversion (MQL5 Implementation)

To decouple the execution pipeline from native MT5 dependencies, all application logic references abstract interface classes. 

### 1. Abstract Port: `ITradeService`
```mql5
// ITradeService.mqh
#include "../Protocol/RequestModels.mqh"
#include "../Protocol/ResponseModels.mqh"

class ITradeService
{
public:
   virtual      ~ITradeService() {}
   
   virtual bool PlaceOrder(const PlaceOrderRequest &request, PlaceOrderResponse &out_response) = 0;
   virtual bool ClosePosition(const ClosePositionRequest &request, ClosePositionResponse &out_response) = 0;
};
```

### 2. Concrete Adapter: `MT5TradeAdapter`
```mql5
// MT5TradeAdapter.mqh
#include "../Interfaces/ITradeService.mqh"

class MT5TradeAdapter : public ITradeService
{
public:
   virtual ~MT5TradeAdapter() {}

   virtual bool PlaceOrder(const PlaceOrderRequest &request, PlaceOrderResponse &out_response) override
   {
      MqlTradeRequest mqlRequest = {};
      MqlTradeResult  mqlResult  = {};

      // Map request structures to native MT5 models
      mqlRequest.action       = TRADE_ACTION_DEAL;
      mqlRequest.symbol       = request.Symbol;
      mqlRequest.volume       = request.Volume;
      mqlRequest.type         = (ENUM_ORDER_TYPE)request.OrderType;
      mqlRequest.price        = request.Price;
      mqlRequest.sl           = request.StopLoss;
      mqlRequest.tp           = request.TakeProfit;
      mqlRequest.magic        = request.MagicNumber;
      mqlRequest.deviation    = request.Slippage;
      mqlRequest.comment      = "NTE Execution";

      ResetLastError();
      bool success = OrderSend(mqlRequest, mqlResult);

      // Populate response model safely
      out_response.Retcode = mqlResult.retcode;
      out_response.Ticket  = mqlResult.order;
      out_response.Price   = mqlResult.price;
      out_response.Volume  = mqlResult.volume;
      out_response.Success = (success && mqlResult.retcode == TRADE_RETCODE_DONE);

      return out_response.Success;
   }

   virtual bool ClosePosition(const ClosePositionRequest &request, ClosePositionResponse &out_response) override
   {
      // Native close validation logic
      return false; 
   }
};
```

---

## Static Composition & Context Registry

Rather than using dynamic runtime dependency injection, objects are created statically or during the EA's initialization phase in the Composition Root (`NexusBridge.mq5`).

```mql5
// NexusBridge.mq5
#include "Core/AppContext.mqh"
#include "Adapters/MT5TradeAdapter.mqh"
#include "Adapters/TcpTransportAdapter.mqh"
#include "Application/CommandHandler.mqh"

// Static allocation of concrete implementations
MT5TradeAdapter     g_TradeAdapter;
TcpTransportAdapter g_TcpAdapter;
CommandHandler      g_CommandHandler;

int OnInit()
{
   // Initialize and bind pointers to the global AppContext
   AppContext::TradeService   = &g_TradeAdapter;
   AppContext::Transport      = &g_TcpAdapter;
   
   if(!g_TcpAdapter.Connect())
   {
      Print("[NTE BRIDGE] [WARN] TCP transport failed initialization. Waiting for reconnect loop.");
   }

   Print("[NTE BRIDGE] Initialized under static compile-time composition.");
   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
   g_TcpAdapter.Disconnect();
}

void OnTick()
{
   // Handle real-time market data streaming and out-going events
}
```

This structural configuration maintains code testability. For backtesting or local offline validation, the composition file can simply bind a mock simulator adapter (e.g., `StrategySimulator`) to the `AppContext` references instead of the live adapters.

---

## Wire-Protocol Specifications

To interact with the C# client framework, NexusBridge parses incoming command envelopes and generates outbound message streams using a structured JSON layout over a raw TCP socket connection.

### 1. Inbound Command Frame (C# Engine to MT5)
```json
{
  "header": {
    "message_id": "9bc32d10-f823-11ef-93a0-12010a760002",
    "correlation_id": "402941-e23a",
    "timestamp": 1740003010,
    "token": "7a9f82dcd12f45ea8cde",
    "signature": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
  },
  "command": "PlaceOrder",
  "payload": {
    "symbol": "XAUUSD",
    "order_type": 0,
    "volume": 0.15,
    "price": 2353.40,
    "sl": 2340.00,
    "tp": 2375.00,
    "magic_number": 100204,
    "slippage": 30
  }
}
```

### 2. Outbound Telemetry Event Frame (MT5 to C# Engine)
```json
{
  "header": {
    "message_id": "a521da10-f823-11ef-93b1-12010a760012",
    "timestamp": 1740003011
  },
  "event_name": "TickEvent",
  "payload": {
    "symbol": "XAUUSD",
    "time": 1740003011,
    "bid": 2353.20,
    "ask": 2353.40,
    "last": 0.0,
    "volume_real": 0.0
  }
}
```

---

## System Resilience & Recovery Procedures

### 1. Zero-Trust State Recovery
The bridge assumes that the TCP connection state can drop out at any time. When a reconnection is established, the `RecoveryManager` synchronizes the local cache with the broker server's ground truth, resolving active execution tickets and updating the external C# engine.

```
                  Connection Interruption & Reconnection Flow
                  
 ┌──────────────────────┐                     ┌─────────────────────┐
 │  Connection Dropped  │                     │ Connection Restored │
 └──────────┬───────────┘                     └──────────┬──────────┘
            │                                            │
            ▼                                            ▼
 ┌──────────────────────┐                     ┌─────────────────────┐
 │ Freeze Outbound Tx   │                     │ Re-Query Active     │
 │ Queue                │                     │ Tickets from Broker │
 └──────────────────────┘                     └──────────┬──────────┘
                                                         │
                                                         ▼
 ┌──────────────────────┐                     ┌─────────────────────┐
 │ Cache Incoming Ticks │                     │ Rebuild Local Cache │
 │ locally              │                     │ State               │
 └──────────────────────┘                     └──────────┬──────────┘
                                                         │
                                                         ▼
                                              ┌─────────────────────┐
                                              │ Stream State Update │
                                              │ to C# Engine        │
                                              └─────────────────────┘
```

### 2. Input Integrity Checks
Prior to execution, all incoming commands undergo basic range and logic checks to intercept anomalous values (such as zero volume or stop levels placed on the wrong side of the current market spread) before they are passed to the broker's processing queue.
