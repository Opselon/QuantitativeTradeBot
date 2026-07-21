using Nexus.Core.Enums;
using System;

namespace Nexus.Core.Entities
{
    /// <summary>
    /// Deep-quantum configuration model holding all customizable parameters for risk control, active trailing,
    /// 20-tier capital safeguards, and the 40 active micro-scalping strategy flags.
    /// Passed dynamically via atomic reference swapping into the active execution engine for instantaneous hot-reloading.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Defined in: src/Nexus.Core/Entities/PositionManagerSettings.cs
    /// - Serviced by: src/Nexus.Core/Interfaces/IPositionManagerSettingsProvider.cs
    /// - Executed within: src/Nexus.Execution/Management/PositionManager.cs
    /// - Configured via UI: src/Nexus.Desktop/ViewModels/Workspaces/PositionManagerViewModel.cs
    /// </remarks>
    public class PositionManagerSettings
    {
        #region Master Execution Switches & Core Strategy Pattern
        /// <summary>
        /// Gets or sets the master switch determining whether the AI Core is permitted to dispatch live orders to MT5.
        /// When false, the pipeline executes telemetry and broadcasts explainability events but intercepts raw routing.
        /// </summary>
        /// <value>Variable Type: <see cref="bool"/> (Default: <c>false</c>).</value>
        public bool IsAutoTradingEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the active high-frequency quantitative strategy pattern selected by the desk operator.
        /// Controls the baseline rule consensus recalibration matrix inside the Decision Fusion Engine.
        /// </summary>
        /// <value>Variable Type: <see cref="ScalpingStrategyType"/> (Default: <see cref="ScalpingStrategyType.SafeTrendFollowing"/>).</value>
        public ScalpingStrategyType ActiveStrategy { get; set; } = ScalpingStrategyType.SafeTrendFollowing;
        #endregion

        #region 20 New Risk Control & Safeguard Feature Parameters
        /// <summary>
        /// Gets or sets a value indicating whether the Account Shield Risk-Free Priority mode is active.
        /// When true, open positions are aggressively moved to Break-Even + 2 pips upon hitting initial momentum spikes.
        /// </summary>
        /// <value>Variable Type: <see cref="bool"/> (Default: <c>true</c>).</value>
        public bool IsRiskFreePriorityEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the system patience cooldown delay in seconds enforcing minimum wait times before trailing adjustments.
        /// Prevents rate-limiting and socket flooding on broker execution servers.
        /// </summary>
        /// <value>Variable Type: <see cref="int"/> (Seconds, Range: 2 to 60, Default: 15).</value>
        public int SystemPatienceSeconds { get; set; } = 15;

        /// <summary>
        /// Gets or sets the Quantum Greed Index ratio (0.0 = Rational Statistical Noise Floor, 1.0 = Aggressive Tight Trail).
        /// Controls how aggressively trailing stop losses tighten behind fast price movements.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Ratio, Range: 0.0 to 1.0, Default: 0.40).</value>
        public double GreedIndex { get; set; } = 0.40;

        /// <summary>
        /// Gets or sets the account balance profit gain percentage threshold required to trigger automated Save Profit scale-outs.
        /// Example: A value of 0.50 triggers a 30% volume reduction when floating profit hits 0.50% of the total account balance.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Percentage, Default: 0.50%).</value>
        public double SaveProfitThresholdPercent { get; set; } = 0.50;

        /// <summary>
        /// Gets or sets the maximum broker spread limit in pips allowed before new market or pending orders are blocked.
        /// Protects 1-minute scalping strategies from extreme news spread widening.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Pips, Default: 2.5 pips).</value>
        public double MaxAllowedSpreadPips { get; set; } = 2.5;

        /// <summary>
        /// Gets or sets the maximum server response ping latency threshold in milliseconds.
        /// If broker bridge latency exceeds this limit, automatic order submissions are paused.
        /// </summary>

        public int HftLatencyLimitMs { get; set; } = 120;

        /// <summary>
        /// Gets or sets the volatility drift sensitivity multiplier used to detect transition from Ranging to Breakout market regimes.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Default: 0.80).</value>
        public double VolatilityDriftThreshold { get; set; } = 0.80;

        /// <summary>
        /// Gets or sets the macro news sentiment impact filter ratio (0.0 = Ignore News, 1.0 = Full Lockdown during high-impact news).
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Ratio, Range: 0.0 to 1.0, Default: 0.50).</value>
        public double NewsSentimentImpactGate { get; set; } = 0.50;

