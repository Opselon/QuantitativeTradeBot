using System.Windows;
using Nexus.Desktop.ViewModels;

namespace Nexus.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
