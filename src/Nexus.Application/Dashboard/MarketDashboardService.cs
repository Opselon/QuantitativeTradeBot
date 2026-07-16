using System;

namespace Nexus.Application.Dashboard
{
    public sealed class MarketDashboardService : IMarketDashboardService
    {
        public string CurrentSymbol { get; set; } = "EURUSD";
        public string MarketRegime { get; private set; } = "Trending Bullish";
        public int MarketQualityScore { get; private set; } = 85;
        public double Liquidity { get; private set; } = 0.90;
        public double Volatility { get; private set; } = 0.25;
        public double Momentum { get; private set; } = 0.75;
        public string D1Consensus { get; private set; } = "Bullish";
        public string H4Consensus { get; private set; } = "Bullish";
        public string M15Consensus { get; private set; } = "Entry Zone";
        public string ConsensusSummary { get; private set; } = "Strong Bullish continuation across multiple timeframes.";

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
            string summary)
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
                Summary = summary
            });
        }
    }
}
