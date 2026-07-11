using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Mt5Bridge.Contracts;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Application.Workflows;
using Nexus.Application.Workflows.DTOs;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAppConfigurationService _configService;
        private readonly ISecretStore _secretStore;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IMt5ConnectionService _connectionService;
        private readonly IMt5TradeService _tradeService;
        private IMt5Session? _session;

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

        // Trade Commands
        public ICommand PlaceOrderUICommand { get; }
        public ICommand ClosePositionUICommand { get; }
        public ICommand RefreshPositionsUICommand { get; }

        public MainViewModel(
            IAppConfigurationService configService,
            ISecretStore secretStore,
            IEnumerable<IDatabaseBootstrapper> bootstrappers,
            IMt5ConnectionService connectionService,
            IMt5AccountService accountService,
            IMt5TradeService tradeService,
            IDiagnosticService diagnosticService)
        {
            _configService = configService;
            _secretStore = secretStore;
            _diagnosticService = diagnosticService;
            _connectionService = connectionService;
            _tradeService = tradeService;

            // Instantiating application-layer commands
            _selectProviderCmd = new SelectPersistenceProviderCommand(_configService);
            _initDbCmd = new InitializeDatabaseCommand(bootstrappers, _configService);
            _migrateDbCmd = new MigrateDatabaseCommand(bootstrappers, _configService);
            _createProfileCmd = new CreateConnectionProfileCommand(_secretStore, _configService);
            _testConnectionCmd = new TestMt5ConnectionCommand(connectionService);
            _launchWorkspaceCmd = new LaunchWorkspaceCommand(_configService);

            // Load initial settings
            var settings = _configService.GetSettings();
            _isOnboarded = settings.IsOnboarded;
            _selectedProvider = settings.SelectedProvider;
            _connectionString = settings.ConnectionString;

            // Map profile properties if exists
            if (!string.IsNullOrEmpty(settings.ProfileName)) _profileName = settings.ProfileName;
            if (!string.IsNullOrEmpty(settings.BrokerServer)) _brokerServer = settings.BrokerServer;
            if (!string.IsNullOrEmpty(settings.LoginAccountId)) _loginAccountId = settings.LoginAccountId;
            if (!string.IsNullOrEmpty(settings.TerminalPath)) _terminalPath = settings.TerminalPath;
            _timeoutSeconds = settings.TimeoutSeconds;
            _autoReconnect = settings.AutoReconnect;
            _mt5Mode = settings.Mt5Mode;
            _mt5BridgeHost = settings.Mt5BridgeHost;
            _mt5BridgePort = settings.Mt5BridgePort;
            _mt5BridgeUseSsl = settings.Mt5BridgeUseSsl;

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
    }
}
