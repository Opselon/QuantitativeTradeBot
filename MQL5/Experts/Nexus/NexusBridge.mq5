//+------------------------------------------------------------------+
//|                                                 NexusBridge.mq5  |
//|                                           Nexus Trading Engine   |
//|                                      https://nexus.example.com   |
//+------------------------------------------------------------------+
#property copyright "Nexus Trading Engine"
#property link      "https://nexus.example.com"
#property version   "5.1"
#property description "Nexus MT5 Bridge EA - Connects MT5 Terminal to C# REST Gateway."
#property strict

//--- Input parameters
input string   InpBridgeHost = "127.0.0.1";  // REST Gateway IP Address
input int      InpBridgePort = 8080;         // REST Gateway Port (Standard 8080)
input int      InpPollIntervalMs = 100;      // Polling Interval (ms)
input bool     InpStreamAllMarketWatch = true; // Stream every broker-selected Market Watch symbol
input int      InpTelemetryIntervalMs = 1000; // Account, positions, and multi-timeframe snapshot cadence

//--- Structures
struct SubscribedSymbolState
{
   string symbol;
   ulong last_time_msc;
   double last_bid;
   double last_ask;
};

//--- Global variables
bool     g_is_connected = false;             // Active connection flag
string   g_incoming_buffer = "";             // Buffer for stream assembly
datetime g_last_recon_time = 0;              // Last reconnect attempt timestamp
string   g_poll_url = "";                    // REST Polling URL
string   g_response_url = "";                // REST Response POST URL
string   g_tick_url = "";                    // REST Tick Ingestion URL
string   g_telemetry_url = "";               // REST account/position/candle ingestion URL
ulong    g_last_telemetry_msc = 0;           // last terminal intelligence publication

SubscribedSymbolState g_symbols_state[];     // dynamically-sized broker symbol subscription set
int      g_symbols_count = 0;                // subscribed symbols counter

//--- Function declarations
bool ConnectToBridge();
void DisconnectFromBridge();
void PollBridgeMessages();
void ProcessIncomingJson(const string json);
void HandleGetAccountSnapshot(const string requestId);
void HandlePing(const string requestId);
void HandleLogin(const string requestId, const string json);
void HandleSubscribeSymbol(const string requestId, const string json);
void HandleUnsubscribeSymbol(const string requestId, const string json);
void HandlePlaceOrder(const string requestId, const string json);
void HandleClosePosition(const string requestId, const string json);
void HandleGetOpenPositions(const string requestId);
void HandleGetAvailableSymbols(const string requestId);
void HandleUnknownCommand(const string requestId, const string command);
string BuildEnvelopeJson(const string messageType, const string requestId, const string command, const string payloadJson, const string errorJson);
void SendResponse(const string responseJson);
void SendJsonToUrl(const string url, const string json);
void StreamLiveTicks();
void StreamSymbolTick(const string symbol);
void StreamTerminalIntelligence();
void SubscribeMarketWatchSymbols();
string BuildMultiTimeframeJson(const string symbol);
string BuildPositionsJson();
string IsoTimestampFromMilliseconds(const ulong timestampMsc);

//--- Simple JSON helpers
string GetJsonStringValue(const string json, const string key);
double GetJsonDoubleValue(const string json, const string key);
long GetJsonIntValue(const string json, const string key);
string EscapeJsonString(const string text);
string StringTrim(const string text);

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   Print("================================================");
   Print("=== NEXUS HTTP REST BRIDGE STARTUP DIAGNOSTICS ===");
   Print("================================================");

   // Configure HTTP Endpoint Routing Targets
   g_poll_url     = "http://" + InpBridgeHost + ":" + IntegerToString(InpBridgePort) + "/api/v1/bridge/poll";
   g_response_url = "http://" + InpBridgeHost + ":" + IntegerToString(InpBridgePort) + "/api/v1/bridge/response";
   g_tick_url     = "http://" + InpBridgeHost + ":" + IntegerToString(InpBridgePort) + "/api/v1/bridge/tick";
   g_telemetry_url = "http://" + InpBridgeHost + ":" + IntegerToString(InpBridgePort) + "/api/v1/bridge/telemetry";

   Print("Polling Endpoint: ", g_poll_url);
   Print("Response Endpoint: ", g_response_url);
   Print("Tick Ingestion Endpoint: ", g_tick_url);
   Print("Terminal Intelligence Endpoint: ", g_telemetry_url);

   // State Setup
   g_symbols_count = 0;
   ArrayResize(g_symbols_state, 0);
   g_is_connected = false;
   if(InpStreamAllMarketWatch) SubscribeMarketWatchSymbols();

   // Test Initial HTTP Gateway Connectivity
   if(!ConnectToBridge())
   {
      Print("NexusBridge: Warning - Initial gateway check failed. Verify Kestrel server is running on port 8080.");
   }

   // Register Polling Timer
   if(!EventSetMillisecondTimer(InpPollIntervalMs))
   {
      Print("NexusBridge: Failed to start millisecond timer.");
      return(INIT_FAILED);
   }

   Print("NexusBridge: Initialization completed successfully.");
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   EventKillTimer();
   DisconnectFromBridge();
   Print("NexusBridge: Deinitialized.");
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // The chart symbol is published on every MT5 tick; timer polling only covers non-chart subscriptions.
   StreamSymbolTick(_Symbol);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   PollBridgeMessages();
   StreamLiveTicks();
   StreamTerminalIntelligence();
}
//+------------------------------------------------------------------+
//| Test REST Bridge Server Connectivity                              |
//+------------------------------------------------------------------+
bool ConnectToBridge()
{
   g_last_recon_time = TimeLocal();

   char data[];
   char result[];
   string headers;

   ResetLastError();
   
   // Request basic status check from Kestrel to test route
   int res = WebRequest("GET", "http://" + InpBridgeHost + ":" + IntegerToString(InpBridgePort) + "/api/v1/health", "", NULL, 2000, 
                        data, 0, result, headers);

   if(res == 200)
   {
      g_is_connected = true;
      Print("NexusBridge: Handshake connection established successfully over WebRequest.");
      return true;
   }
   
   int last_err = GetLastError();
   Print("NexusBridge: WebRequest connection failed. HTTP Status: ", res, ", Error Code: ", last_err);
   Print("NexusBridge: Ensure 'http://", InpBridgeHost, ":", InpBridgePort, "' is added to whitelisted WebRequest URLs.");
   
   g_is_connected = false;
   return false;
}

