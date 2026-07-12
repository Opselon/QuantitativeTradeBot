using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Ports;
using Nexus.Desktop.Models;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels
{
    public class Mt5TradingViewModel : ViewModelBase, IDisposable
    {
        private readonly IMt5OperatorService _operatorService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IAppConfigurationService _configService;

        private bool _isConnected;
        private string _connectionStatusText = "Disconnected";
        private bool _isBusy;
        private bool _isRefreshing;
        private bool _isExecutingTrade;
        private string _selectedSymbol = "EURUSD";
        private decimal _orderVolume = 0.10m;
        private string _statusMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private DateTime _lastRefreshUtc;
        private string _lastResponseTimeText = "0 ms";
        private string _lastSuccessfulRefreshText = "Never";
        private DesktopPositionViewModel? _selectedPosition;
        private ObservableCollection<DesktopPositionViewModel> _openPositions = new();

        private readonly CancellationTokenSource _cts = new();
        private readonly object _refreshLock = new();
        private bool _isRefreshingPositions;
        private readonly int _refreshIntervalSeconds = 5;

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ConnectionStatusText
        {
            get => _connectionStatusText;
            set => SetProperty(ref _connectionStatusText, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public bool IsExecutingTrade
        {
            get => _isExecutingTrade;
            set => SetProperty(ref _isExecutingTrade, value);
        }

        public string SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                if (SetProperty(ref _selectedSymbol, value))
                {
                    ValidateInputs();
                }
            }
        }

        public decimal OrderVolume
        {
            get => _orderVolume;
            set
            {
                if (SetProperty(ref _orderVolume, value))
                {
                    ValidateInputs();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public DateTime LastRefreshUtc
        {
            get => _lastRefreshUtc;
            set => SetProperty(ref _lastRefreshUtc, value);
        }

        public string LastResponseTimeText
        {
            get => _lastResponseTimeText;
            set => SetProperty(ref _lastResponseTimeText, value);
        }

        public string LastSuccessfulRefreshText
        {
            get => _lastSuccessfulRefreshText;
            set => SetProperty(ref _lastSuccessfulRefreshText, value);
        }

        public string CurrentMode
        {
            get
            {
                var settings = _configService.GetSettings();
                return string.Equals(settings.Mt5Mode, "Real", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(settings.Mt5Mode, "RealBridge", StringComparison.OrdinalIgnoreCase)
                    ? "Real MT5"
                    : "Simulation";
            }
        }

        public ObservableCollection<DesktopPositionViewModel> OpenPositions
        {
            get => _openPositions;
            set => SetProperty(ref _openPositions, value);
        }

        public DesktopPositionViewModel? SelectedPosition
        {
            get => _selectedPosition;
            set => SetProperty(ref _selectedPosition, value);
        }

        public IAsyncRelayCommand RefreshPositionsCommand { get; }
        public IAsyncRelayCommand BuyCommand { get; }
        public IAsyncRelayCommand SellCommand { get; }
        public IAsyncRelayCommand CloseCommand { get; }
        public ICommand ClearErrorCommand { get; }

        public Mt5TradingViewModel(
            IMt5OperatorService operatorService,
            IDiagnosticService diagnosticService,
            IAppConfigurationService configService)
        {
            _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            RefreshPositionsCommand = new AsyncRelayCommand(OnRefreshPositionsAsync);
            BuyCommand = new AsyncRelayCommand(OnBuyAsync, CanExecuteTrade);
            SellCommand = new AsyncRelayCommand(OnSellAsync, CanExecuteTrade);
            CloseCommand = new AsyncRelayCommand(OnCloseAsync, CanClosePosition);
            ClearErrorCommand = new RelayCommand(OnClearError);

            ValidateInputs();

            // Start auto refresh loop
            Task.Run(() => RunAutoRefreshLoopAsync(_cts.Token));
        }

        private void OnClearError()
        {
            ErrorMessage = string.Empty;
        }

        private bool CanExecuteTrade()
        {
            if (string.IsNullOrWhiteSpace(SelectedSymbol)) return false;
            if (OrderVolume < 0.01m || OrderVolume > 100m) return false;
            if (IsBusy || IsExecutingTrade) return false;
            return true;
        }

        private bool CanClosePosition()
        {
            return SelectedPosition != null && !IsBusy && !IsExecutingTrade;
        }

        private void ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(SelectedSymbol))
            {
                ErrorMessage = "Symbol is required.";
                return;
            }
            if (OrderVolume < 0.01m || OrderVolume > 100m)
            {
                ErrorMessage = "Volume must be between 0.01 and 100.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private async Task RunAutoRefreshLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalSeconds), token);
                    if (!token.IsCancellationRequested)
                    {
                        await OnRefreshPositionsAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Ignore background exceptions to keep loop alive
                }
            }
        }

        public async Task OnRefreshPositionsAsync()
        {
            lock (_refreshLock)
            {
                if (_isRefreshingPositions)
                    return;
                _isRefreshingPositions = true;
            }

            IsRefreshing = true;
            IsBusy = true;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var positions = await _operatorService.GetPositionsAsync(_cts.Token);

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    OpenPositions.Clear();
                    foreach (var pos in positions)
                    {
                        OpenPositions.Add(new DesktopPositionViewModel(pos));
                    }
                });

                // Fail-safe collection update if Application.Current is null (e.g. inside unit tests)
                if (System.Windows.Application.Current == null)
                {
                    OpenPositions.Clear();
                    foreach (var pos in positions)
                    {
                        OpenPositions.Add(new DesktopPositionViewModel(pos));
                    }
                }

                IsConnected = true;
                ConnectionStatusText = "Connected";
                LastRefreshUtc = DateTime.UtcNow;
                LastSuccessfulRefreshText = LastRefreshUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                LastResponseTimeText = $"{stopwatch.ElapsedMilliseconds} ms";
                StatusMessage = "Positions refreshed successfully.";

                _diagnosticService.Log("Mt5Operator", "INFO", $"Refresh completed in {stopwatch.ElapsedMilliseconds} ms. Success: True, Positions: {positions.Count}");
            }
            catch (OperationCanceledException)
            {
                _diagnosticService.Log("Mt5Operator", "WARN", $"Refresh cancelled after {stopwatch.ElapsedMilliseconds} ms.");
                ErrorMessage = "Operation cancelled";
                StatusMessage = "Refresh cancelled.";
                throw;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatusText = "Disconnected";
                ErrorMessage = ex.Message;
                StatusMessage = "Refresh failed.";
                _diagnosticService.Log("Mt5Operator", "ERROR", $"Refresh failed in {stopwatch.ElapsedMilliseconds} ms. Error: {ex.Message}");
            }
            finally
            {
                lock (_refreshLock)
                {
                    _isRefreshingPositions = false;
                }
                IsRefreshing = false;
                IsBusy = false;
            }
        }

        public async Task OnBuyAsync()
        {
            await OnExecuteTradeAsync(DesktopOrderSide.Buy);
        }

        public async Task OnSellAsync()
        {
            await OnExecuteTradeAsync(DesktopOrderSide.Sell);
        }

        private async Task OnExecuteTradeAsync(DesktopOrderSide side)
        {
            if (!CanExecuteTrade()) return;

            IsExecutingTrade = true;
            IsBusy = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Executing {side} order...";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await _operatorService.PlaceOrderAsync(SelectedSymbol, side, OrderVolume, _cts.Token);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Trade executed successfully. Ticket: {result.Ticket}";
                    _diagnosticService.Log("Mt5Operator", "INFO", $"{side} {SelectedSymbol} {OrderVolume} lots completed in {stopwatch.ElapsedMilliseconds} ms. Success: True, Ticket: {result.Ticket}");

                    // Refresh positions
                    await OnRefreshPositionsAsync();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Trade rejected";
                    StatusMessage = "Trade execution failed.";
                    _diagnosticService.Log("Mt5Operator", "ERROR", $"{side} {SelectedSymbol} {OrderVolume} lots completed in {stopwatch.ElapsedMilliseconds} ms. Success: False, Error: {ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _diagnosticService.Log("Mt5Operator", "WARN", $"{side} {SelectedSymbol} {OrderVolume} lots cancelled after {stopwatch.ElapsedMilliseconds} ms.");
                ErrorMessage = "Operation cancelled";
                StatusMessage = "Trade cancelled.";
                throw;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusMessage = "Unexpected trade execution error.";
                _diagnosticService.Log("Mt5Operator", "ERROR", $"{side} {SelectedSymbol} {OrderVolume} lots failed in {stopwatch.ElapsedMilliseconds} ms. Error: {ex.Message}");
            }
            finally
            {
                IsExecutingTrade = false;
                IsBusy = false;
            }
        }

        public async Task OnCloseAsync()
        {
            if (!CanClosePosition()) return;

            var targetPosition = SelectedPosition;
            if (targetPosition == null) return;

            IsExecutingTrade = true;
            IsBusy = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Closing position ticket {targetPosition.Ticket}...";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await _operatorService.ClosePositionAsync(targetPosition.Ticket, targetPosition.Symbol, _cts.Token);
                if (result.IsSuccess)
                {
                    StatusMessage = $"Position ticket {targetPosition.Ticket} closed successfully.";
                    _diagnosticService.Log("Mt5Operator", "INFO", $"Close position ticket {targetPosition.Ticket} symbol {targetPosition.Symbol} completed in {stopwatch.ElapsedMilliseconds} ms. Success: True");

                    // Refresh positions
                    await OnRefreshPositionsAsync();
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Close rejected";
                    StatusMessage = "Close position execution failed.";
                    _diagnosticService.Log("Mt5Operator", "ERROR", $"Close position ticket {targetPosition.Ticket} symbol {targetPosition.Symbol} completed in {stopwatch.ElapsedMilliseconds} ms. Success: False, Error: {ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                _diagnosticService.Log("Mt5Operator", "WARN", $"Close position ticket {targetPosition.Ticket} cancelled after {stopwatch.ElapsedMilliseconds} ms.");
                ErrorMessage = "Operation cancelled";
                StatusMessage = "Close position cancelled.";
                throw;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusMessage = "Unexpected close position error.";
                _diagnosticService.Log("Mt5Operator", "ERROR", $"Close position ticket {targetPosition.Ticket} symbol {targetPosition.Symbol} failed in {stopwatch.ElapsedMilliseconds} ms. Error: {ex.Message}");
            }
            finally
            {
                IsExecutingTrade = false;
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
