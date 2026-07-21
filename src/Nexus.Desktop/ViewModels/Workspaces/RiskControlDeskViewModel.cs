using Nexus.Core.Entities;
using Nexus.Core.Enums;
using Nexus.Core.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    /// <summary>
    /// ViewModel for the interactive "Quantum Risk Control Desk" workspace.
    /// Provides thread-safe, direct reactive controls to customize high-frequency parameters at runtime.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Mutates: src/Nexus.Core/Interfaces/IPositionManagerSettingsProvider.cs
    /// - Displayed in: src/Nexus.Desktop/Views/Workspaces/RiskControlDeskView.xaml
    /// </remarks>
    public class RiskControlDeskViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly IPositionManagerSettingsProvider _settingsProvider;
        private bool _isAutoTradingEnabled;
        private ScalpingStrategyType _selectedStrategy;
        private double _marginDefenseThreshold;
        private double _noiseFloorMultiplier;
        private double _peakUtilityMultiplier;
        private double _initialSlMultiplier;
        private double _initialTpMultiplier;
        private int _cooldownSeconds;
        private double _hysteresisPips;
        private double _stage1TpPercent;
        private double _stage1VolPercent;
        private double _stage2TpPercent;
        private double _stage2VolPercent;
        #endregion

        #region Public Properties (Bindable to Sliders and Toggles)
        public bool IsAutoTradingEnabled
        {
            get => _isAutoTradingEnabled;
            set
            {
                if (_isAutoTradingEnabled != value)
                {
                    // Master Permission Confirmation Alert Dialog Guard
                    if (value)
                    {
                        var result = MessageBox.Show(
                            "CRITICAL WARNING: You are enabling Live Trading permissions to the Autonomous AI Core. " +
                            "This permits direct API routing and capital exposure on your broker account.\n\n" +
                            "Do you wish to authorize AI routing?",
                            "QUANTUM SECURITY GUARD AUTHORIZATION",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                        {
                            OnPropertyChanged(); // Restore UI state
                            return;
                        }
                    }

                    _isAutoTradingEnabled = value;
                    OnPropertyChanged();
                    PushSettingsUpdate();
                }
            }
        }

        public ScalpingStrategyType SelectedStrategy
        {
            get => _selectedStrategy;
            set { _selectedStrategy = value; OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double MarginDefenseThreshold
        {
            get => _marginDefenseThreshold;
            set { _marginDefenseThreshold = Math.Round(value, 1); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double NoiseFloorMultiplier
        {
            get => _noiseFloorMultiplier;
            set { _noiseFloorMultiplier = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double PeakUtilityMultiplier
        {
            get => _peakUtilityMultiplier;
            set { _peakUtilityMultiplier = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        /// <summary>
        /// Gets or sets the volatility multiplier applied when initializing protective Stop Loss boundaries on naked trades.
        /// Controls default hard stop distances relative to ATR volatility.
        /// </summary>
        /// <value>
        /// Variable Type: <see cref="double"/> (Default: 2.50).
        /// Triggers: Executes <see cref="PushSettingsUpdate"/> to refresh live risk bounds.
        /// </value>
        public double InitialSlMultiplier
        {
            get => _initialSlMultiplier;
            set { _initialSlMultiplier = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }



        /// <summary>
        /// Gets or sets the volatility multiplier applied when initializing target Take Profit boundaries on naked trades.
        /// Controls default target distances relative to ATR volatility.
        /// </summary>
        /// <value>
        /// Variable Type: <see cref="double"/> (Default: 4.50).
        /// Triggers: Executes <see cref="PushSettingsUpdate"/> to refresh live risk bounds.
        /// </value>
        public double InitialTpMultiplier
        {
            get => _initialTpMultiplier;
            set { _initialTpMultiplier = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }



        /// <summary>
        /// Gets or sets the rate-limiting cooldown delay in seconds enforced between consecutive order modifications.
        /// Protects MT5 server sockets from order spam rate-limits.
        /// </summary>
        /// <value>
        /// Variable Type: <see cref="int"/> (Seconds, Range: 5 to 120, Default: 15).
        /// Triggers: Executes <see cref="PushSettingsUpdate"/> to refresh live risk bounds.
        /// </value>
        public int CooldownSeconds
        {
            get => _cooldownSeconds;
            set { _cooldownSeconds = value; OnPropertyChanged(); PushSettingsUpdate(); }
        }


        /// <summary>
        /// Gets or sets the minimum protective price movement required in pips before modifying broker limits (Hysteresis defense).
        /// Prevents jittery Stop Loss adjustments on minor market ticks.
        /// </summary>
        /// <value>
        /// Variable Type: <see cref="double"/> (Pips, Range: 1.0 to 10.0, Default: 3.0).
        /// Triggers: Executes <see cref="PushSettingsUpdate"/> to refresh live risk bounds.
        /// </value>
        public double HysteresisPips
        {
            get => _hysteresisPips;
            set { _hysteresisPips = Math.Round(value, 1); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double Stage1TpPercent
        {
            get => _stage1TpPercent;
            set { _stage1TpPercent = Math.Round(value, 3); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double Stage1VolPercent
        {
            get => _stage1VolPercent;
            set { _stage1VolPercent = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double Stage2TpPercent
        {
            get => _stage2TpPercent;
            set { _stage2TpPercent = Math.Round(value, 3); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        public double Stage2VolPercent
        {
            get => _stage2VolPercent;
            set { _stage2VolPercent = Math.Round(value, 2); OnPropertyChanged(); PushSettingsUpdate(); }
        }

        // Collection to feed Strategy Selector ComboBox
        public ObservableCollection<ScalpingStrategyType> AvailableStrategies { get; }
        #endregion

        #region Commands
        public ICommand ResetDefaultsCommand { get; }
        #endregion

        #region Constructor
        public RiskControlDeskViewModel(IPositionManagerSettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));

            // Load strategy options
            AvailableStrategies = new ObservableCollection<ScalpingStrategyType>(
                (ScalpingStrategyType[])Enum.GetValues(typeof(ScalpingStrategyType))
            );

            // Fetch initial configuration state from the provider
            LoadActiveSettings();

            ResetDefaultsCommand = new RelayCommand(ExecuteResetDefaults);
        }
        #endregion

        #region Private Sync & Reset Methods
        /// <summary>
        /// Reads current active values from the atomic settings reference and maps them to UI properties.
        /// </summary>
        private void LoadActiveSettings()
        {
            var config = _settingsProvider.GetSettings();

            _isAutoTradingEnabled = config.IsAutoTradingEnabled;
            _selectedStrategy = config.ActiveStrategy;
            _marginDefenseThreshold = config.MarginDefenseThreshold;
            _noiseFloorMultiplier = config.NoiseFloorMultiplier;
            _peakUtilityMultiplier = config.PeakUtilityMultiplier;
            _initialSlMultiplier = config.InitialSlVolatilityMultiplier;
            _initialTpMultiplier = config.InitialTpVolatilityMultiplier;
            _cooldownSeconds = config.CooldownSeconds;
            _hysteresisPips = config.HysteresisPipThreshold;
            _stage1TpPercent = config.Stage1TpBalancePercent;
            _stage1VolPercent = config.Stage1CloseVolumePercent;
            _stage2TpPercent = config.Stage2TpBalancePercent;
            _stage2VolPercent = config.Stage2CloseVolumePercent;

            // Notify all WPF bindings
            OnPropertyChanged(string.Empty);
        }

        /// <summary>
        /// Bundles local UI states into a model and pushes it atomically back to the underlying Thread-safe Provider.
        /// </summary>
        private void PushSettingsUpdate()
        {
            var updatedConfig = new PositionManagerSettings
            {
                IsAutoTradingEnabled = this.IsAutoTradingEnabled,
                ActiveStrategy = this.SelectedStrategy,
                MarginDefenseThreshold = this.MarginDefenseThreshold,
                NoiseFloorMultiplier = this.NoiseFloorMultiplier,
                PeakUtilityMultiplier = this.PeakUtilityMultiplier,
                InitialSlVolatilityMultiplier = this.InitialSlMultiplier,
                InitialTpVolatilityMultiplier = this.InitialTpMultiplier,
                CooldownSeconds = this.CooldownSeconds,
                HysteresisPipThreshold = this.HysteresisPips,
                Stage1TpBalancePercent = this.Stage1TpPercent,
                Stage1CloseVolumePercent = this.Stage1VolPercent,
                Stage2TpBalancePercent = this.Stage2TpPercent,
                Stage2CloseVolumePercent = this.Stage2VolPercent
            };

            _settingsProvider.UpdateSettings(updatedConfig);
        }

        private void ExecuteResetDefaults()
        {
            var defaults = new PositionManagerSettings();
            _settingsProvider.UpdateSettings(defaults);
            LoadActiveSettings();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    #region Basic Command Helpers
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    #endregion
}