namespace Nexus.Desktop.ViewModels.Workspaces
{
    public class ManualDeskViewModel : ViewModelBase
    {
        public Mt5TradingViewModel Mt5Trading { get; }

        public ManualDeskViewModel(Mt5TradingViewModel mt5Trading)
        {
            Mt5Trading = mt5Trading;
        }
    }
}
