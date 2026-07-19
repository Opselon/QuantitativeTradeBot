using Nexus.Application.Security;

namespace Nexus.Application.Workflows
{
    public class DeleteConnectionProfileCommand
    {
        private readonly ISecretStore _secretStore;

        public DeleteConnectionProfileCommand(ISecretStore secretStore)
        {
            _secretStore = secretStore;
        }

        public async Task ExecuteAsync(string profileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Profile name cannot be empty.", nameof(profileName));

            await _secretStore.DeleteSecretAsync($"MT5_PWD_{profileName}", cancellationToken);
            await _secretStore.DeleteSecretAsync($"MT5_INV_PWD_{profileName}", cancellationToken);
        }
    }
}
