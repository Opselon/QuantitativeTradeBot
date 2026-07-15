using Nexus.Application.Ports;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly MarketDataPipeline _pipeline;
        private readonly CancellationTokenSource _cts = new();

        public bool IsBridgeConnected => _bridgeService.IsConnected;
        public string ConnectionStatusText => _bridgeService.ConnectionStatusText;
        public double PingLatencyMs => _bridgeService.PingLatencyMs;
        public long ProcessedTickCount => _pipeline.ProcessedTickCount;

        public DashboardViewModel(IMt5BridgeService bridgeService, MarketDataPipeline pipeline)
        {
            _bridgeService = bridgeService;
            _pipeline = pipeline;

            // Simple loop to refresh properties
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, _cts.Token);
                        OnPropertyChanged(nameof(IsBridgeConnected));
                        OnPropertyChanged(nameof(ConnectionStatusText));
                        OnPropertyChanged(nameof(PingLatencyMs));
                        OnPropertyChanged(nameof(ProcessedTickCount));
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                }
            });
        }
    }
}