//+------------------------------------------------------------------+
//| Disconnect from bridge                                           |
//+------------------------------------------------------------------+
void DisconnectFromBridge()
{
   g_is_connected = false;
   Print("NexusBridge: Disconnected from bridge server.");
}

//+------------------------------------------------------------------+
//| Poll outstanding Command envelopes from C# Queue                 |
//+------------------------------------------------------------------+
void PollBridgeMessages()
{
   char data[];
   char result[];
   string headers;

   ResetLastError();

   int res = WebRequest("GET", g_poll_url, "", NULL, 1500, 
                        data, 0, result, headers);

   if(res == 200)
   {
      g_is_connected = true;
      string json = CharArrayToString(result);
      json = StringTrim(json);
      
      // Process payload if a valid command exists
      if(StringLen(json) > 0 && json != "NONE" && json != "{}")
      {
         // ADDED: Print the raw JSON received from C# to trace exactly what is arriving
         Print("NexusBridge Raw JSON: ", json);
         ProcessIncomingJson(json);
      }
   }
   else
   {
      g_is_connected = false;
   }
}
//+------------------------------------------------------------------+
//| Route envelope fields to dedicated business handlers             |
//+------------------------------------------------------------------+
void ProcessIncomingJson(const string json)
{
   string messageType = GetJsonStringValue(json, "messageType");
   string requestId   = GetJsonStringValue(json, "requestId");
   string command     = GetJsonStringValue(json, "command");

   if(messageType != "Request")
   {
      return;
   }

   Print("NexusBridge: Received Command: '", command, "' with Request ID: ", requestId);

   if(command == "GetAccountSnapshot")
   {
      HandleGetAccountSnapshot(requestId);
   }
   else if(command == "Ping")
   {
      HandlePing(requestId);
   }
   else if(command == "Login")
   {
      HandleLogin(requestId, json);
   }
   else if(command == "SubscribeSymbol")
   {
      HandleSubscribeSymbol(requestId, json);
   }
   else if(command == "UnsubscribeSymbol")
   {
      HandleUnsubscribeSymbol(requestId, json);
   }
   else if(command == "PlaceOrder")
   {
      HandlePlaceOrder(requestId, json);
   }
   else if(command == "ClosePosition")
   {
      HandleClosePosition(requestId, json);
   }
   else if(command == "GetOpenPositions")
   {
      HandleGetOpenPositions(requestId);
   }
   else if(command == "GetAvailableSymbols")
   {
      HandleGetAvailableSymbols(requestId);
   }
   else if(command == "ModifyPosition")
   {
      HandleModifyPosition(requestId, json);
   }
   else
   {
      HandleUnknownCommand(requestId, command);
   }
}

