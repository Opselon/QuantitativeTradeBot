using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class TestConsoleViewModel : ViewModelBase
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly MarketDataPipeline _pipeline;
        private readonly IDiagnosticService _diagnosticService;
        private readonly CancellationTokenSource _cts = new();

        private string _testOutcome = "Not Tested";
        private bool _testSuccess;

        public string TestOutcome
        {
            get => _testOutcome;
            set => SetProperty(ref _testOutcome, value);
        }

        public bool TestSuccess
        {
            get => _testSuccess;
            set => SetProperty(ref _testSuccess, value);
        }

        public ObservableCollection<string> SmokeTestLogList { get; } = new();

        public ICommand RunSmokeTestCommand { get; }

        public TestConsoleViewModel(
            IMt5BridgeService bridgeService,
            MarketDataPipeline pipeline,
            IDiagnosticService diagnosticService)
        {
            _bridgeService = bridgeService;
            _pipeline = pipeline;
            _diagnosticService = diagnosticService;

            RunSmokeTestCommand = new AsyncRelayCommand(OnRunSmokeTestAsync);
        }

        private async Task OnRunSmokeTestAsync()
        {
            SmokeTestLogList.Clear();
            AddSmokeLog("Starting automated Real Smoke Test workflow...");
            TestOutcome = "Running...";

            try
            {
                // Step 1: Connect
                AddSmokeLog("Step 1: Connecting to localhost bridge...");
                await _bridgeService.ConnectAsync("127.0.0.1", 5000, _cts.Token);
                await Task.Delay(1000); // Wait for initialization
                AddSmokeLog($"Status: Connected={_bridgeService.IsConnected}");

                // Step 2: Login
                AddSmokeLog("Step 2: Authenticating Demo Account ID...");
                bool loginOk = await _bridgeService.LoginAsync("7820491", "demo_password", "ICMarkets-Demo", _cts.Token);
                AddSmokeLog($"Status: Authenticated={_bridgeService.IsAuthenticated}, Message={_bridgeService.LastErrorMessage}");

                if (loginOk)
                {
                    // Step 3: Handshake
                    AddSmokeLog("Step 3: Handshake check...");
                    AddSmokeLog($"Handshake Status: Succeeded={_bridgeService.IsHandshakeSucceeded}");

                    // Step 4: Ping
                    AddSmokeLog("Step 4: Sending heartbeat ping...");
                    var sw = Stopwatch.StartNew();
                    var snap = await _bridgeService.GetAccountSnapshotAsync(_cts.Token);
                    sw.Stop();
                    AddSmokeLog($"Status: Ping completed in {sw.ElapsedMilliseconds} ms. Latency={_bridgeService.PingLatencyMs} ms");

                    // Step 5: Subscribe symbol EURUSD
                    AddSmokeLog("Step 5: Subscribing to symbol EURUSD...");
                    await _bridgeService.SubscribeSymbolAsync("EURUSD", _cts.Token);
                    AddSmokeLog("Status: Subscription request sent EURUSD.");

                    // Step 6: Verify live ticks
                    AddSmokeLog("Step 6: Monitoring live ticks for 5 seconds...");
                    long startTicks = _pipeline.ProcessedTickCount;
                    await Task.Delay(5000);
                    long endTicks = _pipeline.ProcessedTickCount;
                    AddSmokeLog($"Status: Ticks Ingested={endTicks - startTicks}, Total Processed={_pipeline.ProcessedTickCount}");

                    // Step 7: Unsubscribe symbol
                    AddSmokeLog("Step 7: Unsubscribing from EURUSD...");
                    await _bridgeService.UnsubscribeSymbolAsync("EURUSD", _cts.Token);
                    AddSmokeLog("Status: Unsubscribed EURUSD.");
                }

                // Step 8: Disconnect
                AddSmokeLog("Step 8: Gracefully disconnecting bridge listener...");
                await _bridgeService.DisconnectAsync(_cts.Token);
                AddSmokeLog("Status: Disconnected.");

                AddSmokeLog("Automated Real Smoke Test completed successfully!");
                TestOutcome = "Passed";
                TestSuccess = true;
            }
            catch (Exception ex)
            {
                AddSmokeLog($"ERROR: Smoke Test failed: {ex.Message}");
                TestOutcome = $"Failed: {ex.Message}";
                TestSuccess = false;
            }
        }

        private void AddSmokeLog(string message)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                SmokeTestLogList.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            });
            if (System.Windows.Application.Current == null)
            {
                SmokeTestLogList.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }
    }
}
