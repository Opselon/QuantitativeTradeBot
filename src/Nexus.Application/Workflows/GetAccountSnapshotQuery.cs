using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;

namespace Nexus.Application.Workflows
{
    public class GetAccountSnapshotQuery
    {
        private readonly IMt5AccountService _accountService;

        public GetAccountSnapshotQuery(IMt5AccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<AccountSnapshotDto> ExecuteAsync(IMt5Session session, CancellationToken cancellationToken = default)
        {
            return await _accountService.GetAccountSnapshotAsync(session, cancellationToken);
        }
    }
}
