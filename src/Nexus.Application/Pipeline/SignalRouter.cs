using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Pipeline
{
    public class SignalRouter
    {
        private readonly ExecutionCoordinator _coordinator;

        public SignalRouter(ExecutionCoordinator coordinator)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        public async Task<ExecutionResult> RouteSignalAsync(TradeSignal signal, PipelineContext context, CancellationToken cancellationToken = default)
        {
            return await _coordinator.ProcessSignalAsync(signal, context, cancellationToken);
        }
    }
}
