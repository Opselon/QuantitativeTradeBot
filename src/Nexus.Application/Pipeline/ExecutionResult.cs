using System;

namespace Nexus.Application.Pipeline
{
    public record ExecutionResult(
        Guid RequestId,
        string TicketId,
        bool IsSuccess,
        string ErrorMessage,
        double ExecutionPrice,
        double ExecutedVolume,
        DateTime Timestamp
    );
}