//+------------------------------------------------------------------+
//| Command: GetAccountSnapshot - retrieves MT5 details & responds  |
//+------------------------------------------------------------------+
void HandleGetAccountSnapshot(const string requestId)
{
   ResetLastError();

   long   accountId   = AccountInfoInteger(ACCOUNT_LOGIN);
   string broker      = AccountInfoString(ACCOUNT_COMPANY);
   string currency    = AccountInfoString(ACCOUNT_CURRENCY);
   double balance     = AccountInfoDouble(ACCOUNT_BALANCE);
   double equity      = AccountInfoDouble(ACCOUNT_EQUITY);
   double margin      = AccountInfoDouble(ACCOUNT_MARGIN);
   double freeMargin  = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   int    leverage    = (int)AccountInfoInteger(ACCOUNT_LEVERAGE);

   int last_err = GetLastError();
   string connectionHealth = "Healthy";
   string errorJson = "null";

   if(last_err != 0)
   {
      connectionHealth = "Degraded";
      errorJson = "{"
         "\"code\":\"MT5_API_ERROR\","
         "\"message\":\"MQL5 failed to fetch full stats. Last error: " + IntegerToString(last_err) + "\""
      "}";
   }

   string payload = "{"
      "\"accountId\":"        + IntegerToString(accountId) + ","
      "\"broker\":\""         + EscapeJsonString(broker)   + "\","
      "\"currency\":\""       + EscapeJsonString(currency) + "\","
      "\"balance\":"          + DoubleToString(balance, 2) + ","
      "\"equity\":"           + DoubleToString(equity, 2)  + ","
      "\"margin\":"           + DoubleToString(margin, 2)  + ","
      "\"freeMargin\":"       + DoubleToString(freeMargin, 2) + ","
      "\"leverage\":"         + IntegerToString(leverage)  + ","
      "\"connectionHealth\":\"" + connectionHealth        + "\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "GetAccountSnapshot", payload, errorJson);
   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Command: Ping - simple server tick heartbeat check               |
//+------------------------------------------------------------------+
void HandlePing(const string requestId)
{
   datetime terminal_time = TimeLocal();
   datetime server_time   = TimeCurrent();

   string payload = "{"
      "\"serverTime\":\""   + TimeToString(server_time, TIME_DATE|TIME_SECONDS) + "\","
      "\"terminalTime\":\"" + TimeToString(terminal_time, TIME_DATE|TIME_SECONDS) + "\","
      "\"status\":\"Alive\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "Ping", payload, "null");
   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Command: Login - validates requested account with terminal      |
//+------------------------------------------------------------------+
void HandleLogin(const string requestId, const string json)
{
   string accountId = GetJsonStringValue(json, "accountId");
   long current_login = AccountInfoInteger(ACCOUNT_LOGIN);

   bool success = true;
   string error_msg = "";

   if(accountId != "" && StringToInteger(accountId) != current_login)
   {
      success = false;
      error_msg = "Account login mismatch. Requested: " + accountId + ", active login: " + IntegerToString(current_login);
   }

   string payload = "{"
      "\"success\":" + (success ? "true" : "false") + ","
      "\"errorMessage\":\"" + EscapeJsonString(error_msg) + "\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "Login", payload, "null");
   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Command: SubscribeSymbol - add a symbol to streaming watch list  |
//+------------------------------------------------------------------+
void HandleSubscribeSymbol(const string requestId, const string json)
{
   string symbol = GetJsonStringValue(json, "symbol");
   string error_msg = "";
   bool success = true;

   if(symbol == "")
   {
      success = false;
      error_msg = "Symbol is required.";
   }
   else
   {
      if(!SymbolSelect(symbol, true))
      {
         success = false;
         error_msg = "Symbol " + symbol + " could not be selected in Market Watch.";
      }
      else
      {
         bool exists = false;
         for(int i = 0; i < g_symbols_count; i++)
         {
            if(g_symbols_state[i].symbol == symbol)
            {
               exists = true;
               break;
            }
         }

         if(!exists)
         {
            {
               ArrayResize(g_symbols_state, g_symbols_count + 1);
               g_symbols_state[g_symbols_count].symbol = symbol;
               g_symbols_state[g_symbols_count].last_time_msc = 0;
               g_symbols_state[g_symbols_count].last_bid = 0;
               g_symbols_state[g_symbols_count].last_ask = 0;
               g_symbols_count++;
               Print("NexusBridge: Subscribed to symbol '", symbol, "'. Current active count: ", g_symbols_count);
            }
         }
      }
   }

   string payload = "{"
      "\"success\":" + (success ? "true" : "false") + ","
      "\"errorMessage\":\"" + EscapeJsonString(error_msg) + "\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "SubscribeSymbol", payload, "null");
   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Command: UnsubscribeSymbol - remove symbol from stream watch    |
//+------------------------------------------------------------------+
void HandleUnsubscribeSymbol(const string requestId, const string json)
{
   string symbol = GetJsonStringValue(json, "symbol");
   bool success = false;
   string error_msg = "Symbol not subscribed.";

   if(symbol != "")
   {
      for(int i = 0; i < g_symbols_count; i++)
      {
         if(g_symbols_state[i].symbol == symbol)
         {
            for(int j = i; j < g_symbols_count - 1; j++)
            {
               g_symbols_state[j] = g_symbols_state[j + 1];
            }
            g_symbols_count--;
            ArrayResize(g_symbols_state, g_symbols_count);
            success = true;
            error_msg = "";
            Print("NexusBridge: Unsubscribed from symbol '", symbol, "'. Current active count: ", g_symbols_count);
            break;
         }
      }
   }

   string payload = "{"
      "\"success\":" + (success ? "true" : "false") + ","
      "\"errorMessage\":\"" + EscapeJsonString(error_msg) + "\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "UnsubscribeSymbol", payload, "null");
   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Command: PlaceOrder - submit market order trading command        |
//+------------------------------------------------------------------+
void HandlePlaceOrder(const string requestId, const string json)
{
   ResetLastError();

   string symbol = GetJsonStringValue(json, "symbol");
   string side_str = GetJsonStringValue(json, "side"); 
   double volume = GetJsonDoubleValue(json, "volume");
   
   // Accept both camelCase ("stopLoss") and PascalCase ("StopLoss") serialization patterns
   double stopLoss = GetJsonDoubleValue(json, "stopLoss");
   if(stopLoss <= 0.0) stopLoss = GetJsonDoubleValue(json, "StopLoss");

   double takeProfit = GetJsonDoubleValue(json, "takeProfit");
   if(takeProfit <= 0.0) takeProfit = GetJsonDoubleValue(json, "TakeProfit");

   string comment = GetJsonStringValue(json, "comment");
   string clientCorrelationId = GetJsonStringValue(json, "clientCorrelationId");

   Print("NexusBridge: PlaceOrder - Request ID: ", requestId,
         ", Symbol: ", symbol,
         ", Side: ", side_str,
         ", Volume: ", DoubleToString(volume, 2),
         ", SL (Pips): ", DoubleToString(stopLoss, 2),
         ", TP (Pips): ", DoubleToString(takeProfit, 2));

   if(symbol == "")
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Symbol parameter is required.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "PlaceOrder", "{}", errorJson));
      return;
   }

   if(!SymbolSelect(symbol, true))
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Symbol " + EscapeJsonString(symbol) + " is not available or cannot be selected.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "PlaceOrder", "{}", errorJson));
      return;
   }

   if(volume <= 0.0)
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Volume must be greater than zero.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "PlaceOrder", "{}", errorJson));
      return;
   }

   ENUM_ORDER_TYPE order_type;
   if(side_str == "Buy")
   {
      order_type = ORDER_TYPE_BUY;
   }
   else if(side_str == "Sell")
   {
      order_type = ORDER_TYPE_SELL;
   }
   else
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Side must be 'Buy' or 'Sell'.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "PlaceOrder", "{}", errorJson));
      return;
   }

   double vol_step = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);
   if(vol_step > 0)
   {
      volume = NormalizeDouble(MathRound(volume / vol_step) * vol_step, 2);
   }

   double min_volume = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double max_volume = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   if(volume < min_volume) volume = min_volume;
   if(volume > max_volume) volume = max_volume;

   double price = 0.0;
   MqlTick tick;
   if(SymbolInfoTick(symbol, tick))
   {
      price = (order_type == ORDER_TYPE_BUY) ? tick.ask : tick.bid;
   }
   else
   {
      price = (order_type == ORDER_TYPE_BUY) ? SymbolInfoDouble(symbol, SYMBOL_ASK) : SymbolInfoDouble(symbol, SYMBOL_BID);
   }

   // Calculate absolute stop prices using symbol-specific digits
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   double pip_size = 0.0001; // Default Forex Major
   
   if(digits == 3 || digits == 5)
   {
      pip_size = (digits == 3) ? 0.01 : 0.0001;
   }
   else if(digits == 2 || digits == 4)
   {
      pip_size = (digits == 2) ? 0.1 : 0.001;
   }
   else
   {
      pip_size = _Point;
   }

   double sl_normalized = 0.0;
   double tp_normalized = 0.0;

   if(stopLoss > 0.0)
   {
      if(order_type == ORDER_TYPE_BUY)
         sl_normalized = NormalizeDouble(price - (stopLoss * pip_size), digits);
       else
         sl_normalized = NormalizeDouble(price + (stopLoss * pip_size), digits);
   }

   if(takeProfit > 0.0)
   {
      if(order_type == ORDER_TYPE_BUY)
         tp_normalized = NormalizeDouble(price + (takeProfit * pip_size), digits);
       else
         tp_normalized = NormalizeDouble(price - (takeProfit * pip_size), digits);
   }

   uint filling_modes = (uint)SymbolInfoInteger(symbol, SYMBOL_FILLING_MODE);
   ENUM_ORDER_TYPE_FILLING filling = ORDER_FILLING_FOK;
   if((filling_modes & SYMBOL_FILLING_FOK) != 0)
   {
      filling = ORDER_FILLING_FOK;
   }
   else if((filling_modes & SYMBOL_FILLING_IOC) != 0)
   {
      filling = ORDER_FILLING_IOC;
   }
   else
   {
      filling = ORDER_FILLING_RETURN;
   }

   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action       = TRADE_ACTION_DEAL;
   request.symbol       = symbol;
   request.volume       = volume;
   request.type         = order_type;
   request.price        = price;
   request.deviation    = 10;
   request.sl           = sl_normalized;
   request.tp           = tp_normalized;
   request.comment      = comment;
   request.type_filling = filling;

   bool ok = OrderSend(request, result);
   int last_err = GetLastError();

   string payload = "";
   string errorJson = "null";

   if(ok && result.retcode == TRADE_RETCODE_DONE)
   {
      Print("NexusBridge: PlaceOrder - Succeeded. Ticket: ", result.order, ", Retcode: ", result.retcode, ", SL: ", DoubleToString(sl_normalized, digits), ", TP: ", DoubleToString(tp_normalized, digits));
      payload = "{"
         "\"success\":true,"
         "\"ticket\":" + IntegerToString(result.order) + ","
         "\"status\":\"Executed\","
         "\"brokerMessage\":\"Trade completed successfully.\","
         "\"comment\":\"" + EscapeJsonString(comment) + "\""
      "}";
   }
   else
   {
      string fail_msg = "OrderSend failed. Retcode: " + IntegerToString(result.retcode) + ", Error: " + IntegerToString(last_err);
      Print("NexusBridge: PlaceOrder - Failed. ", fail_msg);
      payload = "{"
         "\"success\":false,"
         "\"ticket\":0,"
         "\"status\":\"Failed\","
         "\"brokerMessage\":\"" + EscapeJsonString(fail_msg) + "\","
         "\"comment\":\"\""
      "}";
      errorJson = "{"
         "\"code\":\"TRADE_REJECTED\","
         "\"message\":\"" + EscapeJsonString(fail_msg) + "\""
      "}";
   }

   SendResponse(BuildEnvelopeJson("Response", requestId, "PlaceOrder", payload, errorJson));
}

