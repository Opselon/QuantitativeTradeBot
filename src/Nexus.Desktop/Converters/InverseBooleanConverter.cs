// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   PRESENTATION LAYER (WPF UI Converters)
// FILE:    InverseBooleanConverter.cs
// REFERENCED BY:
//   - src/Nexus.Desktop/Views/Workspaces/TrainSkillsView.xaml (XAML Resource Binding)
// ============================================================================

using System;
using System.Globalization;
using System.Windows.Data;

namespace Nexus.Desktop.Converters
{
    /// <summary>
    /// Highly optimized, thread-safe value converter designed to invert boolean states.
    /// Commonly used in WPF bindings to negate IsEnabled, IsBusy, or active processing flags.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its inverted counterpart (true -> false, false -> true).
        /// </summary>
        /// <param name="value">The source boolean value.</param>
        /// <param name="targetType">The binding target property type.</param>
        /// <param name="parameter">An optional helper parameter.</param>
        /// <param name="culture">The active UI culture context.</param>
        /// <returns>The inverted boolean value, or true if the input is invalid.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Meticulously check if the binding value is of boolean type
            if (value is bool booleanValue)
            {
                // Return inverted state
                return !booleanValue;
            }

            // Fallback default safe state for visual elements
            return true;
        }

        /// <summary>
        /// Converted back logic (re-inverts the value).
        /// </summary>
        /// <param name="value">The target boolean value.</param>
        /// <param name="targetType">The source binding property type.</param>
        /// <param name="parameter">An optional helper parameter.</param>
        /// <param name="culture">The active UI culture context.</param>
        /// <returns>The inverted boolean value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Perform identical inversion logic for backward updates
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }

            return true;
        }
    }
}