namespace Nexus.Desktop.ViewModels
{
    public class DesktopSymbolViewModel : ViewModelBase
    {
        private string _symbolName = string.Empty;
        private bool _isSubscribed;
        private double _bid;
        private double _ask;
        private double _spread;
        private string _lastUpdateTimeText = "Never";
        private int _tickCount;

        public string SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        public bool IsSubscribed
        {
            get => _isSubscribed;
            set => SetProperty(ref _isSubscribed, value);
        }

        public double Bid
        {
            get => _bid;
            set => SetProperty(ref _bid, value);
        }

        public double Ask
        {
            get => _ask;
            set => SetProperty(ref _ask, value);
        }

        public double Spread
        {
            get => _spread;
            set => SetProperty(ref _spread, value);
        }

        public string LastUpdateTimeText
        {
            get => _lastUpdateTimeText;
            set => SetProperty(ref _lastUpdateTimeText, value);
        }

        public int TickCount
        {
            get => _tickCount;
            set => SetProperty(ref _tickCount, value);
        }
    }
}
