using System;
using System.Collections.Generic;
using Nexus.Core.Entities;

namespace Nexus.Application.Dashboard
{
    public interface IMarketDashboardService
    {
        string CurrentSymbol { get; set; }
        string MarketRegime { get; }
        int MarketQualityScore { get; }
        double Liquidity { get; }
        double Volatility { get; }
        double Momentum { get; }
        string D1Consensus { get; }
        string H4Consensus { get; }
        string M15Consensus { get; }
        string ConsensusSummary { get; }
        IReadOnlyList<double> RecentPrices { get; }

        event Action<MarketDashboardData>? OnMarketUpdated;

        void PushMarketUpdate(
            string symbol,
            string regime,
            int qualityScore,
            double liquidity,
            double volatility,
            double momentum,
            string d1,
            string h4,
            string m15,
            string summary,
            double currentPrice);
    }

    public class MarketDashboardData
    {
        public string Symbol { get; set; } = string.Empty;
        public string Regime { get; set; } = string.Empty;
        public int QualityScore { get; set; }
        public double Liquidity { get; set; }
        public double Volatility { get; set; }
        public double Momentum { get; set; }
        public string D1 { get; set; } = string.Empty;
        public string H4 { get; set; } = string.Empty;
        public string M15 { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<double> RecentPrices { get; set; } = new();
    }
}
