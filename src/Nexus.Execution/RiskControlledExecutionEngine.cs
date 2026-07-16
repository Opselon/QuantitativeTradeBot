using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexus.Execution.Auditing;
using Nexus.Execution.Domain;
using Nexus.Execution.Enums;
using Nexus.Execution.Gateways;
using Nexus.Execution.Management;
using Nexus.Execution.Risk;

namespace Nexus.Execution
{
    public class RiskControlledExecutionEngine
    {
        private readonly IExecutionGateway _simulationGateway;
        private readonly IExecutionGateway _liveGateway;
        private readonly IRiskExecutionGuard _riskGuard;
        private readonly IExecutionAuditService _auditService;
        private readonly PositionManager _positionManager;
        private readonly ILogger<RiskControlledExecutionEngine> _logger;

        public ExecutionProfile Profile { get; set; } = ExecutionProfile.Simulation;
        public bool IsLivePermissionGranted { get; set; } = false;

        public RiskControlledExecutionEngine(
            SimulationExecutionGateway simulationGateway,
            MT5ExecutionGateway liveGateway,
            IRiskExecutionGuard riskGuard,
            IExecutionAuditService auditService,
            PositionManager positionManager,
            ILogger<RiskControlledExecutionEngine> logger)
        {
            _simulationGateway = simulationGateway ?? throw new ArgumentNullException(nameof(simulationGateway));
            _liveGateway = liveGateway ?? throw new ArgumentNullException(nameof(liveGateway));
            _riskGuard = riskGuard ?? throw new ArgumentNullException(nameof(riskGuard));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _positionManager = positionManager ?? throw new ArgumentNullException(nameof(positionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an Order with strict Decision -> Risk Validation -> Execution Permission -> Order Routing steps.
        /// </summary>
        public async Task<ExecutionResult> ExecuteOrderAsync(
            OrderRequest request,
            double currentEquity,
            double currentBalance,
            double cumulativeExposure,
            double dailyLoss,
            string marketRegime,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var overallStopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[DECISION] Evaluating order request: Symbol={Symbol}, Side={Side}, Volume={Volume}, Reason='{Reason}'. Steps: Decision -> Risk",
                request.Symbol, request.Side, request.Volume, request.Reason);

            // Step 1: Track order initially
            _positionManager.TrackOrder(request);
            await _auditService.RecordOrderAsync(request, null, cancellationToken);

            // Step 2: Risk Validation
            var riskResult = await _riskGuard.CheckRiskAsync(
                request, currentEquity, currentBalance, cumulativeExposure, dailyLoss, marketRegime, cancellationToken);

            await _auditService.RecordOrderAsync(request, null, cancellationToken);

            if (!riskResult.IsPassed)
            {
                overallStopwatch.Stop();
                var rejectLatency = overallStopwatch.Elapsed.TotalMilliseconds;

                _logger.LogWarning(
                    "[RISK REJECTED] Order {OrderId} failed risk validation. Reason: {Reason}. Latency: {Latency:F2}ms",
                    request.Id, riskResult.Reason, rejectLatency);

                await _auditService.RecordExecutionErrorAsync(
                    request.Id.ToString(), "RISK_REJECTION", riskResult.Reason, cancellationToken);

                _positionManager.UntrackOrder(request.Id);
                return ExecutionResult.Failed($"Risk Rejected: {riskResult.Reason}", rejectLatency);
            }

            _logger.LogInformation(
                "[RISK PASSED] Order {OrderId} successfully passed all risk checks. Proceeding to Execution Permission.",
                request.Id);

            // Step 3: Execution Permission & Profile Routing
            IExecutionGateway gatewayToUse;
            if (Profile == ExecutionProfile.Live)
            {
                if (!IsLivePermissionGranted)
                {
                    overallStopwatch.Stop();
                    var permLatency = overallStopwatch.Elapsed.TotalMilliseconds;

                    string errorMsg = "Live trading profile selected, but explicit Live permission (IsLivePermissionGranted) is not enabled.";
                    _logger.LogError("[PERMISSION DENIED] Order {OrderId}: {Error}", request.Id, errorMsg);

                    request.TransitionTo(ExecutionState.Rejected);
                    await _auditService.RecordOrderAsync(request, null, cancellationToken);
                    await _auditService.RecordExecutionErrorAsync(request.Id.ToString(), "LIVE_PERMISSION_DENIED", errorMsg, cancellationToken);

                    _positionManager.UntrackOrder(request.Id);
                    return ExecutionResult.Failed(errorMsg, permLatency);
                }

                _logger.LogWarning("[ROUTING] Live execution permission verified. Routing Order {OrderId} to MT5 Live Broker Adapter.", request.Id);
                gatewayToUse = _liveGateway;
            }
            else if (Profile == ExecutionProfile.Paper)
            {
                _logger.LogInformation("[ROUTING] Routing Order {OrderId} to Simulated Execution Gateway (Virtual Paper Trading).", request.Id);
                gatewayToUse = _simulationGateway;
            }
            else // Simulation
            {
                _logger.LogInformation("[ROUTING] Routing Order {OrderId} to Simulation Gateway (No external broker connection).", request.Id);
                gatewayToUse = _simulationGateway;
            }

            // Step 4: Dispatch to Selected Gateway
            request.TransitionTo(ExecutionState.Submitted);
            await _auditService.RecordOrderAsync(request, null, cancellationToken);

            var dispatchStopwatch = Stopwatch.StartNew();
            var gatewayResult = await gatewayToUse.SubmitOrderAsync(request, cancellationToken);
            dispatchStopwatch.Stop();

            overallStopwatch.Stop();
            var totalLatency = overallStopwatch.Elapsed.TotalMilliseconds;

            if (gatewayResult.Success)
            {
                _logger.LogInformation(
                    "[FILLED] Order {OrderId} successfully executed and filled. Ticket={Ticket}, Gateway Latency={GatewayLatency:F2}ms, Total Latency={TotalLatency:F2}ms",
                    request.Id, gatewayResult.OrderId, dispatchStopwatch.Elapsed.TotalMilliseconds, totalLatency);

                // Create and track new position snapshot
                var snapshot = new PositionSnapshot(
                    ticketId: gatewayResult.OrderId,
                    symbol: request.Symbol,
                    direction: request.Side,
                    volume: request.Volume,
                    entryPrice: request.Entry,
                    currentPrice: request.Entry,
                    stopLoss: request.StopLoss,
                    takeProfit: request.TakeProfit,
                    unrealizedPnl: 0m,
                    riskExposure: request.Volume * request.Entry,
                    status: "OPEN"
                );

                _positionManager.TrackPosition(snapshot);
                _positionManager.UntrackOrder(request.Id);

                await _auditService.RecordPositionAsync(snapshot, null, cancellationToken);
                await _auditService.RecordOrderAsync(request, null, cancellationToken);

                return ExecutionResult.Succeeded(gatewayResult.OrderId, totalLatency);
            }
            else
            {
                _logger.LogError(
                    "[GATEWAY REJECTED] Order {OrderId} failed on gateway. Error: {Error}, Gateway Latency={GatewayLatency:F2}ms, Total Latency={TotalLatency:F2}ms",
                    request.Id, gatewayResult.Error, dispatchStopwatch.Elapsed.TotalMilliseconds, totalLatency);

                request.TransitionTo(ExecutionState.Rejected);
                _positionManager.UntrackOrder(request.Id);

                await _auditService.RecordOrderAsync(request, null, cancellationToken);
                await _auditService.RecordExecutionErrorAsync(request.Id.ToString(), "GATEWAY_ERROR", gatewayResult.Error, cancellationToken);

                return ExecutionResult.Failed($"Gateway Error: {gatewayResult.Error}", totalLatency);
            }
        }
    }
}
