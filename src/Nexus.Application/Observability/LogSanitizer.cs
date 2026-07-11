using System;
using System.Text.RegularExpressions;

namespace Nexus.Application.Observability
{
    public static class LogSanitizer
    {
        private static readonly Regex KeyValueSecretRegex = new(
            @"(password|pwd|secret|token|apikey|key|connectionstring|passwd|pass|cred|credential)\s*(:|=)\s*([^;\r\n\s,""]+)",
            RegexOptions.IgnoreCase);

        public static string Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            try
            {
                // Mask key-value secret fields
                var output = KeyValueSecretRegex.Replace(input, "$1$2******");
                return output;
            }
            catch
            {
                return "******";
            }
        }
    }
}
