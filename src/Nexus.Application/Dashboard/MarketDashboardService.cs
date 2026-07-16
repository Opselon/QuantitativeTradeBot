using System;
using System.Collections.Generic;

namespace Nexus.Application.Dashboard
{
    public sealed class MarketDashboardService : IMarketDashboardService
    {
        public string CurrentSymbol { get; set; } = "UNKNOWN";
        public string MarketRegime { get; private set; } = "UNKNOWN";
        public int MarketQualityScore { get; private set; } = 0;
        public double Liquidity { get; private set; } = 0.0;
        public double Volatility { get; private set; } = 0.0;
        public double Momentum { get; private set; } = 0.0;
        public string D1Consensus { get; private set; } = "UNKNOWN";
        public string H4Consensus { get; private set; } = "UNKNOWN";
        public string M15Consensus { get; private set; } = "UNKNOWN";
        public string ConsensusSummary { get; private set; } = "No active data streams.";

        private readonly List<double> _recentPrices = new();
        public IReadOnlyList<double> RecentPrices => _recentPrices;

        public event Action<MarketDashboardData>? OnMarketUpdated;

        public void PushMarketUpdate(
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
            double currentPrice)
        {
            CurrentSymbol = symbol;
            MarketRegime = regime;
            MarketQualityScore = qualityScore;
            Liquidity = liquidity;
            Volatility = volatility;
            Momentum = momentum;
            D1Consensus = d1;
            H4Consensus = h4;
            M15Consensus = m15;
            ConsensusSummary = summary;

            _recentPrices.Add(currentPrice);
            if (_recentPrices.Count > 30)
            {
                _recentPrices.RemoveAt(0);
            }

            OnMarketUpdated?.Invoke(new MarketDashboardData
            {
                Symbol = symbol,
                Regime = regime,
                QualityScore = qualityScore,
                Liquidity = liquidity,
                Volatility = volatility,
                Momentum = momentum,
                D1 = d1,
                H4 = h4,
                M15 = m15,
                Summary = summary,
                RecentPrices = new List<double>(_recentPrices)
            });
        }
    }
}
