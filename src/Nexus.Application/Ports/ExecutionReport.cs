namespace Nexus.Application.Ports
{
    public record ExecutionReport(
        Guid CommandId,
        string ClientOrderId,
        string TicketId,
        bool IsSuccess,
        string ErrorMessage,
        double ExecutionPrice,
        double ExecutedVolume,
        DateTime Timestamp
    );
}
