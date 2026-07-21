using Nexus.Desktop.ViewModels.Workspaces;
using System.Windows.Controls;

namespace Nexus.Desktop.Views.Workspaces
{
    /// <summary>
    /// Interaction logic for PositionManagerView.xaml.
    /// Acts as the code-behind view container bound to <see cref="PositionManagerViewModel"/>.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - XAML Markup: src/Nexus.Desktop/Views/Workspaces/PositionManagerView.xaml
    /// - DataContext: src/Nexus.Desktop/ViewModels/Workspaces/PositionManagerViewModel.cs
    /// </remarks>
    public partial class PositionManagerView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionManagerView"/> class for the WPF XAML designer.
        /// </summary>
        public PositionManagerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionManagerView"/> class with injected ViewModel.
        /// </summary>
        /// <param name="viewModel">The resolved <see cref="PositionManagerViewModel"/> instance.</param>
        public PositionManagerView(PositionManagerViewModel viewModel) : this()
        {
            this.DataContext = viewModel;
        }
    }
}