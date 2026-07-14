using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.Mt5Bridge;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class MarketWatchViewModel : ViewModelBase, IDisposable
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly MarketDataPipeline _pipeline;
        private readonly IDiagnosticService _diagnosticService;
        private readonly CancellationTokenSource _cts = new();

        public ObservableCollection<DesktopSymbolViewModel> SubscribedSymbolsList { get; } = new()
        {
            new DesktopSymbolViewModel { SymbolName = "XAUUSD" },
            new DesktopSymbolViewModel { SymbolName = "EURUSD" },
            new DesktopSymbolViewModel { SymbolName = "GBPUSD" },
            new DesktopSymbolViewModel { SymbolName = "USDJPY" },
            new DesktopSymbolViewModel { SymbolName = "USDCHF" },
            new DesktopSymbolViewModel { SymbolName = "USDCAD" },
            new DesktopSymbolViewModel { SymbolName = "AUDUSD" },
            new DesktopSymbolViewModel { SymbolName = "NZDUSD" }
        };

        public ICommand ToggleSubscriptionCommand { get; }

        public MarketWatchViewModel(
            IMt5BridgeService bridgeService,
            MarketDataPipeline pipeline,
            IDiagnosticService diagnosticService)
        {
            _bridgeService = bridgeService;
            _pipeline = pipeline;
            _diagnosticService = diagnosticService;

            ToggleSubscriptionCommand = new AsyncRelayCommand<DesktopSymbolViewModel>(OnToggleSubscriptionAsync);

            _pipeline.OnPipelineTickProcessed += OnPipelineTickProcessed;
        }

        private async Task OnToggleSubscriptionAsync(DesktopSymbolViewModel? item)
        {
            if (item == null) return;
            try
            {
                if (item.IsSubscribed)
                {
                    await _bridgeService.UnsubscribeSymbolAsync(item.SymbolName, _cts.Token);
                    item.IsSubscribed = false;
                    _diagnosticService.Log("Mt5Bridge", "INFO", $"Unsubscribed symbol '{item.SymbolName}' cleanly.");
                }
                else
                {
                    await _bridgeService.SubscribeSymbolAsync(item.SymbolName, _cts.Token);
                    item.IsSubscribed = true;
                    _diagnosticService.Log("Mt5Bridge", "INFO", $"Subscribed symbol '{item.SymbolName}' via localhost bridge.");
                }
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Failed to toggle symbol subscription for '{item.SymbolName}': {ex.Message}");
            }
        }

        private void OnPipelineTickProcessed(PriceTickEnvelope tick)
        {
            if (tick == null) return;

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                foreach (var item in SubscribedSymbolsList)
                {
                    if (string.Equals(item.SymbolName, tick.SymbolName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Bid = tick.Bid;
                        item.Ask = tick.Ask;
                        item.Spread = tick.Ask - tick.Bid;
                        item.LastUpdateTimeText = tick.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
                        item.TickCount++;
                        break;
                    }
                }
            });
        }

        public void Dispose()
        {
            _pipeline.OnPipelineTickProcessed -= OnPipelineTickProcessed;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
