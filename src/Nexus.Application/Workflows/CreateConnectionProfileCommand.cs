using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Workflows
{
    public class CreateConnectionProfileCommand
    {
        private readonly ISecretStore _secretStore;
        private readonly IAppConfigurationService _configService;

        public CreateConnectionProfileCommand(ISecretStore secretStore, IAppConfigurationService configService)
        {
            _secretStore = secretStore;
            _configService = configService;
        }

        public async Task ExecuteAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(profile.ProfileName))
                throw new ArgumentException("Profile name cannot be empty.", nameof(profile));

            // Save sensitive passwords to Secure Store (DPAPI)
            if (!string.IsNullOrEmpty(profile.Password))
            {
                await _secretStore.SaveSecretAsync($"MT5_PWD_{profile.ProfileName}", profile.Password, cancellationToken);
            }
            if (!string.IsNullOrEmpty(profile.InvestorPassword))
            {
                await _secretStore.SaveSecretAsync($"MT5_INV_PWD_{profile.ProfileName}", profile.InvestorPassword, cancellationToken);
            }

            // In a real multi-profile scenario, we would persist profile metadata in configuration / DB.
            // For the desktop onboarding vertical slice, we can store it in AppSettings.
            var settings = _configService.GetSettings();
            _configService.SaveSettings(settings);
        }
    }
}
