namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents score evaluations of possible market futures analyzed by the Scenario Evaluation Engine.
    /// </summary>
    public class ScenarioScore
    {
        public double TrendContinuationScore { get; }
        public double ReversalScore { get; }
        public double LiquidityFailureScore { get; }
        public double VolatilityExpansionScore { get; }

        public ScenarioScore(
            double trendContinuationScore,
            double reversalScore,
            double liquidityFailureScore,
            double volatilityExpansionScore)
        {
            TrendContinuationScore = trendContinuationScore;
            ReversalScore = reversalScore;
            LiquidityFailureScore = liquidityFailureScore;
            VolatilityExpansionScore = volatilityExpansionScore;
        }

        public string GetDominantScenario()
        {
            double max = TrendContinuationScore;
            string dominant = "Trend Continuation";

            if (ReversalScore > max)
            {
                max = ReversalScore;
                dominant = "Reversal";
            }
            if (LiquidityFailureScore > max)
            {
                max = LiquidityFailureScore;
                dominant = "Liquidity Failure";
            }
            if (VolatilityExpansionScore > max)
            {
                max = VolatilityExpansionScore;
                dominant = "Volatility Expansion";
            }

            return dominant;
        }

        public override string ToString()
        {
            return $"Cont={TrendContinuationScore:F2}, Rev={ReversalScore:F2}, LiqFail={LiquidityFailureScore:F2}, VolExp={VolatilityExpansionScore:F2}";
        }
    }
}
