using System;

namespace Nexus.Application.Workflows.DTOs
{
    public class ConnectionProfileDto
    {
        public string ProfileName { get; set; } = string.Empty;
        public string BrokerServer { get; set; } = string.Empty;
        public string LoginAccountId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string InvestorPassword { get; set; } = string.Empty;
        public string TerminalPath { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool AutoReconnect { get; set; } = true;
    }
}
