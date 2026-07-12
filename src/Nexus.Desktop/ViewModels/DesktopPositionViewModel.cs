using System;
using Nexus.Desktop.Models;

namespace Nexus.Desktop.ViewModels
{
    public class DesktopPositionViewModel : ViewModelBase
    {
        private readonly DesktopPositionDto _dto;

        public DesktopPositionViewModel(DesktopPositionDto dto)
        {
            _dto = dto ?? throw new ArgumentNullException(nameof(dto));
        }

        public long Ticket => _dto.Ticket;
        public string Symbol => _dto.Symbol;
        public string Side => _dto.Side;
        public decimal Volume => _dto.Volume;
        public decimal OpenPrice => _dto.OpenPrice;
        public decimal CurrentPrice => _dto.CurrentPrice;
        public decimal StopLoss => _dto.StopLoss;
        public decimal TakeProfit => _dto.TakeProfit;
        public decimal Profit => _dto.Profit;
        public decimal Swap => _dto.Swap;
        public decimal Commission => _dto.Commission;
        public DateTime OpenTime => _dto.OpenTime;
        public TimeSpan Duration => DateTime.UtcNow - _dto.OpenTime;
        public string Status => _dto.Status;
    }
}
