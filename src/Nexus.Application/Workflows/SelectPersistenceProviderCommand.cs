using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class SelectPersistenceProviderCommand
    {
        private readonly IAppConfigurationService _configService;

        public SelectPersistenceProviderCommand(IAppConfigurationService configService)
        {
            _configService = configService;
        }

        public Task ExecuteAsync(string provider, string connectionString, CancellationToken cancellationToken = default)
        {
            var settings = _configService.GetSettings();
            settings.SelectedProvider = provider;
            settings.ConnectionString = connectionString;
            _configService.SaveSettings(settings);
            return Task.CompletedTask;
        }
    }
}
