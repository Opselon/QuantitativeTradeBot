using Nexus.Execution.Domain;
using Nexus.Execution.Enums;

namespace Nexus.Execution.Risk
{
    public class RiskExecutionGuard : IRiskExecutionGuard
    {
        public double MaxExposureLimit { get; set; } = 250000.0; // Max cumulative exposure amount (e.g. $250k)
        public double MaxDailyLossLimit { get; set; } = 5000.0;   // Max daily loss limit (e.g. $5k)
        public double MaxPositionSize { get; set; } = 10.0;       // Max single position volume in lots (e.g. 10.0 lots)
        public double MaxRiskPercentage { get; set; } = 0.02;     // Max risk per trade (e.g. 2% of equity)

        public Task<RiskGuardResult> CheckRiskAsync(
            OrderRequest request,
            double currentEquity,
            double currentBalance,
            double cumulativeExposure,
            double dailyLoss,
            string marketRegime,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Check Stop Loss Existence
            if (!request.StopLoss.HasValue)
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, "Stop Loss is mandatory. Denied."));
            }

            // 2. Check Daily Loss Limit
            if (dailyLoss >= MaxDailyLossLimit)
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, $"Maximum daily loss limit exceeded ({dailyLoss:F2} >= {MaxDailyLossLimit:F2})."));
            }

            // 3. Check Single Position Size
            if (request.Volume > MaxPositionSize)
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, $"Requested position size {request.Volume:F2} lots exceeds maximum allowed position size {MaxPositionSize:F2} lots."));
            }

            // 4. Check Cumulative Exposure Limit
            double additionalExposure = request.Volume * request.Entry;
            if (cumulativeExposure + additionalExposure > MaxExposureLimit)
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, $"Additional exposure {additionalExposure:F2} exceeds maximum cumulative exposure limit {MaxExposureLimit:F2} (Current cumulative: {cumulativeExposure:F2})."));
            }

            // 5. Check Risk Percentage Limit (Risk to Equity ratio)
            double priceRisk = Math.Abs(request.Entry - request.StopLoss.Value);
            double multiplier = GetContractMultiplier(request.Symbol);
            double tradeRiskAmount = priceRisk * request.Volume * multiplier;
            double actualRiskPercent = currentEquity > 0 ? (tradeRiskAmount / currentEquity) : 1.0;

            if (actualRiskPercent > MaxRiskPercentage)
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, $"Trade risk of {tradeRiskAmount:F2} ({actualRiskPercent * 100:F2}%) exceeds maximum risk percentage limit {MaxRiskPercentage * 100:F2}% of equity ({currentEquity:F2})."));
            }

            // 6. Check Market Conditions
            if (marketRegime.Equals("ExtremeVolatility", StringComparison.OrdinalIgnoreCase) ||
                marketRegime.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
            {
                request.TransitionTo(ExecutionState.Rejected);
                return Task.FromResult(new RiskGuardResult(false, $"Trading is disabled due to restricted market conditions/regime: '{marketRegime}'."));
            }

            // All checks passed!
            request.TransitionTo(ExecutionState.Validated);
            return Task.FromResult(new RiskGuardResult(true, "All risk checks successfully passed."));
        }

        private static double GetContractMultiplier(string symbolName)
        {
            string upper = symbolName.ToUpperInvariant();
            if (upper.Contains("XAU") || upper.Contains("GOLD")) return 100.0;
            if (upper.Contains("XAG") || upper.Contains("SILVER")) return 5000.0;
            return 100000.0; // Default forex lot multiplier
        }
    }
}
