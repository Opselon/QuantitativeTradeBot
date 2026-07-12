namespace Nexus.Desktop.Models
{
    public class DesktopTradeResult
    {
        public bool IsSuccess { get; set; }
        public long Ticket { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Message { get; set; }
    }
}
