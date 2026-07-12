using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the persistent risk status, capital allocations, and limits evaluated before entering trades.
    /// </summary>
    public class RiskState
    {
        public double MarginLevel { get; }
        public double MaxDrawdown { get; }
        public double CurrentDrawdown { get; }
        public int OpenTradeCount { get; }
        public double TotalExposure { get; }
        public bool IsTradingBlocked { get; }

        public RiskState(
            double marginLevel,
            double maxDrawdown,
            double currentDrawdown,
            int openTradeCount,
            double totalExposure,
            bool isTradingBlocked)
        {
            MarginLevel = marginLevel;
            MaxDrawdown = maxDrawdown;
            CurrentDrawdown = currentDrawdown;
            OpenTradeCount = openTradeCount;
            TotalExposure = totalExposure;
            IsTradingBlocked = isTradingBlocked;
        }

        public override string ToString()
        {
            return $"Margin={MarginLevel:F1}%, Drawdown={CurrentDrawdown:P1}, OpenCount={OpenTradeCount}, Blocked={IsTradingBlocked}";
        }
    }
}
