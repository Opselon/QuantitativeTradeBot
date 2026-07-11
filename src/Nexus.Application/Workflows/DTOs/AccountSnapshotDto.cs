using System;

namespace Nexus.Application.Workflows.DTOs
{
    public class AccountSnapshotDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string BrokerServer { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public decimal Margin { get; set; }
        public decimal FreeMargin { get; set; }
        public int Leverage { get; set; }
        public string Currency { get; set; } = "USD";
        public string AccountMode { get; set; } = "Simulation"; // Demo, Real, Simulation, etc.
        public string TerminalStatus { get; set; } = "Connected";
    }
}
