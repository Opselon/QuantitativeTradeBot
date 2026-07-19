using Nexus.Desktop.Models;

namespace Nexus.Desktop.ViewModels
{
    public class DesktopPositionViewModel : ViewModelBase
    {
        private readonly DesktopPositionDto _dto;
        private decimal _currentPrice;
        private decimal _profit;
        private decimal _stopLoss;
        private decimal _takeProfit;

        public DesktopPositionViewModel(DesktopPositionDto dto)
        {
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));
            _currentPrice = dto.CurrentPrice;
            _profit = dto.Profit;
            _stopLoss = dto.StopLoss;
            _takeProfit = dto.TakeProfit;
        }

        public long Ticket => _dto.Ticket;
        public string Symbol => _dto.Symbol;
        public string Side => _dto.Side;
        public decimal Volume => _dto.Volume;
        public decimal OpenPrice => _dto.OpenPrice;

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set => SetProperty(ref _currentPrice, value);
        }

        public decimal StopLoss
        {
            get => _stopLoss;
            set => SetProperty(ref _stopLoss, value);
        }

        public decimal TakeProfit
        {
            get => _takeProfit;
            set => SetProperty(ref _takeProfit, value);
        }

        public decimal Profit
        {
            get => _profit;
            set => SetProperty(ref _profit, value);
        }

        public decimal Swap => _dto.Swap;
        public decimal Commission => _dto.Commission;
        public DateTime OpenTime => _dto.OpenTime;
        public TimeSpan Duration => DateTime.UtcNow - _dto.OpenTime;
        public string Status => _dto.Status;
    }
}