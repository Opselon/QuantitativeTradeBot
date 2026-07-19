using System.Text.RegularExpressions;

namespace Nexus.Application.Security
{
    public static class InputValidator
    {
        private static readonly Regex SymbolRegex = new(@"^[A-Z0-9#\.]{3,10}$", RegexOptions.Compiled);

        public static bool ValidateSymbol(string symbolName)
        {
            if (string.IsNullOrWhiteSpace(symbolName)) return false;
            return SymbolRegex.IsMatch(symbolName.ToUpperInvariant());
        }

        public static bool ValidateOrderSize(double volume)
        {
            return volume > 0 && volume <= 1000.0; // Prevent fat-finger order sizes
        }

        public static bool ValidatePrice(double price)
        {
            return price > 0 && price < 1000000.0;
        }

        public static bool ValidateAccountIdentifier(string accountId)
        {
            return !string.IsNullOrWhiteSpace(accountId) && accountId.Length >= 3 && accountId.Length <= 64;
        }
    }
}
