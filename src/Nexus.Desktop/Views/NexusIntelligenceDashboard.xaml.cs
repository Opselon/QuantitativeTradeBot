using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace Nexus.Desktop.Views
{
    /// <summary>
    /// Interaction logic for NexusIntelligenceDashboard.xaml
    /// </summary>
    public partial class NexusIntelligenceDashboard : UserControl
    {
        public NexusIntelligenceDashboard()
        {
            InitializeComponent();
        }
    }

    public class EvaluationBarPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length >= 3 && values[0] is double buy && values[1] is double sell && values[2] is double width)
            {
                double diff = buy - sell; // -1.0 to 1.0
                double percentage = 0.5 + (diff * 0.5); // 0.0 to 1.0
                double x = (width * percentage) - 3; // Center needle (width 6)
                return Math.Clamp(x, 0, Math.Max(0, width - 6));
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
