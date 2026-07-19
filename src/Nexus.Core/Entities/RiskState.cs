using Nexus.Core.Enums;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents the persistent risk status, capital allocations, and limits evaluated before entering trades.
    /// Integrates newly-defined Value Objects and Enums while keeping full backward compatibility.
    /// </summary>
    public class RiskState
    {
        public double MarginLevel { get; }
        public double MaxDrawdown { get; }
        public double CurrentDrawdown { get; }
        public int OpenTradeCount { get; }
        public double TotalExposure { get; }
        public bool IsTradingBlocked { get; }

        #region Value Object properties

        public Percentage MarginLevelPct => new Percentage(MarginLevel);
        public Percentage MaxDrawdownPct => new Percentage(MaxDrawdown);
        public Percentage CurrentDrawdownPct => new Percentage(CurrentDrawdown);

        public RiskLevel RiskLevel
        {
            get
            {
                if (IsTradingBlocked) return RiskLevel.Extreme;
                if (CurrentDrawdown >= MaxDrawdown * 0.8) return RiskLevel.High;
                if (CurrentDrawdown >= MaxDrawdown * 0.4) return RiskLevel.Medium;
                return RiskLevel.Low;
            }
        }

        #endregion

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