        /// <summary>
        /// Gets or sets the minimum account margin percentage required before triggering Margin Call Defense (MCD) evacuations.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Percentage, Default: 250.0%).</value>
        public double MarginDefenseThreshold { get; set; } = 250.0;

        /// <summary>
        /// Gets or sets the volatility multiplier defining the market noise floor for pre-emptive FAST-ACT cuts.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Default: 1.50).</value>
        public double NoiseFloorMultiplier { get; set; } = 1.50;

        /// <summary>
        /// Gets or sets the volatility multiplier defining the mathematical boundary for peak utility closures.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Default: 4.00).</value>
        public double PeakUtilityMultiplier { get; set; } = 4.00;

        /// <summary>
        /// Gets or sets the default volatility multiplier applied when initializing protective Stop Loss boundaries on naked trades.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Default: 2.50).</value>
        public double InitialSlVolatilityMultiplier { get; set; } = 2.50;

        /// <summary>
        /// Gets or sets the default volatility multiplier applied when initializing target Take Profit boundaries on naked trades.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Default: 4.50).</value>
        public double InitialTpVolatilityMultiplier { get; set; } = 4.50;

        /// <summary>
        /// Gets or sets the rate-limiting cooldown delay in seconds enforced between consecutive order modifications.
        /// </summary>
        /// <value>Variable Type: <see cref="int"/> (Seconds, Default: 15).</value>
        public int CooldownSeconds { get; set; } = 15;

