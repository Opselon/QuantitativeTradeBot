using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;

namespace Nexus.Application.Workflows
{
    public class LaunchWorkspaceCommand
    {
        private readonly IAppConfigurationService _configService;

        public LaunchWorkspaceCommand(IAppConfigurationService configService)
        {
            _configService = configService;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var settings = _configService.GetSettings();
            settings.IsOnboarded = true;
            _configService.SaveSettings(settings);
            return Task.CompletedTask;
        }
    }
}
