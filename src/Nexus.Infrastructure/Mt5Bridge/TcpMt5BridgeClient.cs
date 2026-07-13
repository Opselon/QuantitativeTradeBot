using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Mt5Bridge
{
    public class TcpMt5BridgeClient : IMt5BridgeClient, IDisposable
    {
        private readonly IAppConfigurationService _configService;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<BridgeMessageEnvelope>> _pendingRequests = new();

        private TcpListener? _listener;
        private TcpClient? _activeClient;
        private NetworkStream? _activeStream;
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;
        private Task? _readTask;
        private readonly object _lock = new();
        private bool _isDisposed;

        public bool IsConnected => _activeClient != null && _activeClient.Connected;

        public event Action<BridgeMessageEnvelope>? OnMessageReceived;

        public TcpMt5BridgeClient(IAppConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public Task ConnectAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                if (_listener != null)
                {
                    return Task.CompletedTask; // Already listening/started
                }

                var settings = _configService.GetSettings();
                // Extract host and port from settings. Fallback to localhost:5000 if not specified.
                string host = "127.0.0.1";
                int port = 5000;

                // We will add properties Mt5BridgeHost and Mt5BridgePort to AppSettings shortly.
                // For now, let's dynamically check if they exist or use default values.
                try
                {
                    var modeProp = settings.GetType().GetProperty("Mt5BridgeHost");
                    if (modeProp != null)
                    {
                        host = modeProp.GetValue(settings) as string ?? "127.0.0.1";
                    }
                    var portProp = settings.GetType().GetProperty("Mt5BridgePort");
                    if (portProp != null)
                    {
                        port = (int)(portProp.GetValue(settings) ?? 5000);
                    }
                }
                catch
                {
                    // Fallback to defaults
                }

                if (!IPAddress.TryParse(host, out var ipAddress))
                {
                    ipAddress = IPAddress.Any;
                }

                _cts = new CancellationTokenSource();
                _listener = new TcpListener(ipAddress, port);
                _listener.Start();

                Console.WriteLine($"[TcpMt5BridgeClient] TCP Listener started on {ipAddress}:{port}");

                _listenerTask = Task.Run(() => AcceptConnectionsAsync(_cts.Token), _cts.Token);
            }

            return Task.CompletedTask;
        }

        public async Task DisconnectAsync(CancellationToken ct)
        {
            CancellationTokenSource? localCts = null;
            TcpListener? localListener = null;

            lock (_lock)
            {
                localCts = _cts;
                _cts = null;

                localListener = _listener;
                _listener = null;
            }

            if (localCts != null)
            {
                localCts.Cancel();
                localCts.Dispose();
            }

            if (localListener != null)
            {
                try
                {
                    localListener.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TcpMt5BridgeClient] Error stopping listener: {ex.Message}");
                }
            }

            // Await tasks to complete
            if (_listenerTask != null)
            {
                try { await _listenerTask; } catch { }
                _listenerTask = null;
            }

            CloseActiveClient();

            // Fail any pending requests
            var pendingKeys = _pendingRequests.Keys;
            foreach (var key in pendingKeys)
            {
                if (_pendingRequests.TryRemove(key, out var tcs))
                {
                    tcs.TrySetException(new OperationCanceledException("Bridge disconnected."));
                }
            }
        }

        public async Task<BridgeMessageEnvelope> SendAsync(BridgeMessageEnvelope request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            NetworkStream? stream = null;
            lock (_lock)
            {
                if (!IsConnected)
                {
                    throw new InvalidOperationException("MT5 EA is not connected to the bridge.");
                }
                stream = _activeStream;
            }

            if (stream == null)
            {
                throw new InvalidOperationException("Bridge connection is currently inactive.");
            }

            var tcs = new TaskCompletionSource<BridgeMessageEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingRequests.TryAdd(request.RequestId, tcs))
            {
                throw new InvalidOperationException($"A request with RequestId '{request.RequestId}' is already pending.");
            }

            try
            {
                // Serialize request and append newline delimiter
                var json = JsonSerializer.Serialize(request) + "\n";
                var bytes = Encoding.UTF8.GetBytes(json);

                Console.WriteLine($"[TcpMt5BridgeClient] Sending request RequestId: {request.RequestId}, Command: {request.Command}");

                await stream.WriteAsync(bytes, 0, bytes.Length, ct);
                await stream.FlushAsync(ct);

                // Wait for response or timeout
                using (ct.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _pendingRequests.TryRemove(request.RequestId, out _);
                Console.WriteLine($"[TcpMt5BridgeClient] Error sending request {request.RequestId}: {ex.Message}");
                throw;
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var listener = _listener;
                    if (listener == null) break;

                    var client = await listener.AcceptTcpClientAsync(token);
                    Console.WriteLine($"[TcpMt5BridgeClient] Accepted connection from {client.Client.RemoteEndPoint}");

                    lock (_lock)
                    {
                        CloseActiveClient();
                        _activeClient = client;
                        _activeStream = client.GetStream();
                        _readTask = Task.Run(() => ReadIncomingMessagesAsync(_activeStream, token), token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine($"[TcpMt5BridgeClient] Error accepting connection: {ex.Message}");
                        await Task.Delay(1000, token); // Throttle retries
                    }
                }
            }
        }

        private async Task ReadIncomingMessagesAsync(NetworkStream stream, CancellationToken token)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var line = await reader.ReadLineAsync(token);
                    if (line == null)
                    {
                        // Connection closed by remote side
                        Console.WriteLine("[TcpMt5BridgeClient] Connection closed by MT5 EA.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    ProcessIncomingLine(line);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Console.WriteLine($"[TcpMt5BridgeClient] Error reading message: {ex.Message}");
                    }
                    break;
                }
            }

            lock (_lock)
            {
                if (_activeStream == stream)
                {
                    CloseActiveClient();
                }
            }
        }

        private void ProcessIncomingLine(string line)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<BridgeMessageEnvelope>(line);
                if (envelope == null) return;

                Console.WriteLine($"[TcpMt5BridgeClient] Received response RequestId: {envelope.RequestId}, Command: {envelope.Command}, MessageType: {envelope.MessageType}");

                if (envelope.MessageType == "Response")
                {
                    if (_pendingRequests.TryRemove(envelope.RequestId, out var tcs))
                    {
                        tcs.TrySetResult(envelope);
                    }
                }

                // Always raise OnMessageReceived for any message to support push notifications like streaming ticks.
                OnMessageReceived?.Invoke(envelope);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpMt5BridgeClient] Failed to deserialize incoming message line: {ex.Message}");
            }
        }

        private void CloseActiveClient()
        {
            lock (_lock)
            {
                if (_activeStream != null)
                {
                    try { _activeStream.Dispose(); } catch { }
                    _activeStream = null;
                }

                if (_activeClient != null)
                {
                    try { _activeClient.Close(); } catch { }
                    _activeClient = null;
                }

                _readTask = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
