namespace Nexus.Application.Ports
{
    public class AppSettings
    {
        public string SelectedProvider { get; set; } = "PostgreSQL"; // Default recommendation
        public string ConnectionString { get; set; } = "Host=localhost;Database=nexus_trading;Username=postgres;Password=postgres";
        public bool IsOnboarded { get; set; } = false;

        // MT5 Connection Profile (non-sensitive metadata)
        public string ProfileName { get; set; } = string.Empty;
        public string BrokerServer { get; set; } = string.Empty;
        public string LoginAccountId { get; set; } = string.Empty;
        public string TerminalPath { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool AutoReconnect { get; set; } = true;
    }

    public interface IAppConfigurationService
    {
        AppSettings GetSettings();
        void SaveSettings(AppSettings settings);
    }
}
