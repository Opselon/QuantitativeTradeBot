// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   PRESENTATION LAYER (Views / Controls)
// FILE:    TrainSkillsView.xaml.cs
// REFERENCED BY:
//   - src/Nexus.Desktop/MainWindow.xaml (Dynamic UI Tab Composition)
// ============================================================================

using System.Windows.Controls;

namespace Nexus.Desktop.Views.Workspaces
{
    /// <summary>
    /// Interaction logic for TrainSkillsView.xaml UI control.
    /// Supports the Databound TrainSkillsViewModel context.
    /// </summary>
    public partial class TrainSkillsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrainSkillsView"/> class.
        /// </summary>
        public TrainSkillsView()
        {
            InitializeComponent();
        }
    }
}