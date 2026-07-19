using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Nexus.Desktop.Converters
{
    /// <summary>
    /// Value converter that maps numerical or formatted currency profit values 
    /// to dynamic SolidColorBrush colors (Red for losses, Green for profits).
    /// </summary>
    public class ProfitToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts the profit value to a Color Brush.
        /// Parses doubles, decimals, and formatted strings (e.g. "$10.00" or "-$0.30").
        /// </summary>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Brushes.White;
            }

            try
            {
                // Extract currency symbols and commas to ensure numeric parsing
                string cleanString = value.ToString()
                    ?.Replace("$", "")
                    ?.Replace("€", "")
                    ?.Replace("£", "")
                    ?.Replace(",", "")
                    ?.Trim() ?? string.Empty;

                if (double.TryParse(cleanString, NumberStyles.Any, CultureInfo.InvariantCulture, out double profitAmount))
                {
                    if (profitAmount < 0)
                    {
                        // Return red for losses
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4D4D"));
                    }
                    else if (profitAmount > 0)
                    {
                        // Return green for active profits
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00E676"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProfitToBrushConverter] Error converting color: {ex.Message}");
            }

            // Fallback color for exactly break-even (0.0) or parsing failures
            return Brushes.White;
        }

        /// <summary>
        /// ConvertBack is not supported for unidirectional color presentation.
        /// </summary>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}