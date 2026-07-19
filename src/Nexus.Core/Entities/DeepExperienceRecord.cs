namespace Nexus.Core.Entities
{
    /// <summary>
    /// Represents a highly contextual, Deep-RL structured memory of a trade.
    /// Captures the full environment state, indicators, execution slippage, and post-trade self-evaluation.
    /// Designed specifically to be ingested by Python/TensorFlow offline pipelines.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Consumed by: src/Nexus.Training/ExperienceReplayEngine.cs (to generate and classify episodes)
    /// - Consumed by: src/Nexus.Training/TrainingPipeline.cs (to sync and build datasets)
    /// - Saved by: src/Nexus.Training/TradingLearningPlatform.cs (to write JSON artifacts on disk)
    /// </remarks>
    public class DeepExperienceRecord
    {
        #region 1. Identity & Time
        /// <summary>
        /// Unique GUID identifying this specific trading experience episode.
        /// </summary>
        public string ExperienceId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Mapped financial instrument (e.g., EURUSD, XAUUSD).
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Mapped timeframe interval used during evaluation (e.g., M15, H1).
        /// </summary>
        public string Timeframe { get; set; } = string.Empty;

        /// <summary>
        /// Precise UTC timestamp of the trade closure event.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        #endregion

        #region 2. Execution Context
        /// <summary>
        /// The physical broker entry execution price.
        /// </summary>
        public double EntryPrice { get; set; }

        /// <summary>
        /// The physical broker exit execution price.
        /// </summary>
        public double ExitPrice { get; set; }

        /// <summary>
        /// Mapped trade direction (Buy/Sell).
        /// </summary>
        public string Direction { get; set; } = string.Empty;

        /// <summary>
        /// Average spread recorded at the millisecond of entry.
        /// </summary>
        public double Spread { get; set; }

        /// <summary>
        /// Average slippage recorded during order execution.
        /// </summary>
        public double Slippage { get; set; }
        #endregion

        #region 3. Market Environment Context
        /// <summary>
        /// Trend bias classified by indicators (Bullish/Bearish/Neutral).
        /// </summary>
        public string Trend { get; set; } = string.Empty;

        /// <summary>
        /// Calculated historical standard deviation volatility.
        /// </summary>
        public double Volatility { get; set; }

        /// <summary>
        /// Momentum rate calculated on the active timeframe.
        /// </summary>
        public double Momentum { get; set; }

        /// <summary>
        /// Calculated liquidity density based on orderbook spread.
        /// </summary>
        public double LiquidityDepth { get; set; }

        /// <summary>
        /// The active trading session (London, New York, Tokyo, etc).
        /// </summary>
        public string Session { get; set; } = "Unknown";

        /// <summary>
        /// Economic news impact category active at entry.
        /// </summary>
        public string NewsImpact { get; set; } = "None";

        /// <summary>
        /// The classified market regime (Ranging, Volatile, Trending).
        /// </summary>
        public string MarketRegime { get; set; } = string.Empty;
        #endregion

        #region 4. C++ Quant Features (64-Element Vector)
        /// <summary>
        /// Continuous 64-element feature state exported directly from the C++ bare-metal native DLL.
        /// </summary>
        public float[] MarketVectorFeatures { get; set; } = new float[64];
        #endregion

        #region 5. AI Model Metrics & Decision Context
        /// <summary>
        /// The registry version of the neural model used for training.
        /// </summary>
        public string ModelVersion { get; set; } = "1.0.0";

        /// <summary>
        /// The active running champion model version used during live execution.
        /// </summary>
        public string ActiveModelVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Model's computed probability for buy triggers.
        /// </summary>
        public double BuyConfidence { get; set; }

        /// <summary>
        /// Model's computed probability for sell triggers.
        /// </summary>
        public double SellConfidence { get; set; }

        /// <summary>
        /// The overall risk index assigned to the trade.
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// Model's final confidence score for the taken decision.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The list of structural rules or neural logits that triggered this entry.
        /// </summary>
        public List<string> DecisionReasons { get; set; } = new List<string>();

        /// <summary>
        /// Mapped AI execution action (e.g., BUY, SELL, WAIT).
        /// </summary>
        public string ExecutedAction { get; set; } = "WAIT";
        #endregion

        #region 6. Outcomes & Self-Evaluation (Experience Replay)
        /// <summary>
        /// Flag confirming if the final trade realized positive pips yield.
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// The actual net pips gained or lost from entry to exit.
        /// </summary>
        public double RealizedPips { get; set; }

        /// <summary>
        /// The maximum drawdown (excursion) recorded during the trade lifespan.
        /// </summary>
        public double MaxDrawdownDuringTrade { get; set; }

        /// <summary>
        /// The exact duration of the trade holding time expressed in minutes.
        /// </summary>
        public double HoldingTimeMinutes { get; set; }

        /// <summary>
        /// The final risk-to-reward ratio realized.
        /// </summary>
        public double RiskRewardRatio { get; set; }
        #endregion

        #region 7. AlphaGo Style Mistake Classification
        /// <summary>
        /// Deep-RL mistake classification (e.g., High Volatility Trap, Against Macro Trend).
        /// </summary>
        public string MistakeClassification { get; set; } = "None";

        /// <summary>
        /// Actionable architectural feedback to adjust optimizer thresholds on future training runs.
        /// </summary>
        public List<string> ImprovementFeedback { get; set; } = new List<string>();
        #endregion
    }
}