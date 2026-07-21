using System;

namespace Nexus.Core.Enums
{
    /// <summary>
    /// Specifies the high-frequency scalping logic applied during real-time tick evaluations.
    /// Governs the threshold calibration and features priority matrices inside the decision engine.
    /// </summary>
    public enum ScalpingStrategyType
    {
        /// <summary>
        /// Focuses on extreme directional momentum, buying high and selling higher. 
        /// Prioritizes raw standard deviation breakouts and high ATR indicators.
        /// </summary>
        AggressiveMomentum = 0,

        /// <summary>
        /// A highly statistical mean-reversion algorithm. Capitalizes on temporary price overextensions.
        /// Searches for RSI and Bollinger Bands limit-overflows to trade against the short-term noise.
        /// </summary>
        StatisticalReversion = 1,

        /// <summary>
        /// Slower, highly filtered trend-following model. Evaluates higher timeframe consensus (e.g. H4/D1)
        /// before allowing any short-term tick execution. Focuses on capital preservation first.
        /// </summary>
        SafeTrendFollowing = 2
    }
}