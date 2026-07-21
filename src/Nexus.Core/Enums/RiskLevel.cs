using System;

namespace Nexus.Core.Enums
{
    /// <summary>
    /// Represents the high-fidelity 20-level quantitative risk classification matrix.
    /// Governs position sizing filters, maximum drawdown thresholds, and emergency evacuation locks.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Declared in: src/Nexus.Core/Enums/RiskLevel.cs
    /// - Utilized by: src/Nexus.Core/Entities/RiskState.cs
    /// - Fused in: src/Nexus.Application/AI/Decision/DecisionFusionEngine.cs
    /// </remarks>
    public enum RiskLevel
    {
        #region Tier 1: Risk-Free & Conservative Safeguards (Levels 0 - 4)
        /// <summary>
        /// Level 0: Zero exposure or fully-hedged state. Active arbitrage mode.
        /// </summary>
        RiskFree = 0,

        /// <summary>
        /// Level 1: Extremely small risk trials. Equivalent to micro-lot testing.
        /// </summary>
        UltraLow = 1,

        /// <summary>
        /// Level 2: Low-exposure conservative trading.
        /// </summary>
        Low = 2,

        /// <summary>
        /// Level 3: Defensive scaling mode with high cash reserve allocation.
        /// </summary>
        Conservative = 3,

        /// <summary>
        /// Level 4: Standard defensive configuration. Below historical volatility averages.
        /// </summary>
        VeryConservative = 4,
        #endregion

        #region Tier 2: Standard & Balanced Operations (Levels 5 - 9)
        /// <summary>
        /// Level 5: Normal day-to-day operations. Matches long-term historical variance.
        /// </summary>
        Normal = 5,

        /// <summary>
        /// Level 6: Balanced average risk allocation.
        /// </summary>
        Medium = 6,

        /// <summary>
        /// Level 7: Moderate risk level. Sizing is adjusted slightly upward.
        /// </summary>
        Moderate = 7,

        /// <summary>
        /// Level 8: Optimistic exposure based on high win-rate pattern confirmations.
        /// </summary>
        AboveAverage = 8,

        /// <summary>
        /// Level 9: Maximum standard operational exposure allowed under normal market regimes.
        /// </summary>
        StandardExposure = 9,
        #endregion

        #region Tier 3: Aggressive & Elevated Exposures (Levels 10 - 14)
        /// <summary>
        /// Level 10: Elevated risk. Volatility is rising or drawdowns are starting to accumulate.
        /// </summary>
        Elevated = 10,

        /// <summary>
        /// Level 11: High risk. Prioritizes defensive cuts; tightens active Stop Losses.
        /// </summary>
        High = 11,

        /// <summary>
        /// Level 12: Highly Elevated exposure. Volatile news cycles or high-spread environments.
        /// </summary>
        HighlyElevated = 12,

        /// <summary>
        /// Level 13: Aggressive exposure. Highly-leveraged scalping trades.
        /// </summary>
        Aggressive = 13,

        /// <summary>
        /// Level 14: Hyper-Aggressive exposure. Allowed only during verified macro breakouts.
        /// </summary>
        HyperAggressive = 14,
        #endregion

        #region Tier 4: Critical & Emergency Liquidation Safeguards (Levels 15 - 19)
        /// <summary>
        /// Level 15: Severe state. Sizing is locked to minimum; trailing stop defenses are bypassed.
        /// </summary>
        Severe = 15,

        /// <summary>
        /// Level 16: Extreme risk state. Position managers are primed to pre-emptively liquidate losing assets.
        /// </summary>
        Extreme = 16,

        /// <summary>
        /// Level 17: Critical risk boundary. No new orders are permitted. Wait actions enforced.
        /// </summary>
        Critical = 17,

        /// <summary>
        /// Level 18: Systemic failure state. Pre-emptive evacuation of all risk assets active.
        /// </summary>
        TerminalState = 18,

        /// <summary>
        /// Level 19: Liquidation Imminent. Margin Call Defense (MCD) locks active on the broker server.
        /// </summary>
        LiquidationImminent = 19
        #endregion
    }
}