using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Workflows
{
    public class PersistenceOptionDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class GetPersistenceOptionsQuery
    {
        private readonly IAppConfigurationService _configService;

        public GetPersistenceOptionsQuery(IAppConfigurationService configService)
        {
            _configService = configService;
        }

        public Task<List<PersistenceOptionDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var settings = _configService.GetSettings();
            var options = new List<PersistenceOptionDto>
            {
                new() {
                    ProviderName = "PostgreSQL",
                    IsRecommended = true,
                    Description = "Recommended for production workloads. Requires a running PostgreSQL server.",
                    IsSelected = settings.SelectedProvider == "PostgreSQL"
                },
                new() {
                    ProviderName = "SQLite",
                    IsRecommended = false,
                    Description = "Supported for quick start and local evaluation. Self-contained database file.",
                    IsSelected = settings.SelectedProvider == "SQLite"
                }
            };
            return Task.FromResult(options);
        }
    }
}
