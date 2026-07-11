//+------------------------------------------------------------------+
//|                                                 NexusBridge.mq5  |
//|                                           Nexus Trading Engine   |
//|                                      https://nexus.example.com   |
//+------------------------------------------------------------------+
#property copyright "Nexus Trading Engine"
#property link      "https://nexus.example.com"
#property version   "1.00"
#property description "Nexus MT5 Bridge EA - Connects MT5 Terminal to C# Nexus Brain."
#property strict

//--- input parameters
input string   InpBridgeHost = "127.0.0.1";  // Bridge Host IP Address
input int      InpBridgePort = 5000;         // Bridge Port
input int      InpPollIntervalMs = 250;      // Message Polling Interval (ms)

//--- global variables
int      g_socket = INVALID_HANDLE;          // Direct network socket handle
bool     g_is_connected = false;             // Active connection flag
string   g_incoming_buffer = "";             // Buffer for stream assembly
datetime g_last_recon_time = 0;              // Last reconnect attempt timestamp

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   Print("NexusBridge: Initializing Expert Advisor...");

   // Attempt to open initial bridge socket
   if(!ConnectToBridge())
   {
      Print("NexusBridge: Warning - Initial bridge connection failed. Will retry periodically in timer loop.");
   }

   // Initialize high-precision polling timer
   EventSetMillisecondTimer(InpPollIntervalMs);

   Print("NexusBridge: Initialization completed successfully.");
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   // Terminate the polling timer
   EventKillTimer();

   // Cleanly close connection resources
   DisconnectFromBridge();

   Print("NexusBridge: Deinitialized. Reason code: ", reason);
}

//+------------------------------------------------------------------+
//| Expert tick function (optional for bridge, handled by Timer)    |
//+------------------------------------------------------------------+
void OnTick()
{
   // Empty - we drive message consumption in high-frequency timer loop
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   // Handle automatic reconnection if offline
   if(!g_is_connected)
   {
      datetime now = TimeCurrent();
      if(now - g_last_recon_time >= 5) // retry every 5 seconds
      {
         Print("NexusBridge: Connection offline. Attempting automatic reconnect...");
         ConnectToBridge();
      }
      return;
   }

   // Consume stream and parse discrete newline-terminated lines
   PollBridgeMessages();
}

//+------------------------------------------------------------------+
//| Establish TCP Socket connection to C# Server                     |
//+------------------------------------------------------------------+
bool ConnectToBridge()
{
   g_last_recon_time = TimeCurrent();

   // Create socket using MQL5 Native Network API
   g_socket = SocketCreate();
   if(g_socket == INVALID_HANDLE)
   {
      Print("NexusBridge: SocketCreate failed. Error code: ", GetLastError());
      return false;
   }

   // Connect to the C# Host/Port
   if(!SocketConnect(g_socket, InpBridgeHost, InpBridgePort, 3000))
   {
      Print("NexusBridge: SocketConnect to ", InpBridgeHost, ":", InpBridgePort, " failed. Error code: ", GetLastError());
      SocketClose(g_socket);
      g_socket = INVALID_HANDLE;
      return false;
   }

   g_is_connected = true;
   g_incoming_buffer = "";
   Print("NexusBridge: Successfully connected to Nexus Bridge Server at ", InpBridgeHost, ":", InpBridgePort);
   return true;
}

//+------------------------------------------------------------------+
//| Gracefully terminate the socket connection                       |
//+------------------------------------------------------------------+
void DisconnectFromBridge()
{
   if(g_socket != INVALID_HANDLE)
   {
      SocketClose(g_socket);
      g_socket = INVALID_HANDLE;
   }
   g_is_connected = false;
   Print("NexusBridge: Disconnected from bridge server.");
}