//+------------------------------------------------------------------+
//| Command: ClosePosition - execute full close of active position  |
//+------------------------------------------------------------------+
void HandleClosePosition(const string requestId, const string json)
{
   ResetLastError();

   long ticket = GetJsonIntValue(json, "ticket");
   string symbol = GetJsonStringValue(json, "symbol");
   double volume = GetJsonDoubleValue(json, "volume");

   Print("NexusBridge: ClosePosition - Request ID: ", requestId,
         ", Ticket: ", ticket,
         ", Symbol: ", symbol,
         ", Volume: ", DoubleToString(volume, 2));

   if(ticket <= 0)
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Ticket must be greater than zero.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "ClosePosition", "{}", errorJson));
      return;
   }

   if(!PositionSelectByTicket(ticket))
   {
      string errorJson = "{\"code\":\"POSITION_NOT_FOUND\",\"message\":\"Position with ticket " + IntegerToString(ticket) + " not found.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "ClosePosition", "{}", errorJson));
      return;
   }

   string pos_symbol = PositionGetString(POSITION_SYMBOL);
   ENUM_POSITION_TYPE pos_type = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
   double pos_volume = PositionGetDouble(POSITION_VOLUME);

   if(volume <= 0.0 || volume > pos_volume)
   {
      volume = pos_volume;
   }
   else
   {
      double vol_step = SymbolInfoDouble(pos_symbol, SYMBOL_VOLUME_STEP);
      if(vol_step > 0)
      {
         volume = NormalizeDouble(MathRound(volume / vol_step) * vol_step, 2);
      }
   }

   uint filling_modes = (uint)SymbolInfoInteger(pos_symbol, SYMBOL_FILLING_MODE);
   ENUM_ORDER_TYPE_FILLING filling = ORDER_FILLING_FOK;
   if((filling_modes & SYMBOL_FILLING_FOK) != 0)
   {
      filling = ORDER_FILLING_FOK;
   }
   else if((filling_modes & SYMBOL_FILLING_IOC) != 0)
   {
      filling = ORDER_FILLING_IOC;
   }
   else
   {
      filling = ORDER_FILLING_RETURN;
   }

   ENUM_ORDER_TYPE order_type = (pos_type == POSITION_TYPE_BUY) ? ORDER_TYPE_SELL : ORDER_TYPE_BUY;

   double price = 0.0;
   MqlTick tick_data;
   if(SymbolInfoTick(pos_symbol, tick_data))
   {
      price = (order_type == ORDER_TYPE_BUY) ? tick_data.ask : tick_data.bid;
   }
   else
   {
      price = (order_type == ORDER_TYPE_BUY) ? SymbolInfoDouble(pos_symbol, SYMBOL_ASK) : SymbolInfoDouble(pos_symbol, SYMBOL_BID);
   }

   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action       = TRADE_ACTION_DEAL;
   request.position     = ticket;
   request.symbol       = pos_symbol;
   request.volume       = volume;
   request.type         = order_type;
   request.price        = price;
   request.deviation    = 10;
   request.type_filling = filling;

   bool ok = OrderSend(request, result);
   int last_err = GetLastError();

   string payload = "";
   string errorJson = "null";

   if(ok && result.retcode == TRADE_RETCODE_DONE)
   {
      Print("NexusBridge: ClosePosition - Succeeded. Ticket: ", ticket, ", Retcode: ", result.retcode);
      payload = "{"
         "\"success\":true,"
         "\"ticket\":" + IntegerToString(ticket) + ","
         "\"brokerMessage\":\"Position closed successfully.\""
      "}";
   }
   else
   {
      string fail_msg = "Close failed. Retcode: " + IntegerToString(result.retcode) + ", Error: " + IntegerToString(last_err);
      Print("NexusBridge: ClosePosition - Failed. ", fail_msg);
      payload = "{"
         "\"success\":false,"
         "\"ticket\":" + IntegerToString(ticket) + ","
         "\"brokerMessage\":\"" + EscapeJsonString(fail_msg) + "\""
      "}";
      errorJson = "{"
         "\"code\":\"TRADE_REJECTED\","
         "\"message\":\"" + EscapeJsonString(fail_msg) + "\""
      "}";
   }

   SendResponse(BuildEnvelopeJson("Response", requestId, "ClosePosition", payload, errorJson));
}

