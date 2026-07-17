using Nexus.Execution.Domain;

namespace Nexus.Execution.Gateways
{
    public interface IExecutionGateway
    {
        Task<ExecutionResult> SubmitOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
        Task<ExecutionResult> ClosePositionAsync(string ticketId, double? volume = null, CancellationToken cancellationToken = default);
        Task<ExecutionResult> ModifyPositionAsync(string ticketId, double? sl = null, double? tp = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PositionSnapshot>> GetPositionsAsync(CancellationToken cancellationToken = default);
    }
}
