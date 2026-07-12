namespace Nexus.Application.Mt5
{
    public class PlaceOrderResult
    {
        public bool IsSuccess { get; }
        public long Ticket { get; }
        public string Status { get; }
        public string? ErrorMessage { get; }
        public string? Comment { get; }

        public PlaceOrderResult(bool isSuccess, long ticket, string status, string? errorMessage, string? comment = null)
        {
            IsSuccess = isSuccess;
            Ticket = ticket;
            Status = status;
            ErrorMessage = errorMessage;
            Comment = comment;
        }
    }
}