//+------------------------------------------------------------------+
//| Command: GetOpenPositions - download active position snap       |
//+------------------------------------------------------------------+
void HandleGetOpenPositions(const string requestId)
{
   ResetLastError();

   string positionsJson = "";
   int total = PositionsTotal();

   Print("NexusBridge: GetOpenPositions - Request ID: ", requestId, ", Total positions found: ", total);

   for(int i = 0; i < total; i++)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket > 0)
      {
         string symbol = PositionGetString(POSITION_SYMBOL);
         ENUM_POSITION_TYPE type = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
         double volume = PositionGetDouble(POSITION_VOLUME);
         double open_price = PositionGetDouble(POSITION_PRICE_OPEN);
         double current_price = PositionGetDouble(POSITION_PRICE_CURRENT);
         double sl = PositionGetDouble(POSITION_SL);
         double tp = PositionGetDouble(POSITION_TP);
         double profit = PositionGetDouble(POSITION_PROFIT);
         double swap = PositionGetDouble(POSITION_SWAP);
         long magic = PositionGetInteger(POSITION_MAGIC);
         string comment = PositionGetString(POSITION_COMMENT);
         datetime open_time = (datetime)PositionGetInteger(POSITION_TIME);

         string side_str = (type == POSITION_TYPE_BUY) ? "Buy" : "Sell";

         MqlDateTime mqlTime;
         TimeToStruct(open_time, mqlTime);
         string time_str = StringFormat("%04d-%02d-%02dT%02d:%02d:%02dZ", mqlTime.year, mqlTime.mon, mqlTime.day, mqlTime.hour, mqlTime.min, mqlTime.sec);

         string pos_item = "{"
            "\"ticket\":"        + IntegerToString(ticket) + ","
            "\"symbol\":\""       + EscapeJsonString(symbol) + "\","
            "\"side\":\""         + side_str + "\","
            "\"volume\":"         + DoubleToString(volume, 2) + ","
            "\"openPrice\":"      + DoubleToString(open_price, 5) + ","
            "\"currentPrice\":"   + DoubleToString(current_price, 5) + ","
            "\"stopLoss\":"       + DoubleToString(sl, 5) + ","
            "\"takeProfit\":"     + DoubleToString(tp, 5) + ","
            "\"profit\":"         + DoubleToString(profit, 2) + ","
            "\"swap\":"           + DoubleToString(swap, 2) + ","
            "\"magicNumber\":"    + IntegerToString(magic) + ","
            "\"comment\":\""      + EscapeJsonString(comment) + "\","
            "\"openTime\":\""     + time_str + "\""
         "}";

         if(positionsJson != "") positionsJson += ",";
         positionsJson += pos_item;
      }
   }

   string payload = "{\"positions\":[" + positionsJson + "]}";
   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, "GetOpenPositions", payload, "null");

   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Fallback when command is unrecognized or unsupported             |
//+------------------------------------------------------------------+
void HandleUnknownCommand(const string requestId, const string command)
{
   string errorJson = "{"
      "\"code\":\"UNSUPPORTED_COMMAND\","
      "\"message\":\"The command '" + EscapeJsonString(command) + "' is unrecognized by the NexusBridge EA.\""
   "}";

   string responseEnvelopeJson = BuildEnvelopeJson("Response", requestId, command, "{}", errorJson);

   SendResponse(responseEnvelopeJson);
}

//+------------------------------------------------------------------+
//| Serialize the envelope to exact JSON text                       |
//+------------------------------------------------------------------+
string BuildEnvelopeJson(const string messageType, const string requestId, const string command, const string payloadJson, const string errorJson)
{
   string json = "{"
      "\"messageType\":\"" + messageType + "\","
      "\"requestId\":\""   + requestId   + "\","
      "\"command\":\""     + command     + "\","
      "\"payload\":"       + payloadJson + ","
      "\"error\":"         + errorJson   + ","
      "\"version\":\"1.0\""
   "}";

   return json;
}

//+------------------------------------------------------------------+
//| Transmit serialized message over WebRequest REST Endpoint         |
//+------------------------------------------------------------------+
void SendResponse(const string responseJson)
{
   char data[];
   int bytes_converted = StringToCharArray(responseJson, data, 0, WHOLE_ARRAY, CP_UTF8);
   
   // Trim the null-terminator byte added by StringToCharArray
   if(bytes_converted > 0)
   {
      ArrayResize(data, bytes_converted - 1);
   }

   char result[];
   string headers = "Content-Type: application/json\r\n";
   
   ResetLastError();
   
   // Deliver execution response to Kestrel REST Ingestion API on port 8080
   int res = WebRequest("POST", g_response_url, headers, NULL, 3000, 
                        data, 0, result, headers);
                        
   if(res == -1)
   {
      Print("NexusBridge: WebRequest SendResponse failed. Error Code: ", GetLastError());
   }
}

