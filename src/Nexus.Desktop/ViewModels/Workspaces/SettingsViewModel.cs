using System;
using Nexus.Application.Ports;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IAppConfigurationService _configService;

        public string SelectedProvider => _configService.GetSettings().SelectedProvider;

        public SettingsViewModel(IAppConfigurationService configService)
        {
            _configService = configService;
        }
    }
}
