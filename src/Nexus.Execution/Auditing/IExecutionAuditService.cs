using Nexus.Execution.Domain;

namespace Nexus.Execution.Auditing
{
    public interface IExecutionAuditService
    {
        Task RecordOrderAsync(OrderRequest request, Guid? accountId = null, CancellationToken cancellationToken = default);
        Task RecordExecutionErrorAsync(string? orderId, string errorCode, string errorMessage, CancellationToken cancellationToken = default);
        Task RecordPositionAsync(PositionSnapshot snapshot, Guid? accountId = null, CancellationToken cancellationToken = default);
    }
}
