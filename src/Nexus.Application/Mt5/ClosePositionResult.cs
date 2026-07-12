namespace Nexus.Application.Mt5
{
    public class ClosePositionResult
    {
        public bool IsSuccess { get; }
        public long Ticket { get; }
        public string? ErrorMessage { get; }

        public ClosePositionResult(bool isSuccess, long ticket, string? errorMessage)
        {
            IsSuccess = isSuccess;
            Ticket = ticket;
            ErrorMessage = errorMessage;
        }
    }
}
