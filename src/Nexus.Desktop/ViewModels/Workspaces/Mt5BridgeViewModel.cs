using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class Mt5BridgeViewModel : ViewModelBase
    {
        private readonly IMt5BridgeService _bridgeService;
        private readonly IAppConfigurationService _configService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly CancellationTokenSource _cts = new();

        private string _mt5BridgeHost = "127.0.0.1";
        private int _mt5BridgePort = 5000;
        private string _loginAccountId = "7820491";
        private string _password = "demo_password";
        private string _brokerServer = "ICMarkets-Demo";

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

        public string BrokerServer
        {
            get => _brokerServer;
            set => SetProperty(ref _brokerServer, value);
        }

        public bool IsEaPresentInRepository => _bridgeService.IsEaPresentInRepository;
        public string EaRepositoryFilePath => _bridgeService.EaRepositoryFilePath;
        public long EaRepositoryFileSize => _bridgeService.EaRepositoryFileSize;

        public ICommand BridgeConnectCommand { get; }
        public ICommand BridgeDisconnectCommand { get; }
        public ICommand BridgeLoginCommand { get; }

        public Mt5BridgeViewModel(
            IMt5BridgeService bridgeService,
            IAppConfigurationService configService,
            IDiagnosticService diagnosticService)
        {
            _bridgeService = bridgeService;
            _configService = configService;
            _diagnosticService = diagnosticService;

            BridgeConnectCommand = new AsyncRelayCommand(OnConnectAsync);
            BridgeDisconnectCommand = new AsyncRelayCommand(OnDisconnectAsync);
            BridgeLoginCommand = new AsyncRelayCommand(OnLoginAsync);

            // Load settings
            var settings = _configService.GetSettings();
            if (!string.IsNullOrEmpty(settings.Mt5BridgeHost)) _mt5BridgeHost = settings.Mt5BridgeHost;
            if (settings.Mt5BridgePort > 0) _mt5BridgePort = settings.Mt5BridgePort;
            if (!string.IsNullOrEmpty(settings.LoginAccountId)) _loginAccountId = settings.LoginAccountId;
            if (!string.IsNullOrEmpty(settings.BrokerServer)) _brokerServer = settings.BrokerServer;
        }

        private async Task OnConnectAsync()
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

        private async Task OnDisconnectAsync()
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

        private async Task OnLoginAsync()
        {
            _diagnosticService.Log("Mt5Bridge", "INFO", $"Submitting secure login credentials for Account ID '{LoginAccountId}'...");
            try
            {
                bool success = await _bridgeService.LoginAsync(LoginAccountId, Password, BrokerServer, _cts.Token);
                if (success)
                {
                    _diagnosticService.Log("Mt5Bridge", "INFO", "Authentication succeeded! Real MT5 bridge session is now authorized.");
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
    }
}
