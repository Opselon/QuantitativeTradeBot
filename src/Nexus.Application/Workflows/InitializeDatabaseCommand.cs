using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class InitializeDatabaseCommand
    {
        private readonly IEnumerable<IDatabaseBootstrapper> _bootstrappers;
        private readonly IAppConfigurationService _configService;

        public InitializeDatabaseCommand(IEnumerable<IDatabaseBootstrapper> bootstrappers, IAppConfigurationService configService)
        {
            _bootstrappers = bootstrappers;
            _configService = configService;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var settings = _configService.GetSettings();
            foreach (var bootstrapper in _bootstrappers)
            {
                if (bootstrapper.ProviderName.Equals(settings.SelectedProvider, System.StringComparison.OrdinalIgnoreCase))
                {
                    await bootstrapper.InitializeDatabaseAsync(settings.ConnectionString, cancellationToken);
                    return;
                }
            }
            throw new InvalidOperationException($"No bootstrapper found for provider: {settings.SelectedProvider}");
        }
    }
}
