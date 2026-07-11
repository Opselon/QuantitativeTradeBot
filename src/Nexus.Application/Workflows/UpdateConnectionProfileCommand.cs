using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Security;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Workflows
{
    public class UpdateConnectionProfileCommand
    {
        private readonly ISecretStore _secretStore;

        public UpdateConnectionProfileCommand(ISecretStore secretStore)
        {
            _secretStore = secretStore;
        }

        public async Task ExecuteAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(profile.ProfileName))
                throw new ArgumentException("Profile name cannot be empty.", nameof(profile));

            if (!string.IsNullOrEmpty(profile.Password))
            {
                await _secretStore.SaveSecretAsync($"MT5_PWD_{profile.ProfileName}", profile.Password, cancellationToken);
            }
            if (!string.IsNullOrEmpty(profile.InvestorPassword))
            {
                await _secretStore.SaveSecretAsync($"MT5_INV_PWD_{profile.ProfileName}", profile.InvestorPassword, cancellationToken);
            }
        }
    }
}