//+------------------------------------------------------------------+
//| Loop active subscriptions and stream changed ticks back to C#   |
//+------------------------------------------------------------------+
void StreamLiveTicks()
{
   if(!g_is_connected) return;
   for(int i = 0; i < g_symbols_count; i++)
      StreamSymbolTick(g_symbols_state[i].symbol);
}

//+------------------------------------------------------------------+
//| Publish an actual broker tick exactly once, including msec time  |
//+------------------------------------------------------------------+
void StreamSymbolTick(const string symbol)
{
   if(!g_is_connected || symbol == "") return;

   MqlTick tick;
   if(!SymbolInfoTick(symbol, tick)) return;

   int stateIndex = -1;
   for(int i = 0; i < g_symbols_count; i++)
   {
      if(g_symbols_state[i].symbol == symbol) { stateIndex = i; break; }
   }
   if(stateIndex >= 0 && tick.time_msc == g_symbols_state[stateIndex].last_time_msc &&
      tick.bid == g_symbols_state[stateIndex].last_bid && tick.ask == g_symbols_state[stateIndex].last_ask)
      return;

   if(stateIndex >= 0)
   {
      g_symbols_state[stateIndex].last_time_msc = tick.time_msc;
      g_symbols_state[stateIndex].last_bid = tick.bid;
      g_symbols_state[stateIndex].last_ask = tick.ask;
   }

   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   double volume = tick.volume_real > 0.0 ? tick.volume_real : (double)tick.volume;
   string payload = "{"
      "\"symbol\":\"" + EscapeJsonString(symbol) + "\","
      "\"timestamp\":\"" + IsoTimestampFromMilliseconds(tick.time_msc) + "\","
      "\"timestampMsc\":" + IntegerToString((long)tick.time_msc) + ","
      "\"bid\":" + DoubleToString(tick.bid, digits) + ","
      "\"ask\":" + DoubleToString(tick.ask, digits) + ","
      "\"spread\":" + DoubleToString(tick.ask - tick.bid, digits) + ","
      "\"volume\":" + DoubleToString(volume, 2) + "}";

   string envelopeJson = BuildEnvelopeJson("Request", "tick-" + symbol + "-" + IntegerToString((long)tick.time_msc), "ReceiveTickStream", payload, "null");
   SendJsonToUrl(g_tick_url, envelopeJson);
}

//+------------------------------------------------------------------+
//| Publish real account, positions, and M1..D1 candle intelligence |
//+------------------------------------------------------------------+
void StreamTerminalIntelligence()
{
   if(!g_is_connected || g_symbols_count == 0) return;
   ulong nowMsc = GetTickCount64();
   if(g_last_telemetry_msc != 0 && nowMsc - g_last_telemetry_msc < (ulong)InpTelemetryIntervalMs) return;
   g_last_telemetry_msc = nowMsc;

   double balance = AccountInfoDouble(ACCOUNT_BALANCE);
   double equity = AccountInfoDouble(ACCOUNT_EQUITY);
   double drawdown = balance > 0.0 ? MathMax(0.0, (balance - equity) / balance) : 0.0;
   string payload = "{"
      "\"timestamp\":\"" + TimeToString(TimeGMT(), TIME_DATE|TIME_SECONDS) + "Z\","
      "\"account\":{"
         "\"login\":" + IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN)) + ","
         "\"balance\":" + DoubleToString(balance, 2) + ","
         "\"equity\":" + DoubleToString(equity, 2) + ","
         "\"margin\":" + DoubleToString(AccountInfoDouble(ACCOUNT_MARGIN), 2) + ","
         "\"freeMargin\":" + DoubleToString(AccountInfoDouble(ACCOUNT_MARGIN_FREE), 2) + ","
         "\"leverage\":" + IntegerToString(AccountInfoInteger(ACCOUNT_LEVERAGE)) + ","
         "\"currency\":\"" + EscapeJsonString(AccountInfoString(ACCOUNT_CURRENCY)) + "\","
         "\"drawdown\":" + DoubleToString(drawdown, 8) + "},"
      "\"positions\":" + BuildPositionsJson() + ","
      "\"markets\":[";
   for(int i = 0; i < g_symbols_count; i++)
   {
      if(i > 0) payload += ",";
      payload += "{\"symbol\":\"" + EscapeJsonString(g_symbols_state[i].symbol) + "\",\"timeframes\":" + BuildMultiTimeframeJson(g_symbols_state[i].symbol) + "}";
   }
   payload += "]}";
   SendJsonToUrl(g_telemetry_url, BuildEnvelopeJson("Request", "telemetry-" + IntegerToString((long)nowMsc), "ReceiveTerminalIntelligence", payload, "null"));
}

string BuildPositionsJson()
{
   string output = "[";
   for(int i = 0; i < PositionsTotal(); i++)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket == 0) continue;
      if(StringLen(output) > 1) output += ",";
      output += "{\"ticket\":" + IntegerToString(ticket) + ",\"symbol\":\"" + EscapeJsonString(PositionGetString(POSITION_SYMBOL)) +
         "\",\"type\":\"" + ((PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY) ? "Buy" : "Sell") +
         "\",\"volume\":" + DoubleToString(PositionGetDouble(POSITION_VOLUME), 2) +
         ",\"entry\":" + DoubleToString(PositionGetDouble(POSITION_PRICE_OPEN), 8) +
         ",\"current\":" + DoubleToString(PositionGetDouble(POSITION_PRICE_CURRENT), 8) +
         ",\"stopLoss\":" + DoubleToString(PositionGetDouble(POSITION_SL), 8) +
         ",\"takeProfit\":" + DoubleToString(PositionGetDouble(POSITION_TP), 8) +
         ",\"profit\":" + DoubleToString(PositionGetDouble(POSITION_PROFIT), 2) +
         ",\"swap\":" + DoubleToString(PositionGetDouble(POSITION_SWAP), 2) + "}";
   }
   return output + "]";
}

