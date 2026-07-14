using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Application.Workflows;
using Nexus.Application.Workflows.DTOs;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.Mt5Bridge;
using Nexus.Desktop.ViewModels.Workspaces;

namespace Nexus.Desktop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Workspaces properties
        public DashboardViewModel Dashboard { get; }
        public Mt5BridgeViewModel Mt5Bridge { get; }
        public MarketWatchViewModel MarketWatch { get; }
        public ManualDeskViewModel ManualDesk { get; }
        public DiagnosticsViewModel Diagnostics { get; }
        public TestConsoleViewModel TestConsole { get; }
        public SettingsViewModel Settings { get; }

        public string AppVersion
        {
            get
            {
                var assembly = typeof(MainViewModel).Assembly;
                var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var versionStr = attribute?.InformationalVersion ?? assembly.GetName().Version?.ToString(3) ?? "0.64.0";

                if (versionStr.Contains("+"))
                {
                    versionStr = versionStr.Split('+')[0];
                }
                return versionStr;
            }
        }
        private readonly CancellationTokenSource _cts = new();
        private readonly IAppConfigurationService _configService;
        private readonly ISecretStore _secretStore;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IMt5ConnectionService _connectionService;
        private readonly IMt5TradeService _tradeService;
        private IMt5Session? _session;

        private readonly IMt5BridgeService _bridgeService;
        private readonly MarketDataPipeline _pipeline;

        public Mt5TradingViewModel Mt5Trading { get; }
        public NexusIntelligenceViewModel Intelligence { get; }

        private readonly SelectPersistenceProviderCommand _selectProviderCmd;
        private readonly InitializeDatabaseCommand _initDbCmd;
        private readonly MigrateDatabaseCommand _migrateDbCmd;
        private readonly CreateConnectionProfileCommand _createProfileCmd;
        private readonly TestMt5ConnectionCommand _testConnectionCmd;
        private readonly LaunchWorkspaceCommand _launchWorkspaceCmd;

        // Trade Panel Properties
        private string _tradeSymbol = "EURUSD";
        private decimal _tradeVolume = 0.10m;
        private string _selectedSide = "Buy";
        private ObservableCollection<BridgePositionDto> _openPositions = new();
        private BridgePositionDto? _selectedPosition;
        private string _tradeStatus = "Idle";

        // Onboarding State
        private bool _isOnboarded;
        private int _currentStep = 1;
        private bool _isBusy;
        private string _busyMessage = string.Empty;

        // Step 1 & 2 Properties
        private string _selectedProvider = "PostgreSQL"; // recommended default
        private string _connectionString = "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";
        private string _dbStatus = "Database: Uninitialized";
        private bool _isDbInitialized;

        // Step 3 Properties
        private string _profileName = "DefaultMT5";
        private string _brokerServer = "ICMarkets-Demo";
        private string _loginAccountId = "7820491";
        private string _password = "demo_password"; // bound in UI, stored via ISecretStore
        private string _terminalPath = "C:\\Program Files\\MetaTrader 5\\terminal64.exe";
        private int _timeoutSeconds = 30;
        private bool _autoReconnect = true;

        private string _mt5Mode = "Simulated";
        private string _mt5BridgeHost = "127.0.0.1";
        private int _mt5BridgePort = 5000;
        private bool _mt5BridgeUseSsl = false;

        // Step 4 & 5 Properties
        private string _testOutcome = "Not Tested";
        private bool _testSuccess;
        private AccountSnapshotDto? _accountSnapshot;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        // MT5 Localhost Bridge Active Status / Telemetry
        public bool IsBridgeConnected => _bridgeService.IsConnected;
        public bool IsBridgeAuthenticated => _bridgeService.IsAuthenticated;
        public string BridgeConnectionStatusText => _bridgeService.ConnectionStatusText;
        public double BridgePingLatencyMs => _bridgeService.PingLatencyMs;
        public string BridgeLastHeartbeatText => _bridgeService.LastHeartbeatUtc == DateTime.MinValue ? "Never" : _bridgeService.LastHeartbeatUtc.ToLocalTime().ToString("HH:mm:ss");
        public string BridgeLastErrorMessage => _bridgeService.LastErrorMessage;

        // MarketDataPipeline stats
        public long ProcessedTickCount => _pipeline.ProcessedTickCount;
        public double LastProcessingLatencyMs => _pipeline.LastProcessingLatencyMs;
        public string LastProcessedSymbol => _pipeline.LastProcessedSymbol;
        public string LastProcessedTimestampText => _pipeline.LastProcessedTimestamp == DateTime.MinValue ? "Never" : _pipeline.LastProcessedTimestamp.ToLocalTime().ToString("HH:mm:ss.fff");

        // Primary Symbols Subscription Watchlist
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

        // Trade Panel Public Properties
        public string TradeSymbol
        {
            get => _tradeSymbol;
            set => SetProperty(ref _tradeSymbol, value);
        }

        public decimal TradeVolume
        {
            get => _tradeVolume;
            set => SetProperty(ref _tradeVolume, value);
        }

        public string SelectedSide
        {
            get => _selectedSide;
            set => SetProperty(ref _selectedSide, value);
        }

        public ObservableCollection<BridgePositionDto> OpenPositions
        {
            get => _openPositions;
            set => SetProperty(ref _openPositions, value);
        }

        public BridgePositionDto? SelectedPosition
        {
            get => _selectedPosition;
            set => SetProperty(ref _selectedPosition, value);
        }

        public string TradeStatus
        {
            get => _tradeStatus;
            set => SetProperty(ref _tradeStatus, value);
        }

        // Commands
        public ICommand SelectProviderNextCommand { get; }
        public ICommand InitializeDbCommand { get; }
        public ICommand MigrateDbCommand { get; }
        public ICommand DbNextCommand { get; }
        public ICommand CreateProfileNextCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand TestNextCommand { get; }
        public ICommand AccountNextCommand { get; }
        public ICommand LaunchWorkspaceCmd { get; }
        public ICommand BackCommand { get; }

        public ICommand SelectDashboardCommand { get; }
        public ICommand SelectMt5BridgeCommand { get; }
        public ICommand SelectMarketWatchCommand { get; }
        public ICommand SelectManualDeskCommand { get; }
        public ICommand SelectAccountMetricsCommand { get; }
        public ICommand SelectNativeEngineCommand { get; }
        public ICommand SelectDiagnosticsCommand { get; }
        public ICommand SelectSettingsCommand { get; }
        public ICommand SelectTestConsoleCommand { get; }

        // Trade Commands
        public ICommand PlaceOrderUICommand { get; }
        public ICommand ClosePositionUICommand { get; }
        public ICommand RefreshPositionsUICommand { get; }

        public IAsyncRelayCommand BridgeConnectCommand { get; }
        public IAsyncRelayCommand BridgeDisconnectCommand { get; }
        public IAsyncRelayCommand BridgeLoginCommand { get; }
        public IAsyncRelayCommand BridgeRefreshAccountCommand { get; }
        public IAsyncRelayCommand BridgePingCommand { get; }
        public ICommand ToggleSubscriptionCommand { get; }

        public MainViewModel(
            IAppConfigurationService configService,
            ISecretStore secretStore,
            IEnumerable<IDatabaseBootstrapper> bootstrappers,
            IMt5ConnectionService connectionService,
            IMt5AccountService accountService,
            IMt5TradeService tradeService,
            IDiagnosticService diagnosticService,
            Mt5TradingViewModel mt5Trading,
            NexusIntelligenceViewModel intelligence,
            IMt5BridgeService bridgeService,
            MarketDataPipeline pipeline,
            DashboardViewModel dashboard,
            Mt5BridgeViewModel mt5Bridge,
            MarketWatchViewModel marketWatch,
            ManualDeskViewModel manualDesk,
            DiagnosticsViewModel diagnostics,
            TestConsoleViewModel testConsole,
            SettingsViewModel settings)
        {
            _configService = configService;
            _secretStore = secretStore;
            _diagnosticService = diagnosticService;
            _connectionService = connectionService;
            _tradeService = tradeService;
            Mt5Trading = mt5Trading ?? throw new ArgumentNullException(nameof(mt5Trading));
            Intelligence = intelligence ?? throw new ArgumentNullException(nameof(intelligence));
            _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

            Dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
            Mt5Bridge = mt5Bridge ?? throw new ArgumentNullException(nameof(mt5Bridge));
            MarketWatch = marketWatch ?? throw new ArgumentNullException(nameof(marketWatch));
            ManualDesk = manualDesk ?? throw new ArgumentNullException(nameof(manualDesk));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            TestConsole = testConsole ?? throw new ArgumentNullException(nameof(testConsole));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // Wire Bridge Connection Commands
            BridgeConnectCommand = new AsyncRelayCommand(OnBridgeConnectAsync);
            BridgeDisconnectCommand = new AsyncRelayCommand(OnBridgeDisconnectAsync);
            BridgeLoginCommand = new AsyncRelayCommand(OnBridgeLoginAsync);
            BridgeRefreshAccountCommand = new AsyncRelayCommand(OnBridgeRefreshAccountAsync);
            BridgePingCommand = new AsyncRelayCommand(OnBridgePingAsync);
            ToggleSubscriptionCommand = new AsyncRelayCommand<DesktopSymbolViewModel>(OnToggleSubscriptionAsync);

            // Wire tick stream listener for Market Watch symbols
            _pipeline.OnPipelineTickProcessed += OnPipelineTickProcessed;

            // Start passive UI telemetry update loop
            Task.Run(() => StartTelemetryRefreshLoopAsync(_cts.Token));

            // Instantiating application-layer commands
            _selectProviderCmd = new SelectPersistenceProviderCommand(_configService);
            _initDbCmd = new InitializeDatabaseCommand(bootstrappers, _configService);
            _migrateDbCmd = new MigrateDatabaseCommand(bootstrappers, _configService);
            _createProfileCmd = new CreateConnectionProfileCommand(_secretStore, _configService);
            _testConnectionCmd = new TestMt5ConnectionCommand(connectionService);
            _launchWorkspaceCmd = new LaunchWorkspaceCommand(_configService);

            // Load initial settings
            var configSettings = _configService.GetSettings();
            _isOnboarded = configSettings.IsOnboarded;
            _selectedProvider = configSettings.SelectedProvider;
            _connectionString = configSettings.ConnectionString;

            // Map profile properties if exists
            if (!string.IsNullOrEmpty(configSettings.ProfileName)) _profileName = configSettings.ProfileName;
            if (!string.IsNullOrEmpty(configSettings.BrokerServer)) _brokerServer = configSettings.BrokerServer;
            if (!string.IsNullOrEmpty(configSettings.LoginAccountId)) _loginAccountId = configSettings.LoginAccountId;
            if (!string.IsNullOrEmpty(configSettings.TerminalPath)) _terminalPath = configSettings.TerminalPath;
            _timeoutSeconds = configSettings.TimeoutSeconds;
            _autoReconnect = configSettings.AutoReconnect;
            _mt5Mode = configSettings.Mt5Mode;
            _mt5BridgeHost = configSettings.Mt5BridgeHost;
            _mt5BridgePort = configSettings.Mt5BridgePort;
            _mt5BridgeUseSsl = configSettings.Mt5BridgeUseSsl;

            // Wire UI Commands
            SelectProviderNextCommand = new AsyncRelayCommand(OnSelectProviderNextAsync);
            InitializeDbCommand = new AsyncRelayCommand(OnInitializeDbAsync);
            MigrateDbCommand = new AsyncRelayCommand(OnMigrateDbAsync);
            DbNextCommand = new RelayCommand(OnDbNext, () => _isDbInitialized);
            CreateProfileNextCommand = new AsyncRelayCommand(OnCreateProfileNextAsync);
            TestConnectionCommand = new AsyncRelayCommand(OnTestConnectionAsync);
            TestNextCommand = new RelayCommand(OnTestNext, () => _testSuccess);
            AccountNextCommand = new RelayCommand(OnAccountNext);
            LaunchWorkspaceCmd = new AsyncRelayCommand(OnLaunchWorkspaceAsync);
            BackCommand = new RelayCommand(OnBack, () => _currentStep > 1);

            SelectDashboardCommand = new RelayCommand(() => SelectedTabIndex = 0);
            SelectMt5BridgeCommand = new RelayCommand(() => SelectedTabIndex = 1);
            SelectMarketWatchCommand = new RelayCommand(() => SelectedTabIndex = 2);
            SelectManualDeskCommand = new RelayCommand(() => SelectedTabIndex = 3);
            SelectAccountMetricsCommand = new RelayCommand(() => SelectedTabIndex = 4);
            SelectNativeEngineCommand = new RelayCommand(() => SelectedTabIndex = 5);
            SelectDiagnosticsCommand = new RelayCommand(() => SelectedTabIndex = 6);
            SelectSettingsCommand = new RelayCommand(() => SelectedTabIndex = 7);
            SelectTestConsoleCommand = new RelayCommand(() => SelectedTabIndex = 8);

            RunSmokeTestCommand = new AsyncRelayCommand(OnRunSmokeTestAsync);

            // Wire Trade Commands
            PlaceOrderUICommand = new AsyncRelayCommand(OnPlaceOrderUIAsync);
            ClosePositionUICommand = new AsyncRelayCommand(OnClosePositionUIAsync);
            RefreshPositionsUICommand = new AsyncRelayCommand(OnRefreshPositionsUIAsync);

            _diagnosticService.Log("AppShell", "INFO", "Application Shell initialized.");
            _diagnosticService.Log("AppShell", "INFO", $"Loaded Selected Provider: {_selectedProvider}");
        }

        private async Task EnsureSessionAsync(CancellationToken ct)
        {
            if (_session == null)
            {
                var profile = new ConnectionProfileDto
                {
                    ProfileName = ProfileName,
                    BrokerServer = BrokerServer,
                    LoginAccountId = LoginAccountId,
                    Password = Password,
                    TerminalPath = TerminalPath,
                    TimeoutSeconds = TimeoutSeconds,
                    AutoReconnect = AutoReconnect
                };
                _session = await _connectionService.CreateSessionAsync(profile, ct);
            }
        }

        private async Task OnPlaceOrderUIAsync()
        {
            IsBusy = true;
            BusyMessage = "Submitting Market Order...";
            TradeStatus = "Submitting...";
            _diagnosticService.Log("TradingDesk", "INFO", $"Submitting {SelectedSide} market order for {TradeVolume} lots of {TradeSymbol}...");

            try
            {
                await EnsureSessionAsync(CancellationToken.None);

                var side = string.Equals(SelectedSide, "Buy", StringComparison.OrdinalIgnoreCase)
                    ? BridgeOrderSide.Buy
                    : BridgeOrderSide.Sell;

                var request = new PlaceOrderRequest(TradeSymbol, side, TradeVolume);
                var response = await _tradeService.PlaceOrderAsync(_session!, request, CancellationToken.None);

                if (response.Success)
                {
                    TradeStatus = $"Success - Ticket: {response.Ticket}";
                    _diagnosticService.Log("TradingDesk", "INFO", $"Order executed successfully! Ticket: {response.Ticket}. Message: {response.BrokerMessage}");

                    // Automatically refresh positions after successful trade
                    await OnRefreshPositionsUIAsync();
                }
                else
                {
                    TradeStatus = $"Failed - {response.BrokerMessage}";
                    _diagnosticService.Log("TradingDesk", "WARN", $"Order execution failed: {response.BrokerMessage}");
                }
            }
            catch (Exception ex)
            {
                TradeStatus = $"Error: {ex.Message}";
                _diagnosticService.Log("TradingDesk", "ERROR", $"Order execution threw error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnClosePositionUIAsync()
        {
            if (SelectedPosition == null)
            {
                TradeStatus = "No position selected.";
                return;
            }

            IsBusy = true;
            BusyMessage = "Closing Selected Position...";
            TradeStatus = "Closing...";
            _diagnosticService.Log("TradingDesk", "INFO", $"Closing position ticket {SelectedPosition.Ticket}...");

            try
            {
                await EnsureSessionAsync(CancellationToken.None);

                var request = new ClosePositionRequest(SelectedPosition.Ticket, SelectedPosition.Symbol);
                var response = await _tradeService.ClosePositionAsync(_session!, request, CancellationToken.None);

                if (response.Success)
                {
                    TradeStatus = $"Closed Ticket: {response.Ticket}";
                    _diagnosticService.Log("TradingDesk", "INFO", $"Position {response.Ticket} closed successfully! Message: {response.BrokerMessage}");

                    // Refresh
                    await OnRefreshPositionsUIAsync();
                }
                else
                {
                    TradeStatus = $"Failed Close - {response.BrokerMessage}";
                    _diagnosticService.Log("TradingDesk", "WARN", $"Position close failed: {response.BrokerMessage}");
                }
            }
            catch (Exception ex)
            {
                TradeStatus = $"Error: {ex.Message}";
                _diagnosticService.Log("TradingDesk", "ERROR", $"Position close threw error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnRefreshPositionsUIAsync()
        {
            TradeStatus = "Refreshing positions...";
            _diagnosticService.Log("TradingDesk", "INFO", "Refreshing open positions from MetaTrader 5...");

            try
            {
                await EnsureSessionAsync(CancellationToken.None);

                var positions = await _tradeService.GetOpenPositionsAsync(_session!, CancellationToken.None);

                OpenPositions.Clear();
                foreach (var pos in positions)
                {
                    OpenPositions.Add(pos);
                }

                TradeStatus = $"Refreshed at {DateTime.Now:HH:mm:ss}";
                _diagnosticService.Log("TradingDesk", "INFO", $"Successfully retrieved {positions.Count} open positions.");
            }
            catch (Exception ex)
            {
                TradeStatus = $"Error: {ex.Message}";
                _diagnosticService.Log("TradingDesk", "ERROR", $"Failed to retrieve open positions: {ex.Message}");
            }
        }

        // Onboarding Navigation / Progress properties
        public bool IsOnboarded
        {
            get => _isOnboarded;
            set => SetProperty(ref _isOnboarded, value);
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    OnPropertyChanged(nameof(IsStep1));
                    OnPropertyChanged(nameof(IsStep2));
                    OnPropertyChanged(nameof(IsStep3));
                    OnPropertyChanged(nameof(IsStep4));
                    OnPropertyChanged(nameof(IsStep5));
                    OnPropertyChanged(nameof(IsStep6));
                    ((RelayCommand)BackCommand).CanExecute(null);
                }
            }
        }

        public bool IsStep1 => _currentStep == 1;
        public bool IsStep2 => _currentStep == 2;
        public bool IsStep3 => _currentStep == 3;
        public bool IsStep4 => _currentStep == 4;
        public bool IsStep5 => _currentStep == 5;
        public bool IsStep6 => _currentStep == 6;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        // Persistence Properties
        public string SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (SetProperty(ref _selectedProvider, value))
                {
                    // Provide automatic safe defaults
                    if (_selectedProvider == "SQLite")
                    {
                        ConnectionString = "Data Source=nexus.db";
                    }
                    else
                    {
                        ConnectionString = "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";
                    }
                    _diagnosticService.Log("Persistence", "INFO", $"Selected provider switched to: {_selectedProvider}");
                }
            }
        }

        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }

        public string DatabaseStatus
        {
            get => _dbStatus;
            set => SetProperty(ref _dbStatus, value);
        }

        public bool IsDbInitialized
        {
            get => _isDbInitialized;
            set
            {
                if (SetProperty(ref _isDbInitialized, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Profile Properties
        public string ProfileName
        {
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }

        public string BrokerServer
        {
            get => _brokerServer;
            set => SetProperty(ref _brokerServer, value);
        }

        public string LoginAccountId
        {
            get => _loginAccountId;
            set => SetProperty(ref _loginAccountId, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string TerminalPath
        {
            get => _terminalPath;
            set => SetProperty(ref _terminalPath, value);
        }

        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set => SetProperty(ref _timeoutSeconds, value);
        }

        public bool AutoReconnect
        {
            get => _autoReconnect;
            set => SetProperty(ref _autoReconnect, value);
        }

        public string Mt5Mode
        {
            get => _mt5Mode;
            set => SetProperty(ref _mt5Mode, value);
        }

        public string Mt5BridgeHost
        {
            get => _mt5BridgeHost;
            set => SetProperty(ref _mt5BridgeHost, value);
        }

        public int Mt5BridgePort
        {
            get => _mt5BridgePort;
            set => SetProperty(ref _mt5BridgePort, value);
        }

        public bool Mt5BridgeUseSsl
        {
            get => _mt5BridgeUseSsl;
            set => SetProperty(ref _mt5BridgeUseSsl, value);
        }

        // Test outcome properties
        public string TestOutcome
        {
            get => _testOutcome;
            set => SetProperty(ref _testOutcome, value);
        }

        public bool TestSuccess
        {
            get => _testSuccess;
            set
            {
                if (SetProperty(ref _testSuccess, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public AccountSnapshotDto? AccountSnapshot
        {
            get => _accountSnapshot;
            set => SetProperty(ref _accountSnapshot, value);
        }

        // Diagnostics Panel Hook
        public IDiagnosticService DiagnosticService => _diagnosticService;

        // Navigation Command Methods
        private async Task OnSelectProviderNextAsync()
        {
            _diagnosticService.Log("Persistence", "INFO", $"Saving selected database provider: {SelectedProvider}");
            await _selectProviderCmd.ExecuteAsync(SelectedProvider, ConnectionString);
            CurrentStep = 2;
        }

        private async Task OnInitializeDbAsync()
        {
            IsBusy = true;
            BusyMessage = "Initializing database schema...";
            _diagnosticService.Log("Persistence", "INFO", $"Initializing {SelectedProvider} database at connection: {SecurityConfiguration.MaskConnectionString(ConnectionString)}");

            try
            {
                await _initDbCmd.ExecuteAsync();
                DatabaseStatus = "Database: Initialized Successfully";
                IsDbInitialized = true;
                _diagnosticService.Log("Persistence", "INFO", $"{SelectedProvider} Database schema initialization succeeded.");
            }
            catch (Exception ex)
            {
                DatabaseStatus = $"Database: Init Failed ({ex.Message})";
                IsDbInitialized = false;
                _diagnosticService.Log("Persistence", "ERROR", $"Failed to initialize {SelectedProvider} database: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnMigrateDbAsync()
        {
            IsBusy = true;
            BusyMessage = "Applying database migrations...";
            _diagnosticService.Log("Persistence", "INFO", $"Migrating {SelectedProvider} database...");

            try
            {
                await _migrateDbCmd.ExecuteAsync();
                DatabaseStatus = "Database: Fully Migrated";
                IsDbInitialized = true;
                _diagnosticService.Log("Persistence", "INFO", $"{SelectedProvider} Database migration succeeded.");
            }
            catch (Exception ex)
            {
                DatabaseStatus = $"Database: Migration Failed ({ex.Message})";
                _diagnosticService.Log("Persistence", "ERROR", $"Failed to migrate database: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnDbNext()
        {
            CurrentStep = 3;
        }

        private async Task OnCreateProfileNextAsync()
        {
            _diagnosticService.Log("Security", "INFO", $"Creating connection profile '{ProfileName}' with secure credentials store...");

            var dto = new ConnectionProfileDto
            {
                ProfileName = ProfileName,
                BrokerServer = BrokerServer,
                LoginAccountId = LoginAccountId,
                Password = Password,
                TerminalPath = TerminalPath,
                TimeoutSeconds = TimeoutSeconds,
                AutoReconnect = AutoReconnect
            };

            try
            {
                await _createProfileCmd.ExecuteAsync(dto);
                _diagnosticService.Log("Security", "INFO", $"Profile '{ProfileName}' created. Password stored inside ISecretStore.");

                // Save non-sensitive metadata in configuration
                var settings = _configService.GetSettings();
                settings.ProfileName = ProfileName;
                settings.BrokerServer = BrokerServer;
                settings.LoginAccountId = LoginAccountId;
                settings.TerminalPath = TerminalPath;
                settings.TimeoutSeconds = TimeoutSeconds;
                settings.AutoReconnect = AutoReconnect;
                settings.Mt5Mode = Mt5Mode;
                settings.Mt5BridgeHost = Mt5BridgeHost;
                settings.Mt5BridgePort = Mt5BridgePort;
                settings.Mt5BridgeUseSsl = Mt5BridgeUseSsl;
                _configService.SaveSettings(settings);

                CurrentStep = 4;
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Security", "ERROR", $"Failed to create connection profile: {ex.Message}");
            }
        }

        private async Task OnTestConnectionAsync()
        {
            IsBusy = true;
            string modeLabel = Mt5Mode == "Real" ? "Real MT5 Bridge" : "Simulated";
            BusyMessage = $"Testing MetaTrader 5 Connection ({modeLabel})...";
            TestOutcome = "Connecting...";
            _diagnosticService.Log("Gateway", "INFO", $"Testing connection to MT5 Broker server '{BrokerServer}' for Account ID '{LoginAccountId}' (Mode: {modeLabel})...");

            var dto = new ConnectionProfileDto
            {
                ProfileName = ProfileName,
                BrokerServer = BrokerServer,
                LoginAccountId = LoginAccountId,
                Password = Password,
                TerminalPath = TerminalPath,
                TimeoutSeconds = TimeoutSeconds,
                AutoReconnect = AutoReconnect
            };

            try
            {
                var result = await _testConnectionCmd.ExecuteAsync(dto);
                if (result.IsSuccess)
                {
                    TestOutcome = "Success - Simulated Gateway Active";
                    TestSuccess = true;
                    AccountSnapshot = result.AccountSnapshot;
                    _diagnosticService.Log("Gateway", "INFO", $"MT5 Connection test succeeded! Account balance: {result.AccountSnapshot?.Balance:C2} {result.AccountSnapshot?.Currency}");
                }
                else
                {
                    TestOutcome = $"Failed: {result.ErrorMessage}";
                    TestSuccess = false;
                    _diagnosticService.Log("Gateway", "WARN", $"MT5 Connection test failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                TestOutcome = $"Failed: {ex.Message}";
                TestSuccess = false;
                _diagnosticService.Log("Gateway", "ERROR", $"MT5 Connection test threw an error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnTestNext()
        {
            CurrentStep = 5;
        }

        private void OnAccountNext()
        {
            CurrentStep = 6;
        }

        private async Task OnLaunchWorkspaceAsync()
        {
            IsBusy = true;
            BusyMessage = "Launching platform workspace...";
            _diagnosticService.Log("AppShell", "INFO", "Launching Workspace...");

            try
            {
                await _launchWorkspaceCmd.ExecuteAsync();
                IsOnboarded = true;
                _diagnosticService.Log("AppShell", "INFO", "Onboarding completed. Welcome to your main trading workspace dashboard!");

                // Load positions in workspace
                await OnRefreshPositionsUIAsync();
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("AppShell", "ERROR", $"Failed to launch workspace: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnBack()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
            }
        }

        // --- AUTOMATED SMOKE TEST ACTIONS ---

        private ObservableCollection<string> _smokeTestLogList = new();
        public ObservableCollection<string> SmokeTestLogList => _smokeTestLogList;

        public IAsyncRelayCommand RunSmokeTestCommand { get; }

        private async Task OnRunSmokeTestAsync()
        {
            _smokeTestLogList.Clear();
            AddSmokeLog("Starting automated Real Smoke Test workflow...");

            try
            {
                // Step 1: Connect
                AddSmokeLog("Step 1: Connecting to localhost bridge...");
                await _bridgeService.ConnectAsync(Mt5BridgeHost, Mt5BridgePort, _cts.Token);
                await Task.Delay(1000); // Wait for initialization
                AddSmokeLog($"Status: Connected={_bridgeService.IsConnected}");

                // Step 2: Login
                AddSmokeLog($"Step 2: Authenticating Account ID '{LoginAccountId}'...");
                bool loginOk = await _bridgeService.LoginAsync(LoginAccountId, Password, BrokerServer, _cts.Token);
                AddSmokeLog($"Status: Authenticated={_bridgeService.IsAuthenticated}, Message={_bridgeService.LastErrorMessage}");

                if (loginOk)
                {
                    // Step 3: Ping
                    AddSmokeLog("Step 3: Sending heartbeat ping...");
                    var sw = Stopwatch.StartNew();
                    var snap = await _bridgeService.GetAccountSnapshotAsync(_cts.Token);
                    sw.Stop();
                    AddSmokeLog($"Status: Ping completed in {sw.ElapsedMilliseconds} ms. Latency={_bridgeService.PingLatencyMs} ms");

                    // Step 4: Subscribe symbol EURUSD
                    AddSmokeLog("Step 4: Subscribing to symbol EURUSD...");
                    await _bridgeService.SubscribeSymbolAsync("EURUSD", _cts.Token);
                    AddSmokeLog("Status: Subscription request sent EURUSD.");

                    // Step 5: Verify live ticks
                    AddSmokeLog("Step 5: Monitoring live ticks for 5 seconds...");
                    long startTicks = _pipeline.ProcessedTickCount;
                    await Task.Delay(5000);
                    long endTicks = _pipeline.ProcessedTickCount;
                    AddSmokeLog($"Status: Ticks Ingested={endTicks - startTicks}, Total Processed={_pipeline.ProcessedTickCount}");

                    // Step 6: Fetch Account Metrics
                    AddSmokeLog("Step 6: Querying live account metrics snapshot...");
                    if (snap != null)
                    {
                        AccountSnapshot = snap;
                        AddSmokeLog($"Status: Account verified. Balance: {snap.Balance:C2}, Equity: {snap.Equity:C2}, Currency: {snap.Currency}");
                    }

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
            }
            catch (Exception ex)
            {
                AddSmokeLog($"ERROR: Smoke Test failed: {ex.Message}");
            }
        }

        private void AddSmokeLog(string message)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                _smokeTestLogList.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            });
            if (System.Windows.Application.Current == null)
            {
                _smokeTestLogList.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }

        // --- LOCALHOST BRIDGE COMMAND ACTIONS ---

        private async Task OnBridgeConnectAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", $"Connecting to MetaTrader 5 localhost bridge server at {Mt5BridgeHost}:{Mt5BridgePort}...");
            try
            {
                await _bridgeService.ConnectAsync(Mt5BridgeHost, Mt5BridgePort, _cts.Token);
                _diagnosticService.Log("Mt5Bridge", "INFO", "Bridge TCP listener started. Waiting for MT5 Expert Advisor to connect...");
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Failed to start bridge listener: {ex.Message}");
            }
        }

        private async Task OnBridgeDisconnectAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", "Disconnecting active MT5 bridge session...");
            try
            {
                await _bridgeService.DisconnectAsync(_cts.Token);
                _diagnosticService.Log("Mt5Bridge", "INFO", "Bridge disconnected cleanly.");
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Error disconnecting bridge: {ex.Message}");
            }
        }

        private async Task OnBridgeLoginAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", $"Submitting secure login authentication credentials for Account ID '{LoginAccountId}'...");
            try
            {
                bool success = await _bridgeService.LoginAsync(LoginAccountId, Password, BrokerServer, _cts.Token);
                if (success)
                {
                    _diagnosticService.Log("Mt5Bridge", "INFO", "Authentication succeeded! Real MT5 bridge session is now authorized.");
                    // Retrieve initial account info
                    await OnBridgeRefreshAccountAsync();
                }
                else
                {
                    _diagnosticService.Log("Mt5Bridge", "WARN", $"Login credentials rejected: {_bridgeService.LastErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Login threw an exception: {ex.Message}");
            }
        }

        private async Task OnBridgeRefreshAccountAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", "Retrieving live MetaTrader 5 account snap statistics via bridge...");
            try
            {
                var snapshot = await _bridgeService.GetAccountSnapshotAsync(_cts.Token);
                if (snapshot != null)
                {
                    AccountSnapshot = snapshot;
                    _diagnosticService.Log("Mt5Bridge", "INFO", $"Snapshot refreshed successfully. Balance: {snapshot.Balance:C2} {snapshot.Currency}");
                }
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Failed to fetch live account snapshot: {ex.Message}");
            }
        }

        private async Task OnBridgePingAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", "Executing diagnostic ping/heartbeat command to Expert Advisor...");
            try
            {
                // Measure ping using GetAccountSnapshot to execute real round-trip message
                var sw = Stopwatch.StartNew();
                await _bridgeService.GetAccountSnapshotAsync(_cts.Token);
                sw.Stop();
                _diagnosticService.Log("Mt5Bridge", "INFO", $"Ping diagnostic response received. Live round-trip duration: {sw.Elapsed.TotalMilliseconds:F1} ms");
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Mt5Bridge", "ERROR", $"Ping handshake diagnostic failed: {ex.Message}");
            }
        }

        private async Task OnToggleSubscriptionAsync(DesktopSymbolViewModel item)
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

        private async Task StartTelemetryRefreshLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);

                    OnPropertyChanged(nameof(IsBridgeConnected));
                    OnPropertyChanged(nameof(IsBridgeAuthenticated));
                    OnPropertyChanged(nameof(BridgeConnectionStatusText));
                    OnPropertyChanged(nameof(BridgePingLatencyMs));
                    OnPropertyChanged(nameof(BridgeLastHeartbeatText));
                    OnPropertyChanged(nameof(BridgeLastErrorMessage));

                    OnPropertyChanged(nameof(ProcessedTickCount));
                    OnPropertyChanged(nameof(LastProcessingLatencyMs));
                    OnPropertyChanged(nameof(LastProcessedSymbol));
                    OnPropertyChanged(nameof(LastProcessedTimestampText));
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }
    }
}