        /// <summary>
        /// Gets or sets the minimum protective price movement required in pips before modifying broker limits (Hysteresis defense).
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Pips, Default: 3.0 pips).</value>
        public double HysteresisPipThreshold { get; set; } = 3.0;
        #endregion

        #region VARS Scale-Out & Profit-Taking Thresholds
        /// <summary>
        /// Gets or sets the account balance percentage gain required to activate Stage 1 profit taking (Scale-Out).
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Percentage, Default: 0.20%).</value>
        public double Stage1TpBalancePercent { get; set; } = 0.20;

        /// <summary>
        /// Gets or sets the percentage ratio of position volume to close when Stage 1 criteria is met.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Ratio, Default: 0.30 = 30%).</value>
        public double Stage1CloseVolumePercent { get; set; } = 0.30;

        /// <summary>
        /// Gets or sets the account balance percentage gain required to activate Stage 2 profit taking.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Percentage, Default: 0.50%).</value>
        public double Stage2TpBalancePercent { get; set; } = 0.50;

        /// <summary>
        /// Gets or sets the percentage ratio of position volume to close when Stage 2 criteria is met.
        /// </summary>
        /// <value>Variable Type: <see cref="double"/> (Ratio, Default: 0.50 = 50%).</value>
        public double Stage2CloseVolumePercent { get; set; } = 0.50;
        #endregion

        #region 40 Active HFT Scalping Strategy Flags

        #region Category 1: M1 Hyper Scalping Strategies (10 Algorithms)
        /// <summary>Gets or sets a value indicating whether M1 Momentum Breakout Scalper is active.</summary>
        public bool IsStr_M1_MomentumBreakout { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 RSI Mean Reversion Rebound is active.</summary>
        public bool IsStr_M1_MeanReversion_RSI { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 VWAP Deviation Rider is active.</summary>
        public bool IsStr_M1_VWAP_Rider { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 Bollinger Band Volatility Trap is active.</summary>
        public bool IsStr_M1_BollingerTrap { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M1 Tick Acceleration Velocity Scalper is active.</summary>
        public bool IsStr_M1_TickAcceleration { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 Spread Spike Exploiter is active.</summary>
        public bool IsStr_M1_SpreadExploiter { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 Order Book Depth Imbalance Tracker is active.</summary>
        public bool IsStr_M1_OrderBookImbalance { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M1 Heikin-Ashi Micro-Trend Wave Scalper is active.</summary>
        public bool IsStr_M1_HeikinAshi_TrendWave { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M1 MACD Zero-Line Histogram Breakout is active.</summary>
        public bool IsStr_M1_MACD_HistogramBreakout { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M1 Stochastic Extreme Rebound is active.</summary>
        public bool IsStr_M1_StochasticRebound { get; set; } = false;
        #endregion

        #region Category 2: M5 Structural Scalping Strategies (10 Algorithms)
        /// <summary>Gets or sets a value indicating whether M5 EMA 9/21 Dynamic Crossover is active.</summary>
        public bool IsStr_M5_EMACrossover { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 ATR Band Volatility Breakout is active.</summary>
        public bool IsStr_M5_AtrBandBreakout { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 Daily Pivot Point Reversal Scalper is active.</summary>
        public bool IsStr_M5_PivotPointReversal { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M5 Fibonacci 61.8/78.6 Retracement Scalper is active.</summary>
        public bool IsStr_M5_FibonacciRetracement { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 SuperTrend Volatility Rider is active.</summary>
        public bool IsStr_M5_SuperTrendRider { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 Donchian Channel Breakout Scalper is active.</summary>
        public bool IsStr_M5_DonchianChannelBreakout { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M5 Volume Spread Analysis (VSA) is active.</summary>
        public bool IsStr_M5_VolumeSpreadAnalysis { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 RSI Divergence Peak/Valley Tracker is active.</summary>
        public bool IsStr_M5_RsiDivergence { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether M5 MACD Signal Line Crossover is active.</summary>
        public bool IsStr_M5_MacdSignalLineCrossover { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether M5 Keltner Channel Boundary Reversal is active.</summary>
        public bool IsStr_M5_KeltnerChannelReversal { get; set; } = false;
        #endregion

        #region Category 3: HFT & Liquidity Strategies (10 Algorithms)
        /// <summary>Gets or sets a value indicating whether HFT Order Flow Imbalance Analyzer is active.</summary>
        public bool IsStr_HFT_OrderFlowImbalance { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether HFT Market Maker Bid/Ask Spread Arbitrage is active.</summary>
        public bool IsStr_HFT_MarketMakerSpread { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether HFT Sub-Millisecond Tick Velocity Tracker is active.</summary>
        public bool IsStr_HFT_TickMomentum_Velocity { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether HFT Statistical Spread Arbitrage Engine is active.</summary>
        public bool IsStr_HFT_StatisticalSpreadArbitrage { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether HFT Hidden Iceberg Order Detector is active.</summary>
        public bool IsStr_HFT_IcebergOrderDetection { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether HFT Liquidity Stop-Hunting Exploiter is active.</summary>
        public bool IsStr_HFT_StopHuntingExploiter { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether HFT High-Volume Cluster Reversal Engine is active.</summary>
        public bool IsStr_HFT_VolumeClusterReversal { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether HFT Large Block Order Liquidity Wall Trailing is active.</summary>
        public bool IsStr_HFT_BlockOrderTrailing { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether HFT Triangular Currency Arbitrage Switch is active.</summary>
        public bool IsStr_HFT_TriangularArbitrage_Fallback { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether HFT Multi-Asset Correlation Matrix Scalper is active.</summary>
        public bool IsStr_HFT_CorrelationScalping { get; set; } = true;
        #endregion

        #region Category 4: MTF & Macro Alignment Strategies (10 Algorithms)
        /// <summary>Gets or sets a value indicating whether MTF M1/M5 Channel Consensus Rider is active.</summary>
        public bool IsStr_MTF_ConsensusRider_M1_M5 { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether MTF H4/D1 Dominant Macro Trend Follower is active.</summary>
        public bool IsStr_MTF_H4_D1_TrendFollowing { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether MTF Hybrid VWAP/SMA Alignment Engine is active.</summary>
        public bool IsStr_MTF_VwapSma_Hybrid { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether MTF Multi-Oscillator Consensus Aggregator is active.</summary>
        public bool IsStr_MTF_MultiOscillator_Consensus { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether MTF Time-Series Volatility Drift Follower is active.</summary>
        public bool IsStr_MTF_VolatilityDriftFollower { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether MTF High-Impact News Momentum Surge Scalper is active.</summary>
        public bool IsStr_MTF_MacroNews_Momentum { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether MTF Session Open Breakout Engine (London/NY) is active.</summary>
        public bool IsStr_MTF_SessionOpen_Breakout { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether MTF Session Close Liquidity Reversion Engine is active.</summary>
        public bool IsStr_MTF_SessionClose_Reversion { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether MTF Weekend Price Gap Fill Scalper is active.</summary>
        public bool IsStr_MTF_WeekendGap_Close { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether MTF Deep Learning Champion Neural Model Signal Router is active.</summary>
        public bool IsStr_MTF_SelfLearning_ChampionModel { get; set; } = true;
        #endregion

        #endregion
    }
}