string BuildMultiTimeframeJson(const string symbol)
{
   ENUM_TIMEFRAMES frames[7] = { PERIOD_M1, PERIOD_M5, PERIOD_M15, PERIOD_M30, PERIOD_H1, PERIOD_H4, PERIOD_D1 };
   string names[7] = { "M1", "M5", "M15", "M30", "H1", "H4", "D1" };
   string output = "[";
   for(int i = 0; i < 7; i++)
   {
      MqlRates rates[];
      ArraySetAsSeries(rates, true);
      if(CopyRates(symbol, frames[i], 0, 2, rates) < 1) continue;
      double atrBuffer[];
      ArraySetAsSeries(atrBuffer, true);
      int atrHandle = iATR(symbol, frames[i], 14);
      double atr = 0.0, ema = 0.0, rsi = 0.0;
      if(atrHandle != INVALID_HANDLE) { if(CopyBuffer(atrHandle, 0, 0, 1, atrBuffer) > 0) atr = atrBuffer[0]; IndicatorRelease(atrHandle); }
      double indicatorBuffer[]; ArraySetAsSeries(indicatorBuffer, true);
      int emaHandle = iMA(symbol, frames[i], 20, 0, MODE_EMA, PRICE_CLOSE);
      if(emaHandle != INVALID_HANDLE) { if(CopyBuffer(emaHandle, 0, 0, 1, indicatorBuffer) > 0) ema = indicatorBuffer[0]; IndicatorRelease(emaHandle); }
      int rsiHandle = iRSI(symbol, frames[i], 14, PRICE_CLOSE);
      if(rsiHandle != INVALID_HANDLE) { if(CopyBuffer(rsiHandle, 0, 0, 1, indicatorBuffer) > 0) rsi = indicatorBuffer[0]; IndicatorRelease(rsiHandle); }
      string trend = rates[0].close > ema ? "Bullish" : (rates[0].close < ema ? "Bearish" : "Neutral");
      if(StringLen(output) > 1) output += ",";
      output += "{\"timeframe\":\"" + names[i] + "\",\"timestamp\":\"" + TimeToString(rates[0].time, TIME_DATE|TIME_SECONDS) + "Z\",\"open\":" + DoubleToString(rates[0].open, 8) + ",\"high\":" + DoubleToString(rates[0].high, 8) + ",\"low\":" + DoubleToString(rates[0].low, 8) + ",\"close\":" + DoubleToString(rates[0].close, 8) + ",\"volume\":" + IntegerToString((long)rates[0].tick_volume) + ",\"atr\":" + DoubleToString(atr, 8) + ",\"range\":" + DoubleToString(rates[0].high-rates[0].low, 8) + ",\"body\":" + DoubleToString(rates[0].close-rates[0].open, 8) + "}";
   }
   return output + "]";
}

void SubscribeMarketWatchSymbols()
{
   int total = SymbolsTotal(true);
   for(int i = 0; i < total; i++)
   {
      string symbol = SymbolName(i, true);
      if(symbol == "") continue;
      ArrayResize(g_symbols_state, g_symbols_count + 1);
      g_symbols_state[g_symbols_count].symbol = symbol;
      g_symbols_state[g_symbols_count].last_time_msc = 0;
      g_symbols_state[g_symbols_count].last_bid = 0.0;
      g_symbols_state[g_symbols_count].last_ask = 0.0;
      g_symbols_count++;
   }
}

void SendJsonToUrl(const string url, const string json)
{
   char data[]; int count = StringToCharArray(json, data, 0, WHOLE_ARRAY, CP_UTF8); if(count > 0) ArrayResize(data, count - 1);
   char result[]; string headers = "Content-Type: application/json\r\n";
   ResetLastError();
   int response = WebRequest("POST", url, headers, NULL, 2000, data, 0, result, headers);
   if(response < 200 || response >= 300) Print("NexusBridge: telemetry POST failed: ", response, ", error=", GetLastError());
}

string IsoTimestampFromMilliseconds(const ulong timestampMsc)
{
   datetime seconds = (datetime)(timestampMsc / 1000);
   int milliseconds = (int)(timestampMsc % 1000);
   MqlDateTime value; TimeToStruct(seconds, value);
   return StringFormat("%04d-%02d-%02dT%02d:%02d:%02d.%03dZ", value.year, value.mon, value.day, value.hour, value.min, value.sec, milliseconds);
}

//+------------------------------------------------------------------+
//| Helper: Extract string value of a JSON key (simple parser)       |
//+------------------------------------------------------------------+
string GetJsonStringValue(const string json, const string key)
{
   string search_key = "\"" + key + "\"";
   int key_pos = StringFind(json, search_key);
   if(key_pos < 0) return "";

   int colon_pos = StringFind(json, ":", key_pos + StringLen(search_key));
   if(colon_pos < 0) return "";

   int quote_start = StringFind(json, "\"", colon_pos + 1);
   if(quote_start < 0) return "";

   int quote_end = StringFind(json, "\"", quote_start + 1);
   if(quote_end < 0) return "";

   return StringSubstr(json, quote_start + 1, quote_end - quote_start - 1);
}

//+------------------------------------------------------------------+
//| Helper: Extract double value of a JSON key                       |
//+------------------------------------------------------------------+
double GetJsonDoubleValue(const string json, const string key)
{
   string search_key = "\"" + key + "\"";
   int key_pos = StringFind(json, search_key);
   if(key_pos < 0) return 0.0;

   int colon_pos = StringFind(json, ":", key_pos + StringLen(search_key));
   if(colon_pos < 0) return 0.0;

   int end_pos = colon_pos + 1;
   while(end_pos < StringLen(json))
   {
      ushort ch = StringGetCharacter(json, end_pos);
      if(ch == ',' || ch == '}' || ch == ']' || ch == '\r' || ch == '\n' || ch == ' ')
      {
         break;
      }
      end_pos++;
   }

   string val_str = StringSubstr(json, colon_pos + 1, end_pos - colon_pos - 1);
   val_str = StringTrim(val_str);

   if(StringLen(val_str) > 0 && StringGetCharacter(val_str, 0) == '"')
   {
      val_str = StringSubstr(val_str, 1, StringLen(val_str) - 2);
   }

   return StringToDouble(val_str);
}

