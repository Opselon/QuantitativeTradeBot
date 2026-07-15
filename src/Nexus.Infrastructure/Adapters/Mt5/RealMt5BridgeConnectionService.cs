using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Infrastructure.Adapters.Mt5
{
    /// <summary>
    /// Service responsible for orchestrating connection handshakes and testing connectivity 
    /// between the local trading engine and the active MetaTrader 5 Expert Advisor client.
    /// Includes an automated diagnostic log system to guide users through MT5-specific restrictions.
    /// </summary>
    public class RealMt5BridgeConnectionService : IMt5ConnectionService
    {
        private readonly IMt5BridgeClient _bridgeClient;
        private readonly IMt5AccountService _realAccountService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealMt5BridgeConnectionService"/> class.
        /// </summary>
        /// <param name="bridgeClient">The underlying TCP bridge communications client.</param>
        /// <param name="realAccountService">The service used to request live account details.</param>
        public RealMt5BridgeConnectionService(IMt5BridgeClient bridgeClient, IMt5AccountService realAccountService)
        {
            _bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
            _realAccountService = realAccountService ?? throw new ArgumentNullException(nameof(realAccountService));
        }

        #region NEW VERSION - MT5 Handshake Diagnostics Trace Engine
        /// <summary>
        /// Executes an asynchronous end-to-end handshake test.
        /// Performs port checks, boots the socket listener, and monitors for incoming client packets.
        /// </summary>
        /// <param name="profile">The connectivity profile containing port and timeout information.</param>
        /// <param name="cancellationToken">Token to cancel the request.</param>
        /// <returns>A DTO containing success status and account snapshot details.</returns>
        public async Task<ConnectionTestResultDto> TestConnectionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            var trace = new StringBuilder();
            trace.AppendLine("=================================================================");
            trace.AppendLine("         NEXUS DIRECT GATEWAY HANDSHAKE DIAGNOSTICS TRACE        ");
            trace.AppendLine("=================================================================");
            trace.AppendLine($"Execution Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            trace.AppendLine($"Settings Timeout: {profile.TimeoutSeconds} seconds");

            int activePort = 5000;

            try
            {
                // Step 1: Pre-flight check for port collision
                trace.AppendLine("\n[Step 1/5] Running pre-flight network collision checks...");
                bool isPortBusy = IsPortOccupied(activePort);
                if (isPortBusy)
                {
                    trace.AppendLine($"[WARN] Port {activePort} is already registered as occupied on this machine!");
                    trace.AppendLine("[WARN] This occurs if another instance of NTE or Kestrel is holding the port.");
                }
                else
                {
                    trace.AppendLine($"[INFO] Port {activePort} is free and ready to accept listener bindings.");
                }

                // Step 2: Initialize local server listener
                trace.AppendLine("\n[Step 2/5] Initiating local TCP listener server...");
                LogDiagnostic("Initiating TCP Server Listener for direct connection test...");
                await _bridgeClient.ConnectAsync(cancellationToken);
                trace.AppendLine($"[INFO] Socket server listener successfully opened. Port {activePort} is active.");

                // Step 3: Enter handshaking poll loop
                trace.AppendLine($"\n[Step 3/5] Starting handshake listener loop. Awaiting MT5 EA connection...");
                LogDiagnostic($"Waiting up to {profile.TimeoutSeconds}s for the MT5 EA to connect on port {activePort}...");

                bool isConnected = false;
                int pollLimit = Math.Max(5, Math.Min(profile.TimeoutSeconds, 15));
                int pollAttempts = pollLimit * 2;

                for (int i = 1; i <= pollAttempts; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        trace.AppendLine($"[CANCEL] Handshake cancelled by user at poll index {i}.");
                        LogWarning("Connection test cancelled by operator.");
                        break;
                    }

                    bool clientActive = IsClientConnected(_bridgeClient);
                    trace.AppendLine($"[POLL] Check {i}/{pollAttempts}: Handshake Established = {clientActive}");

                    if (clientActive)
                    {
                        isConnected = true;
                        LogDiagnostic("Socket handshake detected. MetaTrader 5 EA successfully connected!");
                        trace.AppendLine("[INFO] Handshake verified. MT5 client socket actively piped into bridge stream.");
                        break;
                    }

                    await Task.Delay(500, cancellationToken);
                }

                // Step 4: Handle failure states and display detailed diagnostic advice
                if (!isConnected)
                {
                    trace.AppendLine("\n[Step 4/5] Handshake loop completed. Connection State: FAILED.");
                    trace.AppendLine("[ERROR] MetaTrader 5 EA did not connect within the timeout limit.");

                    LogError("Socket handshake timed out. No client connection received from MetaTrader 5.");

                    string formattedError = FormatDiagnosticsFailure(trace, activePort);
                    return new ConnectionTestResultDto
                    {
                        IsSuccess = false,
                        ErrorMessage = formattedError
                    };
                }

                // Step 5: Query account details to verify data integrity
                trace.AppendLine("\n[Step 5/5] Executing live test command transmission...");
                LogDiagnostic("Requesting live account snapshot over the active socket session...");

                using var session = new RealMt5BridgeSession(_bridgeClient);
                var snapshot = await _realAccountService.GetAccountSnapshotAsync(session, cancellationToken);

                LogDiagnostic($"Snapshot retrieval complete! Account: {snapshot.AccountId}, Balance: {snapshot.Balance:C}");
                trace.AppendLine($"[SUCCESS] Snapshot received! Account ID: {snapshot.AccountId}, Balance: {snapshot.Balance:C2}");
                trace.AppendLine("=================================================================");

                return new ConnectionTestResultDto
                {
                    IsSuccess = true,
                    AccountSnapshot = snapshot
                };
            }
            catch (Exception ex)
            {
                trace.AppendLine($"\n[FATAL ERROR] Diagnostic run crashed: {ex.Message}");
                trace.AppendLine(ex.StackTrace);
                trace.AppendLine("=================================================================");

                LogError($"Critical error during connection testing routine: {ex.Message}");
                return new ConnectionTestResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = trace.ToString()
                };
            }
        }

        /// <summary>
        /// Creates and establishes a live stateful session wrapped around the direct bridge connection client.
        /// </summary>
        /// <param name="profile">The connection configuration profile.</param>
        /// <param name="cancellationToken">Cancellation token context.</param>
        public async Task<IMt5Session> CreateSessionAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            LogDiagnostic("Initializing live bridge session...");
            var session = new RealMt5BridgeSession(_bridgeClient);
            await session.ConnectAsync(cancellationToken);
            return session;
        }
        #endregion

        #region HELPER METHODS
        /// <summary>
        /// Safely evaluates connection status without tightly binding to a concrete class.
        /// Handles direct concrete references, dynamic proxies, and interceptor decorators.
        /// </summary>
        /// <param name="client">The target IMt5BridgeClient interface instance.</param>
        /// <returns>True if the socket represents an active connected channel; otherwise false.</returns>
        private bool IsClientConnected(IMt5BridgeClient client)
        {
            if (client == null) return false;

            // 1. Inspect direct concrete class if available
            if (client is TcpMt5BridgeClient tcpClient)
            {
                return tcpClient.IsConnected;
            }

            // 2. Fall back to dynamic reflection to support DI wrappers/mocks/proxies 
            try
            {
                PropertyInfo? property = client.GetType().GetProperty("IsConnected");
                if (property != null && property.PropertyType == typeof(bool))
                {
                    return (bool)property.GetValue(client)!;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to dynamically inspect 'IsConnected' via reflection: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Queries active network connections to check if the target port is currently occupied.
        /// </summary>
        /// <param name="port">The target port to inspect (typically 5000).</param>
        /// <returns>True if the port is in use; otherwise false.</returns>
        private bool IsPortOccupied(int port)
        {
            try
            {
                var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                var activeListeners = ipProperties.GetActiveTcpListeners();

                foreach (var listener in activeListeners)
                {
                    if (listener.Port == port)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Could not verify active port listeners via IPGlobalProperties: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Compiles the trace log with customized troubleshooting directions for Error 4014.
        /// </summary>
        /// <param name="trace">The accumulated string builder.</param>
        /// <param name="port">The target port used during binding.</param>
        /// <returns>A formatted diagnostics instruction string.</returns>
        private string FormatDiagnosticsFailure(StringBuilder trace, int port)
        {
            trace.AppendLine("\n=================================================================");
            trace.AppendLine("             CRITICAL ANALYSIS FOR MQL5 ERROR CODE: 4014         ");
            trace.AppendLine("=================================================================");
            trace.AppendLine("Error Code 4014 is 'Function is not allowed for call'.");
            trace.AppendLine("This occurs because the MetaTrader 5 Terminal blocks raw TCP");
            trace.AppendLine("socket functions until the address is explicitly whitelisted.");
            trace.AppendLine("\nFIX STEP 1: Add raw host and port to the MT5 Options whitelist:");
            trace.AppendLine($"   Open MetaTrader 5 -> Tools -> Options -> Expert Advisors.");
            trace.AppendLine($"   Double-click the empty space under 'Allow WebRequest for listed URL' and add:");
            trace.AppendLine($"   ->  127.0.0.1:{port}      (REQUIRED: Add without HTTP prefix)");
            trace.AppendLine($"   ->  localhost:{port}");
            trace.AppendLine($"   ->  http://127.0.0.1:{port}");
            trace.AppendLine("\nFIX STEP 2: Verify folder location:");
            trace.AppendLine("   Ensure 'fsds.mq5' is inside the 'MQL5\\Experts\\' folder.");
            trace.AppendLine("   If it is inside 'MQL5\\Indicators\\', MT5 blocks network sockets.");
            trace.AppendLine("=================================================================");

            return trace.ToString();
        }
        #endregion

        #region DIAGNOSTIC LOGGING SYSTEM
        private void LogDiagnostic(string message)
        {
            Console.WriteLine($"[RealMt5BridgeConnectionService] [INFO] {message}");
        }

        private void LogWarning(string message)
        {
            Console.WriteLine($"[RealMt5BridgeConnectionService] [WARN] {message}");
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[RealMt5BridgeConnectionService] [ERROR] {message}");
        }
        #endregion
    }
}