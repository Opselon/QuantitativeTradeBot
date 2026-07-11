using System;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Application.Pipeline
{
    public class PreTradeRiskEvaluator
    {
        private readonly IRiskManager _riskManager;

        public PreTradeRiskEvaluator(IRiskManager riskManager)
        {
            _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        }

        public async Task<RiskDecision> EvaluateAsync(Account account, Order order)
        {
            if (account == null) throw new ArgumentNullException(nameof(account));
            if (order == null) throw new ArgumentNullException(nameof(order));

            var result = await _riskManager.CheckOrderRiskAsync(account, order);
            return new RiskDecision(result.IsPassed, result.Reason);
        }
    }
}