//+------------------------------------------------------------------+
//| Helper: Extract integer value of a JSON key                      |
//+------------------------------------------------------------------+
long GetJsonIntValue(const string json, const string key)
{
   string search_key = "\"" + key + "\"";
   int key_pos = StringFind(json, search_key);
   if(key_pos < 0) return 0;

   int colon_pos = StringFind(json, ":", key_pos + StringLen(search_key));
   if(colon_pos < 0) return 0;

   int end_pos = colon_pos + 1;
   while(end_pos < StringLen(json))
   {
      ushort ch = StringGetCharacter(json, end_pos);
      if(ch == ',' || ch == '}' || ch == ']' || ch == '\r' || ch == '\n' || ch == ' ')
      {
         break;
      }
      end_pos++;
   }

   string val_str = StringSubstr(json, colon_pos + 1, end_pos - colon_pos - 1);
   val_str = StringTrim(val_str);

   if(StringLen(val_str) > 0 && StringGetCharacter(val_str, 0) == '"')
   {
      val_str = StringSubstr(val_str, 1, StringLen(val_str) - 2);
   }

   return StringToInteger(val_str);
}

//+------------------------------------------------------------------+
//| Helper: Escape characters for safe JSON serialization            |
//+------------------------------------------------------------------+
string EscapeJsonString(const string text)
{
   string escaped = text;
   StringReplace(escaped, "\\", "\\\\");
   StringReplace(escaped, "\"", "\\\"");
   StringReplace(escaped, "\n", "\\n");
   StringReplace(escaped, "\r", "\\r");
   StringReplace(escaped, "\t", "\\t");
   return escaped;
}

//+------------------------------------------------------------------+
//| Helper: Trim whitespace from both ends of a string               |
//+------------------------------------------------------------------+
string StringTrim(const string text)
{
   string result = text;
   while(StringLen(result) > 0 && (StringGetCharacter(result, 0) == ' ' || StringGetCharacter(result, 0) == '\t' || StringGetCharacter(result, 0) == '\r' || StringGetCharacter(result, 0) == '\n'))
   {
      result = StringSubstr(result, 1);
   }
   while(StringLen(result) > 0 && (StringGetCharacter(result, StringLen(result) - 1) == ' ' || StringGetCharacter(result, StringLen(result) - 1) == '\t' || StringGetCharacter(result, StringLen(result) - 1) == '\r' || StringGetCharacter(result, StringLen(result) - 1) == '\n'))
   {
      result = StringSubstr(result, 0, StringLen(result) - 1);
   }
   return result;
}
//+------------------------------------------------------------------+
//| Command: ModifyPosition - passive broker-side SL/TP update      |
//+------------------------------------------------------------------+
void HandleModifyPosition(const string requestId, const string json)
{
   ResetLastError();

   long ticket = GetJsonIntValue(json, "ticket");
   string symbol = GetJsonStringValue(json, "symbol");
   
   double sl = GetJsonDoubleValue(json, "sl");
   if(sl <= 0.0) sl = GetJsonDoubleValue(json, "StopLoss");

   double tp = GetJsonDoubleValue(json, "tp");
   if(tp <= 0.0) tp = GetJsonDoubleValue(json, "TakeProfit");

   Print("NexusBridge: ModifyPosition - Request ID: ", requestId,
         ", Ticket: ", ticket,
         ", Symbol: ", symbol,
         ", SL Target: ", DoubleToString(sl, 5),
         ", TP Target: ", DoubleToString(tp, 5));

   if(ticket <= 0)
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Ticket must be greater than zero.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "ModifyPosition", "{}", errorJson));
      return;
   }

   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action   = TRADE_ACTION_SLTP;
   request.position = ticket;
   request.symbol   = symbol;
   request.sl       = sl;
   request.tp       = tp;

   bool ok = OrderSend(request, result);
   int last_err = GetLastError();

   string payload = "";
   string errorJson = "null";

   if(ok && result.retcode == TRADE_RETCODE_DONE)
   {
      Print("NexusBridge: ModifyPosition - Succeeded for ticket: ", ticket);
      payload = "{"
         "\"success\":true,"
         "\"ticket\":" + IntegerToString(ticket) + ","
         "\"brokerMessage\":\"Position modified successfully.\""
      "}";
   }
   else
   {
      string fail_msg = "Modification failed. Retcode: " + IntegerToString(result.retcode) + ", Error: " + IntegerToString(last_err);
      Print("NexusBridge: ModifyPosition - Failed: ", fail_msg);
      payload = "{"
         "\"success\":false,"
         "\"ticket\":" + IntegerToString(ticket) + ","
         "\"brokerMessage\":\"" + EscapeJsonString(fail_msg) + "\""
      "}";
      errorJson = "{"
         "\"code\":\"TRADE_REJECTED\","
         "\"message\":\"" + EscapeJsonString(fail_msg) + "\""
      "}";
   }

   SendResponse(BuildEnvelopeJson("Response", requestId, "ModifyPosition", payload, errorJson));
}

//+------------------------------------------------------------------+
//| Command: GetAvailableSymbols - retrieves all Market Watch symbols|
//+------------------------------------------------------------------+
void HandleGetAvailableSymbols(const string requestId)
{
   ResetLastError();

   string symbolsJson = "";
   int total = SymbolsTotal(true); // true = only symbols selected in Market Watch

   Print("NexusBridge: GetAvailableSymbols - Request ID: ", requestId, ", Total symbols in Market Watch: ", total);

   for(int i = 0; i < total; i++)
   {
      string name = SymbolName(i, true);
      if(name != "")
      {
         string item = "\"" + EscapeJsonString(name) + "\"";
         if(symbolsJson != "") symbolsJson += ",";
         symbolsJson += item;
      }
   }

   string payload = "{\"symbols\":[" + symbolsJson + "]}";
   string responseJson = BuildEnvelopeJson("Response", requestId, "GetAvailableSymbols", payload, "null");
   SendResponse(responseJson);
}