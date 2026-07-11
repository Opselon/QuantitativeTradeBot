using System;

namespace Nexus.Application.Security
{
    public enum EnvironmentProfile
    {
        Development,
        Simulation,
        PaperTrading,
        LiveTrading
    }

    public class SecurityConfiguration
    {
        public EnvironmentProfile Profile { get; set; } = EnvironmentProfile.Development;
        public bool IsLiveModeEnabled { get; set; } = false; // Safe default: disabled by default

        public static string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;

            try
            {
                var parts = connectionString.Split(';');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                        parts[i].Contains("pwd", StringComparison.OrdinalIgnoreCase) ||
                        parts[i].Contains("Secret", StringComparison.OrdinalIgnoreCase))
                    {
                        var eq = parts[i].IndexOf('=');
                        if (eq >= 0)
                        {
                            parts[i] = parts[i].Substring(0, eq + 1) + "******";
                        }
                    }
                }
                return string.Join(";", parts);
            }
            catch
            {
                return "******";
            }
        }
    }
}
