using Nexus.Desktop.ViewModels.Workspaces;
using System.Windows;
using System.Windows.Controls;

namespace Nexus.Desktop.Views.Workspaces
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ConfirmCallback = async (message) =>
                {
                    bool confirmed = false;

                    // Standard WPF MessageBox confirmation on the UI thread
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        var result = MessageBox.Show(
                            message,
                            "NEXUS ALGORITHMIC SECURITY GATES",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning,
                            MessageBoxResult.No);

                        confirmed = result == MessageBoxResult.Yes;
                    });

                    return await Task.FromResult(confirmed);
                };
            }
        }
    }
}