//+------------------------------------------------------------------+
//| Read incoming stream chunk, accumulate, and extract full JSONs   |
//+------------------------------------------------------------------+
void PollBridgeMessages()
{
   if(g_socket == INVALID_HANDLE) return;

   uint data_ready = 0;
   // Check if data is pending in socket buffer
   if(!SocketIsReadable(g_socket, data_ready))
   {
      int err = GetLastError();
      if(err != 0)
      {
         Print("NexusBridge: Socket error checking readability. Code: ", err);
         DisconnectFromBridge();
      }
      return;
   }

   if(data_ready == 0) return;

   uchar buffer[];
   ArrayResize(buffer, data_ready);

   int bytes_read = SocketRead(g_socket, buffer, data_ready, 100);
   if(bytes_read <= 0)
   {
      Print("NexusBridge: Socket read returned empty or error. Disconnecting.");
      DisconnectFromBridge();
      return;
   }

   // Convert array to string and append to workspace buffer
   string data_str = CharArrayToString(buffer, 0, bytes_read, CP_UTF8);
   g_incoming_buffer += data_str;

   // Process newline-separated message packets
   int newline_idx;
   while((newline_idx = StringFind(g_incoming_buffer, "\n")) >= 0)
   {
      string single_message = StringSubstr(g_incoming_buffer, 0, newline_idx);
      g_incoming_buffer = StringSubstr(g_incoming_buffer, newline_idx + 1);

      single_message = StringTrim(single_message);
      if(StringLen(single_message) > 0)
      {
         ProcessIncomingJson(single_message);
      }
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
      // Ignore or log non-request envelope flows
      return;
   }

   Print("NexusBridge: Received command '", command, "' with Request ID: ", requestId);

   if(command == "GetAccountSnapshot")
   {
      HandleGetAccountSnapshot(requestId);
   }
   else if(command == "Ping")
   {
      HandlePing(requestId);
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
   double freeMargin  = AccountInfoDouble(ACCOUNT_FREEMARGIN);
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

   // Format the JSON payload matching the C# contract GetAccountSnapshotResponse fields
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

   // Build the outer envelope matching the C# contract BridgeMessageEnvelope fields
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
//| Command: PlaceOrder - submit market order trading command        |
//+------------------------------------------------------------------+
void HandlePlaceOrder(const string requestId, const string json)
{
   ResetLastError();

   string symbol = GetJsonStringValue(json, "symbol");
   string side_str = GetJsonStringValue(json, "side"); // "Buy" or "Sell"
   double volume = GetJsonDoubleValue(json, "volume");
   double stopLoss = GetJsonDoubleValue(json, "stopLoss");
   double takeProfit = GetJsonDoubleValue(json, "takeProfit");
   string comment = GetJsonStringValue(json, "comment");

   // Validations
   if(symbol == "")
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Symbol parameter is required.\"}";
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

   // Prepare trade request
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action       = TRADE_ACTION_DEAL;
   request.symbol       = symbol;
   request.volume       = volume;
   request.type         = order_type;
   request.price        = (order_type == ORDER_TYPE_BUY) ? SymbolInfoDouble(symbol, SYMBOL_ASK) : SymbolInfoDouble(symbol, SYMBOL_BID);
   request.deviation    = 10;
   request.sl           = stopLoss;
   request.tp           = takeProfit;
   request.comment      = comment;
   request.type_filling = ORDER_FILLING_FOK;

   // Send order
   bool ok = OrderSend(request, result);
   int last_err = GetLastError();

   string payload = "";
   string errorJson = "null";

   if(ok && result.retcode == TRADE_RETCODE_DONE)
   {
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

   if(ticket <= 0)
   {
      string errorJson = "{\"code\":\"INVALID_PAYLOAD\",\"message\":\"Ticket must be greater than zero.\"}";
      SendResponse(BuildEnvelopeJson("Response", requestId, "ClosePosition", "{}", errorJson));
      return;
   }

   // Validate position exists
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
      volume = pos_volume; // Default to full close
   }

   // Prepare close deal order
   MqlTradeRequest request;
   MqlTradeResult result;
   ZeroMemory(request);
   ZeroMemory(result);

   request.action       = TRADE_ACTION_DEAL;
   request.position     = ticket;
   request.symbol       = pos_symbol;
   request.volume       = volume;
   request.type         = (pos_type == POSITION_TYPE_BUY) ? ORDER_TYPE_SELL : ORDER_TYPE_BUY;
   request.price        = (request.type == ORDER_TYPE_BUY) ? SymbolInfoDouble(pos_symbol, SYMBOL_ASK) : SymbolInfoDouble(pos_symbol, SYMBOL_BID);
   request.deviation    = 10;
   request.type_filling = ORDER_FILLING_FOK;

   bool ok = OrderSend(request, result);
   int last_err = GetLastError();

   string payload = "";
   string errorJson = "null";

   if(ok && result.retcode == TRADE_RETCODE_DONE)
   {
      payload = "{"
         "\"success\":true,"
         "\"ticket\":" + IntegerToString(ticket) + ","
         "\"brokerMessage\":\"Position closed successfully.\""
      "}";
   }
   else
   {
      string fail_msg = "Close failed. Retcode: " + IntegerToString(result.retcode) + ", Error: " + IntegerToString(last_err);
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
//| Transmit serialized message over socket with trailing newline    |
//+------------------------------------------------------------------+
void SendResponse(const string responseJson)
{
   if(g_socket == INVALID_HANDLE) return;

   // Append the protocol standard line delimiter
   string outbound_data = responseJson + "\n";

   uchar buffer[];
   int bytes_converted = StringToCharArray(outbound_data, buffer, 0, WHOLE_ARRAY, CP_UTF8);

   // We skip the null-terminator byte added by StringToCharArray (bytes_converted - 1)
   int bytes_sent = SocketWrite(g_socket, buffer, bytes_converted - 1);
   if(bytes_sent < 0)
   {
      Print("NexusBridge: SocketWrite failed. Error code: ", GetLastError());
      DisconnectFromBridge();
   }
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
