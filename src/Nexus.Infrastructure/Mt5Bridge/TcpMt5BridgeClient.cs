using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using System.Collections.Concurrent;

namespace Nexus.Infrastructure.Mt5Bridge
{
    /// <summary>
    /// Custom IMt5BridgeClient acting as an HTTP Queue Bridge on Port 8080.
    /// Safely bridges asynchronous C# Task commands to MT5's short-polling REST requests.
    /// </summary>
    public class TcpMt5BridgeClient : IMt5BridgeClient, IDisposable
    {
        private readonly IAppConfigurationService _configService;
        private readonly ConcurrentQueue<BridgeMessageEnvelope> _outgoingRequests = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<BridgeMessageEnvelope>> _pendingRequests = new();
        private DateTime _lastPollTime = DateTime.MinValue;
        private readonly object _lock = new();
        private bool _isDisposed;

        /// <summary>
        /// Gets a value indicating whether the MetaTrader 5 EA polled Kestrel recently (within 5 seconds).
        /// </summary>
        public bool IsConnected => (DateTime.UtcNow - _lastPollTime).TotalSeconds < 5;

        /// <summary>
        /// Occurs when any message (such as live tick stream) is received from MT5 over Kestrel.
        /// </summary>
        public event Action<BridgeMessageEnvelope>? OnMessageReceived;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpMt5BridgeClient"/> class.
        /// </summary>
        public TcpMt5BridgeClient(IAppConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        #region NEW VERSION - HTTP REST Handshake & Queue Processing
        /// <summary>
        /// Starts the listener status. Connectivity is managed dynamically via HTTP polling.
        /// </summary>
        public Task ConnectAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                _lastPollTime = DateTime.UtcNow;
                Console.WriteLine("[TcpMt5BridgeClient] HTTP Queue Interface started on Port 8080. Awaiting MT5 polls...");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gracefully stops and cancels all waiting Tasks.
        /// </summary>
        public Task DisconnectAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                _outgoingRequests.Clear();
                var pendingKeys = _pendingRequests.Keys;
                foreach (var key in pendingKeys)
                {
                    if (_pendingRequests.TryRemove(key, out var tcs))
                    {
                        tcs.TrySetException(new OperationCanceledException("Bridge connection was disconnected."));
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Queues a request to the MT5 poll queue and asynchronously awaits its POST response.
        /// </summary>
        public async Task<BridgeMessageEnvelope> SendAsync(BridgeMessageEnvelope request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var tcs = new TaskCompletionSource<BridgeMessageEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingRequests.TryAdd(request.RequestId, tcs))
            {
                throw new InvalidOperationException($"A request with RequestId '{request.RequestId}' is already pending.");
            }

            // Queue request to be grabbed by next GET poll
            _outgoingRequests.Enqueue(request);

            Console.WriteLine($"[TcpMt5BridgeClient] Queued command request {request.RequestId}. Awaiting MT5 poll...");

            try
            {
                using (ct.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _pendingRequests.TryRemove(request.RequestId, out _);
                Console.WriteLine($"[TcpMt5BridgeClient] Request {request.RequestId} execution error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the next command envelope queued for MT5. Called by Kestrel Poll Route.
        /// </summary>
        public BridgeMessageEnvelope? DequeueRequest()
        {
            _lastPollTime = DateTime.UtcNow;
            if (_outgoingRequests.TryDequeue(out var request))
            {
                return request;
            }
            return null;
        }

        /// <summary>
        /// Registers a response envelope POSTed back from MT5.
        /// Resolves the corresponding TaskCompletionSource.
        /// </summary>
        public void RegisterResponse(BridgeMessageEnvelope response)
        {
            _lastPollTime = DateTime.UtcNow;

            if (_pendingRequests.TryRemove(response.RequestId, out var tcs))
            {
                tcs.TrySetResult(response);
            }

            // Notify downstream pipeline of tick streams and logs
            OnMessageReceived?.Invoke(response);
        }
        #endregion

        #region ERROR HANDLING & DISPOSAL
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        #endregion
    }
}