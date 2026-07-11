namespace Nexus.Application.Workflows.DTOs
{
    public class ConnectionTestResultDto
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public AccountSnapshotDto? AccountSnapshot { get; set; }
    }
}
