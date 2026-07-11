using System.Threading;
using System.Threading.Tasks;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Workflows
{
    public class TestMt5ConnectionCommand
    {
        private readonly IMt5ConnectionService _connectionService;

        public TestMt5ConnectionCommand(IMt5ConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        public async Task<ConnectionTestResultDto> ExecuteAsync(ConnectionProfileDto profile, CancellationToken cancellationToken = default)
        {
            return await _connectionService.TestConnectionAsync(profile, cancellationToken);
        }
    }
}
