using System;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Pipeline
{
    public class DefaultRiskManager : IRiskManager
    {
        private readonly double _maxDrawdownPercent;
        private readonly int _maxPositions;

        public DefaultRiskManager(double maxDrawdownPercent = 20.0, int maxPositions = 10)
        {
            _maxDrawdownPercent = maxDrawdownPercent;
            _maxPositions = maxPositions;
        }

        public Task<RiskResult> CheckOrderRiskAsync(Account account, Order order)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (order == null) throw new ArgumentNullException(nameof(order));

            // Check drawdown constraint
            if (account.Balance > 0)
            {
                decimal drawdown = (account.Balance - account.Equity) / account.Balance * 100m;
                if (drawdown > (decimal)_maxDrawdownPercent)
                {
                    return Task.FromResult(new RiskResult(false, $"Account drawdown {drawdown:F2}% exceeds maximum limit of {_maxDrawdownPercent}%"));
                }
            }

            // Check order parameters
            if (order.Volume.Value <= 0)
            {
                return Task.FromResult(new RiskResult(false, $"Invalid volume size: {order.Volume.Value}"));
            }

            // Check price
            if (order.Price <= 0)
            {
                return Task.FromResult(new RiskResult(false, $"Invalid order price: {order.Price}"));
            }

            return Task.FromResult(new RiskResult(true, "Passed risk check"));
        }
    }
}
