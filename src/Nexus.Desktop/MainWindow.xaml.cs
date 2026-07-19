using Nexus.Desktop.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace Nexus.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Switch to Light Theme
            Tag = "True";
            var app = System.Windows.Application.Current;
            var lightDict = new ResourceDictionary { Source = new Uri("pack://application:,,,/Nexus.Desktop;component/LightTheme.xaml", UriKind.Absolute) };

            // Find and replace the theme dictionary
            for (int i = 0; i < app.Resources.MergedDictionaries.Count; i++)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null && (dict.Source.OriginalString.Contains("DarkTheme.xaml") || dict.Source.OriginalString.Contains("LightTheme.xaml")))
                {
                    app.Resources.MergedDictionaries[i] = lightDict;
                    return;
                }
            }
            app.Resources.MergedDictionaries.Add(lightDict);
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Switch to Dark Theme
            Tag = "False";
            var app = System.Windows.Application.Current;
            var darkDict = new ResourceDictionary { Source = new Uri("pack://application:,,,/Nexus.Desktop;component/DarkTheme.xaml", UriKind.Absolute) };

            for (int i = 0; i < app.Resources.MergedDictionaries.Count; i++)
            {
                var dict = app.Resources.MergedDictionaries[i];
                if (dict.Source != null && (dict.Source.OriginalString.Contains("DarkTheme.xaml") || dict.Source.OriginalString.Contains("LightTheme.xaml")))
                {
                    app.Resources.MergedDictionaries[i] = darkDict;
                    return;
                }
            }
            app.Resources.MergedDictionaries.Add(darkDict);
        }
    }
}
