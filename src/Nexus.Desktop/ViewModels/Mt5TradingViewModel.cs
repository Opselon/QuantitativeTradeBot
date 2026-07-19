using Nexus.Application.Ports;
using Nexus.Desktop.Models;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.Mt5Bridge;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Nexus.Desktop.ViewModels
{
    public class Mt5TradingViewModel : ViewModelBase, IDisposable
    {
        private readonly IMt5OperatorService _operatorService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IAppConfigurationService _configService;
        private readonly MarketDataPipeline _pipeline;

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

        // Preset list of standard selectable symbols
        private ObservableCollection<string> _availableSymbols = new()
        {
            "EURUSD", "GBPUSD", "USDJPY", "AUDUSD", "USDCAD", "USDCHF", "NZDUSD", "XAUUSD"
        };

        // --- Dynamic Virtual Risk Engine Fields (in Pips) ---
        private decimal _stopLossPips = 0;
        private decimal _takeProfitPips = 0;
        private decimal _trailingStopPips = 0;

        // Tracks peak favorable prices reached for accurate trailing stop trails
        private readonly ConcurrentDictionary<long, decimal> _peakPrices = new();

        private readonly CancellationTokenSource _cts = new();
        private readonly object _refreshLock = new();
        private bool _isRefreshingPositions;
        private readonly int _refreshIntervalSeconds = 5;

        private readonly ConcurrentDictionary<long, decimal> _positionMultipliers = new();

        public ObservableCollection<string> AvailableSymbols
        {
            get => _availableSymbols;
            set => SetProperty(ref _availableSymbols, value);
        }

        // --- Dynamic Risk Properties ---

        public decimal StopLossPips
        {
            get => _stopLossPips;
            set => SetProperty(ref _stopLossPips, value);
        }

        public decimal TakeProfitPips
        {
            get => _takeProfitPips;
            set => SetProperty(ref _takeProfitPips, value);
        }

        public decimal TrailingStopPips
        {
            get => _trailingStopPips;
            set => SetProperty(ref _trailingStopPips, value);
        }

        // --- Aggregated Real-time Summary Properties ---
        public decimal TotalProfit => OpenPositions.Sum(p => p.Profit);

        public double WinRate
        {
            get
            {
                if (OpenPositions.Count == 0) return 0.0;
                double wins = OpenPositions.Count(p => p.Profit > 0);
                return (wins / OpenPositions.Count) * 100.0;
            }
        }

        public int TotalWins => OpenPositions.Count(p => p.Profit > 0);
        public int TotalLosses => OpenPositions.Count(p => p.Profit < 0);
        public decimal TotalVolume => OpenPositions.Sum(p => p.Volume);

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
        public IAsyncRelayCommand CloseAllCommand { get; }
        public IAsyncRelayCommand CloseAllProfitsCommand { get; }
        public IAsyncRelayCommand CloseAllLossesCommand { get; }
        public ICommand ClearErrorCommand { get; }

        public Mt5TradingViewModel(
            IMt5OperatorService operatorService,
            IDiagnosticService diagnosticService,
            IAppConfigurationService configService,
            MarketDataPipeline pipeline)
        {
            _operatorService = operatorService ?? throw new ArgumentNullException(nameof(operatorService));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

            RefreshPositionsCommand = new AsyncRelayCommand(OnRefreshPositionsAsync);
            BuyCommand = new AsyncRelayCommand(OnBuyAsync, CanExecuteTrade);
            SellCommand = new AsyncRelayCommand(OnSellAsync, CanExecuteTrade);
            CloseCommand = new AsyncRelayCommand(OnCloseAsync, CanClosePosition);

            CloseAllCommand = new AsyncRelayCommand(OnCloseAllAsync, CanExecuteBatchClose);
            CloseAllProfitsCommand = new AsyncRelayCommand(OnCloseAllProfitsAsync, CanExecuteBatchClose);
            CloseAllLossesCommand = new AsyncRelayCommand(OnCloseAllLossesAsync, CanExecuteBatchClose);

            ClearErrorCommand = new RelayCommand(OnClearError);

            ValidateInputs();

            _pipeline.OnPipelineTickProcessed += OnLiveTickProcessed;

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

        private bool CanExecuteBatchClose()
        {
            return OpenPositions.Count > 0 && !IsBusy && !IsExecutingTrade;
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
                }
            }
        }

        /// <summary>
        /// Dynamic pip size calculator based on asset specifications.
        /// </summary>
        private decimal GetPipSize(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return 0.0001m;
            string sym = symbol.ToUpper();
            if (sym.Contains("JPY")) return 0.01m;
            if (sym.Contains("XAU") || sym.Contains("GOLD")) return 0.10m; // Gold pip scaling
            return 0.0001m; // Standard Forex Major Pip scaling
        }

        private void OnLiveTickProcessed(PriceTickEnvelope tick)
        {
            if (tick == null || OpenPositions.Count == 0) return;

            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                lock (_refreshLock)
                {
                    if (_isRefreshingPositions) return;
                }

                bool updatedAny = false;
                foreach (var pos in OpenPositions)
                {
                    if (string.Equals(pos.Symbol, tick.SymbolName, StringComparison.OrdinalIgnoreCase))
                    {
                        decimal pipSize = GetPipSize(pos.Symbol);
                        decimal currentPrice = string.Equals(pos.Side, "Buy", StringComparison.OrdinalIgnoreCase)
                            ? (decimal)tick.Bid
                            : (decimal)tick.Ask;

                        pos.CurrentPrice = currentPrice;

                        // --- C# TRAILING STOP LOGIC ---
                        if (TrailingStopPips > 0)
                        {
                            decimal trailingDistance = TrailingStopPips * pipSize;
                            decimal targetSl = 0;

                            if (string.Equals(pos.Side, "Buy", StringComparison.OrdinalIgnoreCase))
                            {
                                targetSl = Math.Round(currentPrice - trailingDistance, 5);

                                // Only trail upwards (to lock in profits/reduce risk)
                                if (targetSl > pos.StopLoss || pos.StopLoss == 0)
                                {
                                    pos.StopLoss = targetSl;
                                    var target = pos;

                                    // Dispatch modification command asynchronously to MT5
                                    Task.Run(async () =>
                                    {
                                        await _operatorService.ModifyPositionAsync(target.Ticket, target.Symbol, target.StopLoss, target.TakeProfit, _cts.Token);
                                    });
                                }
                            }
                            else if (string.Equals(pos.Side, "Sell", StringComparison.OrdinalIgnoreCase))
                            {
                                targetSl = Math.Round(currentPrice + trailingDistance, 5);

                                // Only trail downwards
                                if (targetSl < pos.StopLoss || pos.StopLoss == 0)
                                {
                                    pos.StopLoss = targetSl;
                                    var target = pos;

                                    Task.Run(async () =>
                                    {
                                        await _operatorService.ModifyPositionAsync(target.Ticket, target.Symbol, target.StopLoss, target.TakeProfit, _cts.Token);
                                    });
                                }
                            }
                        }

                        // Recalculate Profit
                        decimal priceDiff = pos.CurrentPrice - pos.OpenPrice;
                        if (string.Equals(pos.Side, "Sell", StringComparison.OrdinalIgnoreCase))
                        {
                            priceDiff = pos.OpenPrice - pos.CurrentPrice;
                        }

                        if (_positionMultipliers.TryGetValue((long)pos.Ticket, out decimal multiplier))
                        {
                            pos.Profit = priceDiff * pos.Volume * multiplier;
                            updatedAny = true;
                        }
                    }
                }

                if (updatedAny)
                {
                    NotifySummaryMetrics();
                }
            }));
        }

        private void NotifySummaryMetrics()
        {
            OnPropertyChanged(nameof(TotalProfit));
            OnPropertyChanged(nameof(WinRate));
            OnPropertyChanged(nameof(TotalWins));
            OnPropertyChanged(nameof(TotalLosses));
            OnPropertyChanged(nameof(TotalVolume));
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

                var tempMultipliers = new Dictionary<long, decimal>();
                foreach (var pos in positions)
                {
                    decimal volume = pos.Volume;
                    decimal openPrice = pos.OpenPrice;
                    decimal currentPrice = pos.CurrentPrice;
                    decimal profit = pos.Profit;

                    decimal priceDiff = currentPrice - openPrice;
                    if (string.Equals(pos.Side, "Sell", StringComparison.OrdinalIgnoreCase))
                    {
                        priceDiff = openPrice - currentPrice;
                    }

                    decimal multiplier = 100000m;
                    if (priceDiff != 0 && volume != 0)
                    {
                        multiplier = profit / (priceDiff * volume);
                        if (multiplier < 0) multiplier = Math.Abs(multiplier);
                    }
                    tempMultipliers[pos.Ticket] = multiplier;
                }

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    OpenPositions.Clear();
                    _positionMultipliers.Clear();

                    foreach (var pos in positions)
                    {
                        OpenPositions.Add(new DesktopPositionViewModel(pos));
                        if (tempMultipliers.TryGetValue(pos.Ticket, out decimal mult))
                        {
                            _positionMultipliers[pos.Ticket] = mult;
                        }

                        if (!string.IsNullOrWhiteSpace(pos.Symbol) && !AvailableSymbols.Contains(pos.Symbol))
                        {
                            AvailableSymbols.Add(pos.Symbol);
                        }
                    }
                    NotifySummaryMetrics();
                });

                if (System.Windows.Application.Current == null)
                {
                    OpenPositions.Clear();
                    _positionMultipliers.Clear();
                    foreach (var pos in positions)
                    {
                        OpenPositions.Add(new DesktopPositionViewModel(pos));
                        if (tempMultipliers.TryGetValue(pos.Ticket, out decimal mult))
                        {
                            _positionMultipliers[pos.Ticket] = mult;
                        }

                        if (!string.IsNullOrWhiteSpace(pos.Symbol) && !AvailableSymbols.Contains(pos.Symbol))
                        {
                            AvailableSymbols.Add(pos.Symbol);
                        }
                    }
                    NotifySummaryMetrics();
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
                decimal? calculatedSl = StopLossPips > 0 ? StopLossPips : null;
                decimal? calculatedTp = TakeProfitPips > 0 ? TakeProfitPips : null;

                // Format comment to register trailing stop command inside the MT5 order comment
                string comment = "Operator Manual Trade";
                if (TrailingStopPips > 0)
                {
                    comment = $"TS:{TrailingStopPips}";
                }

                var result = await _operatorService.PlaceOrderAsync(
                    SelectedSymbol,
                    side,
                    OrderVolume,
                    calculatedSl,
                    calculatedTp,
                    comment, // Passed comment
                    _cts.Token);

                if (result.IsSuccess)
                {
                    StatusMessage = $"Trade executed successfully. Ticket: {result.Ticket}";
                    _diagnosticService.Log("Mt5Operator", "INFO", $"{side} {SelectedSymbol} {OrderVolume} lots completed in {stopwatch.ElapsedMilliseconds} ms. Success: True, Ticket: {result.Ticket}, SL Pips: {calculatedSl}, TP Pips: {calculatedTp}");

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

        private async Task OnCloseAllAsync()
        {
            await ExecuteBatchCloseAsync(p => true, "Close All");
        }

        private async Task OnCloseAllProfitsAsync()
        {
            await ExecuteBatchCloseAsync(p => p.Profit > 0, "Close All Profits");
        }

        private async Task OnCloseAllLossesAsync()
        {
            await ExecuteBatchCloseAsync(p => p.Profit < 0, "Close All Losses");
        }

        private async Task ExecuteBatchCloseAsync(Func<DesktopPositionViewModel, bool> predicate, string operationName)
        {
            var targets = OpenPositions.Where(predicate).ToList();
            if (targets.Count == 0)
            {
                StatusMessage = $"No active positions matched criteria for: {operationName}.";
                return;
            }

            IsExecutingTrade = true;
            IsBusy = true;
            ErrorMessage = string.Empty;
            StatusMessage = $"Executing {operationName} batch close for {targets.Count} targets...";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int succeededCount = 0;
            int failedCount = 0;

            try
            {
                foreach (var pos in targets)
                {
                    var result = await _operatorService.ClosePositionAsync(pos.Ticket, pos.Symbol, _cts.Token);
                    if (result.IsSuccess)
                    {
                        succeededCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }

                StatusMessage = $"Batch {operationName} completed. Success: {succeededCount}, Failures: {failedCount}.";
                _diagnosticService.Log("Mt5Operator", "INFO", $"Batch {operationName} execution completed in {stopwatch.ElapsedMilliseconds} ms. Succeeded: {succeededCount}, Failed: {failedCount}");

                await OnRefreshPositionsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                StatusMessage = $"Unexpected failure during batch {operationName}.";
                _diagnosticService.Log("Mt5Operator", "ERROR", $"Batch {operationName} failed: {ex.Message}");
            }
            finally
            {
                IsExecutingTrade = false;
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            if (_pipeline != null)
            {
                _pipeline.OnPipelineTickProcessed -= OnLiveTickProcessed;
            }
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}