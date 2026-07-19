using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    /// <summary>
    /// Represents a live, stateful MetaTrader 5 direct bridge execution session.
    /// </summary>
    public class RealMt5BridgeSession : IMt5Session
    {
        private readonly IMt5BridgeClient _bridgeClient;
        private bool _isDisposed;

        /// <summary>
        /// Gets the unique session identifier.
        /// </summary>
        public string SessionId { get; } = "MT5_REAL_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();

        /// <summary>
        /// Gets the current connection status of the gateway session.
        /// </summary>
        public GatewayConnectionStatus Status { get; private set; } = GatewayConnectionStatus.Disconnected;

        /// <summary>
        /// Occurs when the session's gateway connection status changes.
        /// </summary>
        public event Action<GatewayConnectionStatus>? OnStatusChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealMt5BridgeSession"/> class.
        /// </summary>
        public RealMt5BridgeSession(IMt5BridgeClient bridgeClient)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
        }
        #region NEW VERSION - MT5 Bridge Improvements
        /// <summary>
        /// Transitions the session state and registers connection capabilities with the bridge.
        /// </summary>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Connected)
            {
                LogDiagnostic("Session connection requested but state is already Connected.");
                return;
            }

            try
            {
                UpdateStatus(GatewayConnectionStatus.Connecting);

                LogDiagnostic("Opening direct socket connection layer on the bridge...");
                await _bridgeClient.ConnectAsync(cancellationToken);

                UpdateStatus(GatewayConnectionStatus.Connected);
                LogDiagnostic("Session established and marked as active.");
            }
            catch (Exception ex)
            {
                UpdateStatus(GatewayConnectionStatus.Disconnected);
                LogError($"Session connection failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gracefully disconnects the stateful execution session.
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (Status == GatewayConnectionStatus.Disconnected)
            {
                LogDiagnostic("Session disconnect requested but state is already Disconnected.");
                return;
            }

            try
            {
                UpdateStatus(GatewayConnectionStatus.Connecting);

                LogDiagnostic("Disconnecting direct socket connection layer on the bridge...");
                await _bridgeClient.DisconnectAsync(cancellationToken);

                UpdateStatus(GatewayConnectionStatus.Disconnected);
                LogDiagnostic("Session successfully disposed and disconnected.");
            }
            catch (Exception ex)
            {
                UpdateStatus(GatewayConnectionStatus.Disconnected);
                LogError($"Session disconnection error: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region HELPER METHODS
        private void UpdateStatus(GatewayConnectionStatus newStatus)
        {
            if (Status != newStatus)
            {
                Status = newStatus;
                OnStatusChanged?.Invoke(Status);
            }
        }
        #endregion

        #region ERROR HANDLING & DISPOSAL
        /// <summary>
        /// Disposes resource dependencies and transitions status cleanly.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            UpdateStatus(GatewayConnectionStatus.Disconnected);
        }
        #endregion

        #region LOGGING
        private void LogDiagnostic(string message)
        {
            Console.WriteLine($"[RealMt5BridgeSession] [{SessionId}] [INFO] {message}");
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[RealMt5BridgeSession] [{SessionId}] [ERROR] {message}");
        }
        #endregion
    }
}