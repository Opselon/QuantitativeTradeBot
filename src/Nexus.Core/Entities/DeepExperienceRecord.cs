
namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a highly contextual, Deep-RL structured memory of a trade.
    /// Captures the full environment state, indicators, execution slippage, and post-trade self-evaluation.
    /// Designed specifically to be ingested by Python/TensorFlow offline pipelines.
    /// </summary>
    public class DeepExperienceRecord
    {
        public string ExperienceId { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = string.Empty;
        public string Timeframe { get; set; } = string.Empty;

        // Execution Context
        public double EntryPrice { get; set; }
        public double ExitPrice { get; set; }
        public string Direction { get; set; } = string.Empty;
        public double Spread { get; set; }
        public double Slippage { get; set; }

        // Market Environment Context
        public string Trend { get; set; } = string.Empty;
        public double Volatility { get; set; }
        public double Momentum { get; set; }
        public double LiquidityDepth { get; set; }
        public string Session { get; set; } = "Unknown";
        public string NewsImpact { get; set; } = "None";
        public string MarketRegime { get; set; } = string.Empty;

        // Neural & Decision Context
        public List<string> DecisionReasons { get; set; } = new();
        public double Confidence { get; set; }
        public string ActiveModelVersion { get; set; } = string.Empty;

        // Outcome & Self-Evaluation (Experience Replay)
        public bool IsWin { get; set; }
        public double RealizedPips { get; set; }
        public double MaxDrawdownDuringTrade { get; set; }
        public double HoldingTimeMinutes { get; set; }
        public double RiskRewardRatio { get; set; }

        // AlphaGo Style Mistake Classification
        public string MistakeClassification { get; set; } = "None";
        public List<string> ImprovementFeedback { get; set; } = new();
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}