using Microsoft.Extensions.Logging;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexus.Infrastructure.Mt5Bridge
{
    public class Mt5BridgeService : IMt5BridgeService, IDisposable
    {
        private readonly IMt5BridgeClient _bridgeClient;
        private readonly ILogger<Mt5BridgeService> _logger;
        private readonly List<string> _subscribedSymbols = new();
        private readonly object _lock = new();

        private CancellationTokenSource? _loopCts;
        private Task? _telemetryTask;

        private bool _isConnected;
        private bool _isAuthenticated;
        private double _pingLatencyMs;
        private DateTime _lastHeartbeatUtc = DateTime.MinValue;
        private string _lastErrorMessage = string.Empty;

        // EA status & file properties
        public bool IsEaPresentInRepository { get; private set; }
        public long EaRepositoryFileSize { get; private set; }
        public DateTime EaRepositoryFileLastModifiedUtc { get; private set; }
        public string EaRepositoryFilePath { get; private set; } = string.Empty;
        public bool IsEaInstalledConfirmed { get; set; }
        public bool IsHandshakeSucceeded { get; private set; }
        public string EaName { get; private set; } = string.Empty;
        public string EaVersion { get; private set; } = string.Empty;
        public string ChartSymbol { get; private set; } = string.Empty;
        public string HandshakeAccountId { get; private set; } = string.Empty;
        public string HandshakeBrokerServer { get; private set; } = string.Empty;

        private string? _host;
        private int _port;
        private string? _lastAccountId;
        private string? _lastPassword;
        private string? _lastBrokerServer;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnStatusChanged?.Invoke(ConnectionStatusText);
                }
            }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    OnStatusChanged?.Invoke(ConnectionStatusText);
                }
            }
        }

        public string ConnectionStatusText
        {
            get
            {
                if (!IsConnected) return "Disconnected";
                if (!IsAuthenticated) return "Connected (Unauthenticated)";
                return "Authenticated";
            }
        }

        public double PingLatencyMs
        {
            get => _pingLatencyMs;
            private set => _pingLatencyMs = value;
        }

        public DateTime LastHeartbeatUtc
        {
            get => _lastHeartbeatUtc;
            private set => _lastHeartbeatUtc = value;
        }

        public string LastErrorMessage
        {
            get => _lastErrorMessage;
            private set => _lastErrorMessage = value;
        }

        public IReadOnlyCollection<string> SubscribedSymbols
        {
            get
            {
                lock (_lock)
                {
                    return _subscribedSymbols.AsReadOnly();
                }
            }
        }

        public event Action<PriceTickEnvelope>? OnTickReceived;
        public event Action<string>? OnStatusChanged;

        public Mt5BridgeService(IMt5BridgeClient bridgeClient, ILogger<Mt5BridgeService> logger)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _bridgeClient.OnMessageReceived += HandleIncomingBridgeMessage;
        }

        public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
        {
            lock (_lock)
            {
                _host = host;
                _port = port;
                _loopCts?.Cancel();
                _loopCts = new CancellationTokenSource();
            }

            _logger.LogInformation("[Mt5BridgeService] Initiating TCP bridge listener on {Host}:{Port}...", host, port);

            try
            {
                await _bridgeClient.ConnectAsync(ct);
                IsConnected = true;
                LastErrorMessage = string.Empty;
                _logger.LogInformation("[Mt5BridgeService] TCP bridge server listener started successfully.");

                // Start background health check and push-telemetry monitoring loop
                _telemetryTask = Task.Run(() => TelemetryLoopAsync(_loopCts.Token));
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastErrorMessage = ex.Message;
                _logger.LogError(ex, "[Mt5BridgeService] Failed to establish listener on {Host}:{Port}.", host, port);
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("[Mt5BridgeService] Disconnecting bridge connection...");

            CancellationTokenSource? localCts;
            lock (_lock)
            {
                localCts = _loopCts;
                _loopCts = null;
            }

            if (localCts != null)
            {
                localCts.Cancel();
                localCts.Dispose();
            }

            if (_telemetryTask != null)
            {
                try { await _telemetryTask; } catch { }
                _telemetryTask = null;
            }

            try
            {
                await _bridgeClient.DisconnectAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Mt5BridgeService] Error during clean client disconnect.");
            }

            IsConnected = false;
            IsAuthenticated = false;
            _logger.LogInformation("[Mt5BridgeService] Bridge disconnected cleanly.");
        }

        public async Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("AccountId is required.", nameof(accountId));
            if (string.IsNullOrWhiteSpace(brokerServer)) throw new ArgumentException("BrokerServer is required.", nameof(brokerServer));

            lock (_lock)
            {
                _lastAccountId = accountId;
                _lastPassword = password;
                _lastBrokerServer = brokerServer;
            }

            _logger.LogInformation("[Mt5BridgeService] Dispatching Login command for Account: {AccountId}, Server: {BrokerServer}", accountId, brokerServer);

            var requestPayload = new LoginRequest
            {
                AccountId = accountId,
                Password = password,
                BrokerServer = brokerServer
            };

            string requestId = Guid.NewGuid().ToString();
            var envelope = BridgeMessageEnvelope.CreateRequest(requestId, "Login", requestPayload);

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(envelope, ct);

                if (responseEnvelope.Error != null)
                {
                    IsAuthenticated = false;
                    LastErrorMessage = responseEnvelope.Error.Message;
                    _logger.LogWarning("[Mt5BridgeService] Login rejected by EA. Code: {Code}, Message: {Msg}", responseEnvelope.Error.Code, responseEnvelope.Error.Message);
                    return false;
                }

                if (responseEnvelope.Payload == null)
                {
                    IsAuthenticated = false;
                    LastErrorMessage = "Empty payload response received.";
                    return false;
                }

                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var response = JsonSerializer.Deserialize<LoginResponse>(payloadJson);

                if (response != null && response.Success)
                {
                    IsAuthenticated = true;
                    LastErrorMessage = string.Empty;
                    _logger.LogInformation("[Mt5BridgeService] Login verified successfully. Session Authenticated.");
                    return true;
                }
                else
                {
                    IsAuthenticated = false;
                    LastErrorMessage = response?.ErrorMessage ?? "Authentication failed.";
                    _logger.LogWarning("[Mt5BridgeService] Authentication failed: {Reason}", LastErrorMessage);
                    return false;
                }
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                LastErrorMessage = ex.Message;
                _logger.LogError(ex, "[Mt5BridgeService] Exception thrown during Login flow.");
                return false;
            }
        }

        public async Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken ct = default)
        {
            if (!IsConnected) throw new InvalidOperationException("Bridge is not connected.");

            _logger.LogInformation("[Mt5BridgeService] Fetching account snapshot from bridge...");

            string requestId = Guid.NewGuid().ToString();
            var request = BridgeMessageEnvelope.CreateRequest(requestId, "GetAccountSnapshot", null);

            try
            {
                var responseEnvelope = await _bridgeClient.SendAsync(request, ct);

                if (responseEnvelope.Error != null)
                {
                    throw new Exception($"MT5 Bridge Error [{responseEnvelope.Error.Code}]: {responseEnvelope.Error.Message}");
                }

                if (responseEnvelope.Payload == null)
                {
                    throw new Exception("MT5 Bridge received empty account snapshot payload.");
                }

                var payloadJson = JsonSerializer.Serialize(responseEnvelope.Payload);
                var snapshotResponse = JsonSerializer.Deserialize<GetAccountSnapshotResponse>(payloadJson);

                if (snapshotResponse == null)
                {
                    throw new Exception("Failed to deserialize Account Snapshot from response.");
                }

                return new AccountSnapshotDto
                {
                    AccountId = snapshotResponse.AccountId.ToString(),
                    BrokerServer = snapshotResponse.Broker,
                    Balance = snapshotResponse.Balance,
                    Equity = snapshotResponse.Equity,
                    Margin = snapshotResponse.Margin,
                    FreeMargin = snapshotResponse.FreeMargin,
                    Leverage = snapshotResponse.Leverage,
                    Currency = snapshotResponse.Currency,
                    AccountMode = snapshotResponse.Broker.Contains("Demo", StringComparison.OrdinalIgnoreCase) ? "Demo" : "Real",
                    TerminalStatus = snapshotResponse.ConnectionHealth
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Mt5BridgeService] Failed to retrieve account snapshot.");
                LastErrorMessage = ex.Message;
                throw;
            }
        }

        #region Dynamic Case-Insensitive Symbol Management
        /// <summary>
        /// Registers a case-insensitive symbol subscription and dispatches the command to MT5.
        /// Normalizes strings to prevent double-subscription and casing mismatches.
        /// </summary>
        public async Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));

            // REASON: Normalize casing and remove trailing whitespaces to prevent collection mismatch
            string normalizedSymbol = symbol.Trim().ToUpperInvariant();

            lock (_lock)
            {
                if (_subscribedSymbols.Any(s => string.Equals(s, normalizedSymbol, StringComparison.OrdinalIgnoreCase))) return;
                _subscribedSymbols.Add(normalizedSymbol);
            }

            _logger.LogInformation("[Mt5BridgeService] Subscribing to symbol '{Symbol}'", normalizedSymbol);

            if (IsConnected)
            {
                await SendSubscriptionCommandAsync(normalizedSymbol, "SubscribeSymbol", ct);
            }
        }

        /// <summary>
        /// Removes an active symbol subscription with strict case-insensitivity, 
        /// and dispatches the shutdown stream command to the MT5 EA.
        /// </summary>
        public async Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));

            // REASON: Normalize inputs to guarantee matching existing uppercase collection elements
            string normalizedSymbol = symbol.Trim().ToUpperInvariant();

            bool removed = false;
            lock (_lock)
            {
                // Case-insensitive locator and removal
                var existing = _subscribedSymbols.FirstOrDefault(s => string.Equals(s, normalizedSymbol, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _subscribedSymbols.Remove(existing);
                    removed = true;
                }
            }

            if (!removed)
            {
                _logger.LogWarning("[Mt5BridgeService] Unsubscribe aborted. Symbol '{Symbol}' was not found in active subscriptions.", normalizedSymbol);
                return;
            }

            _logger.LogInformation("[Mt5BridgeService] Unsubscribing from symbol '{Symbol}'", normalizedSymbol);

            if (IsConnected)
            {
                await SendSubscriptionCommandAsync(normalizedSymbol, "UnsubscribeSymbol", ct);
            }
        }
        #endregion
        private async Task SendSubscriptionCommandAsync(string symbol, string command, CancellationToken ct)
        {
            string requestId = Guid.NewGuid().ToString();
            var payload = new { symbol = symbol };
            var envelope = BridgeMessageEnvelope.CreateRequest(requestId, command, payload);

            try
            {
                var response = await _bridgeClient.SendAsync(envelope, ct);
                if (response.Error != null)
                {
                    _logger.LogWarning("[Mt5BridgeService] Subscription command '{Cmd}' failed for symbol '{Symbol}'. Error: {Err}", command, symbol, response.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Mt5BridgeService] Exception during subscription command '{Cmd}' for symbol '{Symbol}'", command, symbol);
            }
        }

        private void HandleIncomingBridgeMessage(BridgeMessageEnvelope envelope)
        {
            if (envelope == null) return;

            // Handle ReceiveTickStream push message from the EA
            if (string.Equals(envelope.Command, "ReceiveTickStream", StringComparison.OrdinalIgnoreCase))
            {
                if (envelope.Payload == null) return;

                try
                {
                    var payloadJson = JsonSerializer.Serialize(envelope.Payload);
                    var tickNotify = JsonSerializer.Deserialize<TickNotification>(payloadJson);

                    if (tickNotify != null)
                    {
                        // Update Last Heartbeat on receiving tick stream
                        LastHeartbeatUtc = DateTime.UtcNow;

                        // Normalize and construct PriceTickEnvelope
                        DateTime timestamp = DateTime.TryParse(tickNotify.Timestamp, out var dt)
                            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                            : DateTime.UtcNow;

                        var tickEnvelope = new PriceTickEnvelope(
                            SymbolName: tickNotify.Symbol,
                            Timestamp: timestamp,
                            Bid: tickNotify.Bid,
                            Ask: tickNotify.Ask,
                            SequenceNumber: DateTime.UtcNow.Ticks
                        );

                        _logger.LogDebug("[Mt5BridgeService] Stream tick received: Symbol={Symbol}, Bid={Bid}, Ask={Ask}", tickNotify.Symbol, tickNotify.Bid, tickNotify.Ask);

                        OnTickReceived?.Invoke(tickEnvelope);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Mt5BridgeService] Error parsing incoming Tick Stream notification.");
                }
            }
            else if (string.Equals(envelope.Command, "Ping", StringComparison.OrdinalIgnoreCase) && envelope.MessageType == "Response")
            {
                // Active Ping response from EA
                LastHeartbeatUtc = DateTime.UtcNow;
            }
        }

        private async Task TelemetryLoopAsync(CancellationToken token)
        {
            int missedPings = 0;
            var sw = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, token);

                    if (_bridgeClient is TcpMt5BridgeClient tcpClient && !tcpClient.IsConnected)
                    {
                        // Client disconnected at socket level
                        if (IsConnected)
                        {
                            _logger.LogWarning("[Mt5BridgeService] Socket disconnected. Entering recovery path.");
                            HandleDisconnectState();
                        }
                        continue;
                    }

                    // Execute ping handshake to measure latency
                    string requestId = Guid.NewGuid().ToString();
                    var envelope = BridgeMessageEnvelope.CreateRequest(requestId, "Ping", null);

                    sw.Restart();
                    try
                    {
                        var response = await _bridgeClient.SendAsync(envelope, token);
                        sw.Stop();

                        if (response != null)
                        {
                            PingLatencyMs = sw.Elapsed.TotalMilliseconds;
                            LastHeartbeatUtc = DateTime.UtcNow;
                            missedPings = 0;

                            // If socket connects but we lost authentication state (e.g. terminal restart), recover login
                            if (IsConnected && !IsAuthenticated && _lastAccountId != null && _lastPassword != null && _lastBrokerServer != null)
                            {
                                _logger.LogInformation("[Mt5BridgeService] Session alive but unauthenticated. Restoring credentials login...");
                                await LoginAsync(_lastAccountId, _lastPassword, _lastBrokerServer, token);

                                if (IsAuthenticated)
                                {
                                    // Restore active symbol subscriptions
                                    await RestoreSubscriptionsAsync(token);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        missedPings++;
                        _logger.LogWarning(ex, "[Mt5BridgeService] Heartbeat ping failed. Missed count: {Missed}", missedPings);

                        if (missedPings >= 3)
                        {
                            _logger.LogError("[Mt5BridgeService] 3 consecutive heartbeats missed. Session declared stale.");
                            HandleDisconnectState();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Mt5BridgeService] Telemetry Loop encountered error.");
                }
            }
        }

        private async Task RestoreSubscriptionsAsync(CancellationToken token)
        {
            string[] symbols;
            lock (_lock)
            {
                symbols = _subscribedSymbols.ToArray();
            }

            foreach (var symbol in symbols)
            {
                _logger.LogInformation("[Mt5BridgeService] Restoring subscription for {Symbol}...", symbol);
                await SendSubscriptionCommandAsync(symbol, "SubscribeSymbol", token);
            }
        }

        private void HandleDisconnectState()
        {
            IsConnected = false;
            IsAuthenticated = false;
            PingLatencyMs = 0;
            LastErrorMessage = "Bridge connection lost.";

            // Attempt reconnect logic with retry backoff in background
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("[Mt5BridgeService] Automatic connection recovery started.");
                int retryCount = 1;
                while (!_isConnected && _loopCts != null && !_loopCts.IsCancellationRequested)
                {
                    try
                    {
                        int delay = Math.Min(30, retryCount * 5); // backoff caps at 30 seconds
                        _logger.LogInformation("[Mt5BridgeService] Reconnect attempt {Retry} in {Delay}s...", retryCount, delay);
                        await Task.Delay(delay * 1000, _loopCts.Token);

                        if (_host != null)
                        {
                            await _bridgeClient.ConnectAsync(_loopCts.Token);
                            IsConnected = true;
                            _logger.LogInformation("[Mt5BridgeService] Socket listener restored successfully.");

                            // Recover Login
                            if (_lastAccountId != null && _lastPassword != null && _lastBrokerServer != null)
                            {
                                await LoginAsync(_lastAccountId, _lastPassword, _lastBrokerServer, _loopCts.Token);
                                if (IsAuthenticated)
                                {
                                    await RestoreSubscriptionsAsync(_loopCts.Token);
                                }
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[Mt5BridgeService] Reconnect attempt {Retry} failed: {Msg}", retryCount, ex.Message);
                        retryCount++;
                    }
                }
            });
        }

        public void Dispose()
        {
            _bridgeClient.OnMessageReceived -= HandleIncomingBridgeMessage;
            DisconnectAsync().GetAwaiter().GetResult();
        }

        // Internal contracts classes
        private class LoginRequest
        {
            [JsonPropertyName("accountId")]
            public string AccountId { get; set; } = string.Empty;

            [JsonPropertyName("password")]
            public string Password { get; set; } = string.Empty;

            [JsonPropertyName("brokerServer")]
            public string BrokerServer { get; set; } = string.Empty;
        }

        private class LoginResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("errorMessage")]
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class TickNotification
        {
            [JsonPropertyName("symbol")]
            public string Symbol { get; set; } = string.Empty;

            [JsonPropertyName("timestamp")]
            public string Timestamp { get; set; } = string.Empty;

            [JsonPropertyName("bid")]
            public double Bid { get; set; }

            [JsonPropertyName("ask")]
            public double Ask { get; set; }

            [JsonPropertyName("spread")]
            public double Spread { get; set; }

            [JsonPropertyName("volume")]
            public double Volume { get; set; }
        }
    }
}
