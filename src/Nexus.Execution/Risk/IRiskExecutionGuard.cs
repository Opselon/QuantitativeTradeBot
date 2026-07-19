using Nexus.Execution.Domain;

namespace Nexus.Execution.Risk
{
    public record RiskGuardResult(bool IsPassed, string Reason);

    public interface IRiskExecutionGuard
    {
        Task<RiskGuardResult> CheckRiskAsync(
            OrderRequest request,
            double currentEquity,
            double currentBalance,
            double cumulativeExposure,
            double dailyLoss,
            string marketRegime,
            CancellationToken cancellationToken = default);
    }
}
