using Nexus.Application.Ports;

namespace Nexus.Tests.EndToEnd.Mocks
{
    public class SimulatedExecutionGateway : IExecutionGateway
    {
        private long _ticketCounter = 100000;
        private bool _simulateFailure = false;
        private string _failureReason = string.Empty;

        public void ConfigureSimulation(bool simulateFailure, string failureReason = "")
        {
            _simulateFailure = simulateFailure;
            _failureReason = failureReason;
        }

        public Task<ExecutionReport> ExecuteAsync(ExecutionCommand command, CancellationToken cancellationToken = default)
        {
            if (_simulateFailure)
            {
                return Task.FromResult(new ExecutionReport(
                    command.CommandId,
                    command.ClientOrderId,
                    string.Empty,
                    false,
                    _failureReason,
                    0,
                    0,
                    DateTime.UtcNow
                ));
            }

            var ticketId = "TKT_" + Interlocked.Increment(ref _ticketCounter);
            return Task.FromResult(new ExecutionReport(
                command.CommandId,
                command.ClientOrderId,
                ticketId,
                true,
                string.Empty,
                command.Price, // Fill at requested price
                command.Volume,
                DateTime.UtcNow
            ));
        }
    }
}
