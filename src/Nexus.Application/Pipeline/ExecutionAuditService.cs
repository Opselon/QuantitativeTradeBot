using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexus.Application.Pipeline
{
    public class ExecutionAuditService
    {
        private readonly ILogger<ExecutionAuditService> _logger;
        private readonly ConcurrentQueue<string> _auditTrail = new();

        public ExecutionAuditService(ILogger<ExecutionAuditService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogSignalReceived(TradeSignal signal, string correlationId)
        {
            string log = $"[CorrID: {correlationId}] [AUDIT] Signal received from Strategy {signal.StrategyId} for {signal.SymbolName} {signal.Direction} {signal.Volume} lot(s).";
            _auditTrail.Enqueue(log);
            _logger.LogInformation("{AuditLog}", log);
        }

        public void LogRiskEvaluated(string strategyId, string symbol, bool passed, string reason, string correlationId)
        {
            string outcome = passed ? "PASSED" : "REJECTED";
            string log = $"[CorrID: {correlationId}] [AUDIT] Risk check {outcome} for Strategy {strategyId} on {symbol}. Reason: {reason}";
            _auditTrail.Enqueue(log);
            if (passed)
            {
                _logger.LogInformation("{AuditLog}", log);
            }
            else
            {
                _logger.LogWarning("{AuditLog}", log);
            }
        }

        public void LogOrderSubmitted(Guid orderId, string symbol, string correlationId)
        {
            string log = $"[CorrID: {correlationId}] [AUDIT] Order {orderId} submitted for {symbol}.";
            _auditTrail.Enqueue(log);
            _logger.LogInformation("{AuditLog}", log);
        }

        public void LogOrderExecutionResult(Guid orderId, string ticketId, bool success, string message, string correlationId)
        {
            string outcome = success ? $"FILLED ticket {ticketId}" : $"FAILED: {message}";
            string log = $"[CorrID: {correlationId}] [AUDIT] Order {orderId} execution outcome: {outcome}.";
            _auditTrail.Enqueue(log);
            _logger.LogInformation("{AuditLog}", log);
        }

        public IReadOnlyList<string> GetAuditTrail()
        {
            return _auditTrail.ToArray();
        }
    }
}
