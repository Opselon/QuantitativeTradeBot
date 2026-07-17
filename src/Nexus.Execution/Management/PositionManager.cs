using Microsoft.Extensions.DependencyInjection;
using Nexus.Application.Dashboard;
using Nexus.Application.Ports;
using Nexus.Core.DomainEvents;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Execution.Domain;
using Nexus.Execution.Enums;
using Nexus.Execution.Gateways;
using Nexus.Training;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Execution.Management
{
    /// <summary>
    /// Jet-Engine Scale, ultra-fast, multi-scenario quantitative position manager.
    /// Executes high-frequency mathematical evaluations, volume-weighted scale-outs,
    /// margin-level defense locks, and preemptive survival-first closures.
    /// Fully integrated with the AlphaGo-style Experience Replay Engine.
    /// </summary>
    public class PositionManager
    {
        #region Private Fields & Concurrency Trackers
        private readonly ConcurrentDictionary<string, PositionSnapshot> _openPositions = new();
        private readonly ConcurrentBag<PositionSnapshot> _closedPositions = new();
        private readonly ConcurrentDictionary<Guid, OrderRequest> _pendingOrders = new();

        // Infrastructure Dependencies
        private readonly Nexus.Execution.Gateways.IExecutionGateway _executionGateway;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly IMarketDashboardService _marketDashboard;
        private readonly IDecisionEventStream _decisionStream;
        private readonly ExperienceReplayEngine _replayEngine;

        // Concurrency Guard: Strictly prevents duplicate executions, race conditions, or double-closures on the same ticket.
        private readonly ConcurrentDictionary<string, byte> _processingTickets = new();

        // Scale-out State Machine: Tracks the specific profit-taking stages executed for each ticket (0=None, 1=Stage1, 2=Stage2).
        private readonly ConcurrentDictionary<string, int> _scaleOutStages = new();

        // Broker Protection Guard: Prevents rate-limiting. Cooldown is bypassed instantly in emergency states (FAST-ACT).
        private readonly ConcurrentDictionary<string, (DateTime Time, double Sl, double Tp)> _lastModifications = new();
        #endregion

        #region Public Properties (Thread-Safe Snapshot Readers)
        public IReadOnlyList<PositionSnapshot> OpenPositions => _openPositions.Values.ToList();
        public IReadOnlyList<PositionSnapshot> ClosedPositions => _closedPositions.ToList();
        public IReadOnlyList<OrderRequest> PendingOrders => _pendingOrders.Values.ToList();
        #endregion

        #region Constructor
        public PositionManager(
            Nexus.Execution.Gateways.IExecutionGateway executionGateway,
            IDecisionEventStream decisionStream,
            ExperienceReplayEngine replayEngine,
            IServiceScopeFactory? scopeFactory = null,
            IMarketDashboardService marketDashboard = null!)
        {
            _executionGateway = executionGateway ?? throw new ArgumentNullException(nameof(executionGateway));
            _decisionStream = decisionStream ?? throw new ArgumentNullException(nameof(decisionStream));
            _replayEngine = replayEngine ?? throw new ArgumentNullException(nameof(replayEngine));
            _scopeFactory = scopeFactory;
            _marketDashboard = marketDashboard ?? throw new ArgumentNullException(nameof(marketDashboard));
        }
        #endregion

        #region Core Position & Order Tracking
        public void TrackOrder(OrderRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _pendingOrders[request.Id] = request;
        }

        public void UntrackOrder(Guid orderId)
        {
            _pendingOrders.TryRemove(orderId, out _);
        }

        public void TrackPosition(PositionSnapshot position)
        {
            if (position == null) throw new ArgumentNullException(nameof(position));
            _openPositions[position.TicketId] = position;
        }
        #endregion

        #region High-Frequency Quantum Risk Control Engine (Survival Guard)
        /// <summary>
        /// Executes high-speed mathematical evaluations and pre-emptive capital preservation closures.
        /// Decouples from sluggish cooldowns in volatile market spikes to execute FAST-ACT.
        /// </summary>
        public async Task ManageActivePositionsRiskAsync(
            double currentEquity,
            double currentBalance,
            double freeMargin,
            double marginLevel,
            CancellationToken cancellationToken = default)
        {
            if (_openPositions.IsEmpty) return;

            #region Extract Active Market Volatility & Regime
            double volatility = _marketDashboard != null ? _marketDashboard.Volatility : 0.15;
            if (volatility <= 0) volatility = 0.12;

            string currentRegime = _marketDashboard != null ? _marketDashboard.MarketRegime : "Ranging";
            #endregion

            #region PRIORITY 1: Margin Call Defense (MCD Lock - Survival First)
            // REASON: If Margin Level drops below 250%, the account is at extreme risk of cascading liquidations.
            // Fast-Act: Immediately execute a market order to close the worst losing position. Survival > Profit.
            bool isMarginCritical = marginLevel > 0 && marginLevel < 250.0;
            if (isMarginCritical)
            {
                var worstLosingPosition = _openPositions.Values
                    .Where(p => p.Status == "OPEN" && p.UnrealizedPnl < 0)
                    .OrderBy(p => p.UnrealizedPnl)
                    .FirstOrDefault();

                if (worstLosingPosition != null)
                {
                    _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                        Guid.NewGuid(), worstLosingPosition.Symbol, "MARGIN_DEFENSE", worstLosingPosition.Volume,
                        $"[EMERGENCY] Margin Level critical ({marginLevel:F1}%). Evacuating position {worstLosingPosition.TicketId} to prevent margin call."
                    ));

                    await HandlePartialCloseAsync(worstLosingPosition.TicketId, worstLosingPosition.Volume, worstLosingPosition.CurrentPrice, cancellationToken);
                    return; // Yield thread to allow broker margin recalculation before processing other trades
                }
            }
            #endregion

            // 2. High-Frequency Loop over all active open positions
            foreach (var pos in _openPositions.Values)
            {
                string ticketId = pos.TicketId;

                // Concurrency Lock: Skip if this ticket is currently undergoing another broker modification via API
                if (_processingTickets.ContainsKey(ticketId)) continue;

                try
                {
                    if (pos.Status != "OPEN") continue;

                    double entryPrice = pos.EntryPrice;
                    double currentPrice = pos.CurrentPrice;
                    string direction = pos.Direction;
                    double pipSize = GetPipSize(pos.Symbol);
                    double volume = pos.Volume;

                    double pipMultiplier = pos.Symbol.ToUpperInvariant().Contains("JPY") || pos.Symbol.ToUpperInvariant().Contains("XAU") ? 100.0 : 10000.0;
                    double floatingPips = CalculateRealizedPips(pos.Symbol, direction, entryPrice, currentPrice);

                    #region SCENARIO 0: Default SL/TP Initialization (Anti-Naked Guard)
                    // REASON: Ensure no trade exists without a mathematical hard-stop on the broker server.
                    if (!pos.StopLoss.HasValue || !pos.TakeProfit.HasValue || pos.StopLoss.Value == 0.0)
                    {
                        bool isBuy = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase);
                        double defaultSl = (!pos.StopLoss.HasValue || pos.StopLoss.Value == 0.0)
                            ? (isBuy ? (entryPrice - (volatility * 2.5 * pipMultiplier * pipSize)) : (entryPrice + (volatility * 2.5 * pipMultiplier * pipSize)))
                            : pos.StopLoss.Value;

                        double defaultTp = (!pos.TakeProfit.HasValue || pos.TakeProfit.Value == 0.0)
                            ? (isBuy ? (entryPrice + (volatility * 4.5 * pipMultiplier * pipSize)) : (entryPrice - (volatility * 4.5 * pipMultiplier * pipSize)))
                            : pos.TakeProfit.Value;

                        var modifyResult = await HandleStopModificationAsync(pos.TicketId, defaultSl, defaultTp, cancellationToken);
                        if (modifyResult.Success)
                        {
                            _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                Guid.NewGuid(), pos.Symbol, "INIT_STOPS", volume,
                                $"Initialized statistical boundaries. SL: {defaultSl:F5}, TP: {defaultTp:F5}"
                            ));
                        }
                        continue;
                    }
                    #endregion

                    #region SCENARIO 1: Statistical Noise Floor & Pre-Emptive Capital Preservation (Fast-Act Cut)
                    // REASON: Do not wait for a hard Stop Loss. Calculate a Survival Probability.
                    // If loss exceeds the market noise floor (1.5 * Volatility) OR the momentum is violently against us,
                    // execute a pre-emptive FAST-ACT close. Understanding the trade is doomed.
                    if (floatingPips < 0)
                    {
                        double lossPips = Math.Abs(floatingPips);
                        double noiseFloorPips = volatility * 1.5 * pipMultiplier;

                        bool isBuy = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase);
                        bool regimeFlipped = (isBuy && currentRegime.Contains("Bearish")) || (!isBuy && currentRegime.Contains("Bullish"));

                        if (lossPips >= noiseFloorPips || regimeFlipped)
                        {
                            _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                Guid.NewGuid(), pos.Symbol, "SURVIVAL_CUT", volume,
                                $"[FAST-ACT] Noise floor broken ({lossPips:F1} pips) or regime flipped. Pre-emptive survival close executed."
                            ));

                            await HandlePartialCloseAsync(ticketId, volume, currentPrice, cancellationToken);
                            continue;
                        }
                    }
                    #endregion

                    #region SCENARIO 2: Multi-Stage Take Profit & Volume-Aware Scale-Out (VARS)
                    // REASON: Volume-Aware Sizing dictates that we scale out of heavy positions rapidly.
                    _scaleOutStages.TryAdd(ticketId, 0);
                    int currentStage = _scaleOutStages[ticketId];

                    double profitPercentage = currentBalance > 0 ? (double)((double)pos.UnrealizedPnl / currentBalance) * 100.0 : 0.0;

                    if (volume >= 1.0)
                    {
                        // Heavy-Volume Sizing Buffer: Tighten stops to lock in equity quickly
                        double peakExpectedMfePips = volatility * 3.0 * pipMultiplier;

                        // STAGE 1: Lock 50% cash flow at 50% of peak expected profit
                        if (floatingPips >= peakExpectedMfePips * 0.50 && currentStage == 0)
                        {
                            _scaleOutStages[ticketId] = 1;

                            double closeVol = Math.Round(volume * 0.50, 2);
                            if (closeVol < 0.01) closeVol = 0.01;

                            await HandlePartialCloseAsync(ticketId, closeVol, currentPrice, cancellationToken);

                            double beSl = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? (entryPrice + (2.0 * pipSize)) : (entryPrice - (2.0 * pipSize));
                            await HandleStopModificationAsync(ticketId, beSl, pos.TakeProfit, cancellationToken);

                            _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                Guid.NewGuid(), pos.Symbol, "VOLUME_SCALE_OUT", closeVol,
                                $"Heavy volume detected. Scaled out 50% at half of peak expected profit. SL moved to Break-Even."
                            ));
                            continue;
                        }
                    }
                    else
                    {
                        // Standard Volume Multi-Stage Target
                        // STAGE 1: Lock 30% of position when profit reaches 0.20% of the entire account balance
                        if (profitPercentage >= 0.20 && currentStage == 0)
                        {
                            _scaleOutStages[ticketId] = 1;

                            double closeVol = Math.Round(volume * 0.30, 2);
                            if (closeVol < 0.01) closeVol = 0.01;

                            await HandlePartialCloseAsync(ticketId, closeVol, currentPrice, cancellationToken);

                            double beSl = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? (entryPrice + (2.0 * pipSize)) : (entryPrice - (2.0 * pipSize));
                            await HandleStopModificationAsync(ticketId, beSl, pos.TakeProfit, cancellationToken);

                            _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                Guid.NewGuid(), pos.Symbol, "STAGE_1_TP", closeVol,
                                $"Profit hit 0.20% of balance. Scaled out 30% volume. SL to Break-Even: {beSl:F5}"
                            ));
                            continue;
                        }

                        // STAGE 2: Lock 50% of the remaining volume when profit reaches 0.50% of the account balance
                        if (profitPercentage >= 0.50 && currentStage == 1)
                        {
                            _scaleOutStages[ticketId] = 2;

                            double closeVol = Math.Round(volume * 0.50, 2);
                            if (closeVol < 0.01) closeVol = 0.01;

                            await HandlePartialCloseAsync(ticketId, closeVol, currentPrice, cancellationToken);

                            double lockSl = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? (entryPrice + (15.0 * pipSize)) : (entryPrice - (15.0 * pipSize));
                            await HandleStopModificationAsync(ticketId, lockSl, pos.TakeProfit, cancellationToken);

                            _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                Guid.NewGuid(), pos.Symbol, "STAGE_2_TP", closeVol,
                                $"Profit hit 0.50% of balance. Scaled out 50% remaining. SL locked at: {lockSl:F5}"
                            ));
                            continue;
                        }
                    }

                    // STAGE 3 (Peak-Utility Limit): Absolute mathematical boundary close
                    double peakExpectedPips = volatility * 4.0 * pipMultiplier;
                    if (floatingPips >= peakExpectedPips * 0.90)
                    {
                        _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                            Guid.NewGuid(), pos.Symbol, "PEAK_CLOSE", volume,
                            $"Statistical peak utility reached ({floatingPips:F1} pips). Full profit-lock close executed."
                        ));

                        await HandlePartialCloseAsync(ticketId, volume, currentPrice, cancellationToken);
                        continue;
                    }
                    #endregion

                    #region SCENARIO 3: Hysteresis-Protected Continuous SL/TP Tuning
                    // REASON: Continuous modification of stops on every tick ensures the protective boundaries
                    // dynamically adjust. Protects against MT5 Rate-Limits by enforcing a 15-second cooldown and 3-pip minimum change.
                    bool isBuyOrder = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase);
                    double targetSl = isBuyOrder
                        ? (currentPrice - (volatility * 1.8 * pipMultiplier * pipSize))
                        : (currentPrice + (volatility * 1.8 * pipMultiplier * pipSize));

                    // Rule: Never move Stop Loss backwards (never increase trade risk!)
                    bool isFavorableMove = isBuyOrder
                        ? (pos.StopLoss.HasValue && targetSl > pos.StopLoss.Value)
                        : (pos.StopLoss.HasValue && targetSl < pos.StopLoss.Value);

                    if (isFavorableMove)
                    {
                        bool isCooldownExpired = true;
                        bool isHysteresisMet = true;

                        if (_lastModifications.TryGetValue(ticketId, out var lastMod))
                        {
                            isCooldownExpired = (DateTime.UtcNow - lastMod.Time) >= TimeSpan.FromSeconds(15);
                            isHysteresisMet = Math.Abs(targetSl - lastMod.Sl) >= (3.0 * pipSize);
                        }

                        // FAST-ACT Bypass: If standard deviation spikes, bypass the 15s cooldown to secure capital
                        bool isVolatilitySpike = volatility > 0.40;

                        if ((isCooldownExpired && isHysteresisMet) || isVolatilitySpike)
                        {
                            var modRes = await HandleStopModificationAsync(pos.TicketId, targetSl, pos.TakeProfit, cancellationToken);
                            if (modRes.Success && isVolatilitySpike)
                            {
                                _decisionStream.PublishPositionManagement(new PositionManagementEvent(
                                    Guid.NewGuid(), pos.Symbol, "FAST_ACT_TRAIL", volume,
                                    $"[FAST-ACT] Volatility spike detected. Bypassed cooldown to secure Stop Loss at: {targetSl:F5}"
                                ));
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RISK GUARD ERROR] Failed to evaluate risk bounds for Position {pos.TicketId}: {ex.Message}");
                }
            }
        }
        #endregion

        #region Operations Handlers (Close & Modify)
        public async Task<ExecutionResult> HandleStopModificationAsync(
            string ticketId,
            double? sl,
            double? tp,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));

            // Atomic Concurrency Lock
            if (!_processingTickets.TryAdd(ticketId, 0))
            {
                return ExecutionResult.Failed("Ticket already undergoing processing.", 0);
            }

            try
            {
                var result = await _executionGateway.ModifyPositionAsync(ticketId, sl, tp, cancellationToken);

                if (result.Success)
                {
                    if (_openPositions.TryGetValue(ticketId, out var position))
                    {
                        position.StopLoss = sl;
                        position.TakeProfit = tp;
                    }
                    _lastModifications[ticketId] = (DateTime.UtcNow, sl ?? 0.0, tp ?? 0.0);
                }

                return result;
            }
            finally
            {
                _processingTickets.TryRemove(ticketId, out _);
            }
        }

        public async Task<ExecutionResult> HandlePartialCloseAsync(
            string ticketId,
            double closeVolume,
            double closePrice,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                throw new ArgumentException("Ticket ID cannot be empty.", nameof(ticketId));
            if (closeVolume <= 0)
                throw new ArgumentException("Close volume must be greater than zero.", nameof(closeVolume));

            // Atomic Concurrency Lock
            if (!_processingTickets.TryAdd(ticketId, 0))
            {
                return ExecutionResult.Failed("Ticket already undergoing processing.", 0);
            }

            try
            {
                if (!_openPositions.TryGetValue(ticketId, out var position))
                {
                    return ExecutionResult.Failed($"Position with ticket {ticketId} not found in open positions tracker.", 0);
                }

                if (closeVolume > position.Volume)
                {
                    return ExecutionResult.Failed($"Close volume {closeVolume} exceeds current position volume {position.Volume}.", 0);
                }

                var result = await _executionGateway.ClosePositionAsync(ticketId, closeVolume, cancellationToken);

                if (result.Success)
                {
                    if (Math.Abs(position.Volume - closeVolume) < 0.0001)
                    {
                        // Full close: Clean up the trackers
                        if (_openPositions.TryRemove(ticketId, out var closedPos))
                        {
                            closedPos.Status = "CLOSED";
                            closedPos.CurrentPrice = closePrice;
                            closedPos.UnrealizedPnl = RecalculatePnl(closedPos.Symbol, closedPos.Direction, closedPos.Volume, closedPos.EntryPrice, closePrice);
                            _closedPositions.Add(closedPos);

                            _scaleOutStages.TryRemove(ticketId, out _);
                            _lastModifications.TryRemove(ticketId, out _);

                            double pips = CalculateRealizedPips(closedPos.Symbol, closedPos.Direction, closedPos.EntryPrice, closePrice);
                            _ = RecordExperienceToAlphaGoEngineAsync(closedPos, closePrice, pips, CancellationToken.None);
                        }
                    }
                    else
                    {
                        // Partial close: Update volume and calculate exact Realized P&L
                        var updatedVolume = position.Volume - closeVolume;
                        var newPos = new PositionSnapshot(
                            ticketId: position.TicketId,
                            symbol: position.Symbol,
                            direction: position.Direction,
                            volume: updatedVolume,
                            entryPrice: position.EntryPrice,
                            currentPrice: closePrice,
                            stopLoss: position.StopLoss,
                            takeProfit: position.TakeProfit,
                            unrealizedPnl: RecalculatePnl(position.Symbol, position.Direction, updatedVolume, position.EntryPrice, closePrice),
                            riskExposure: updatedVolume * position.EntryPrice,
                            status: "OPEN"
                        );
                        _openPositions[ticketId] = newPos;

                        // Realized P&L accounting for the closed portion
                        var closedPortion = new PositionSnapshot(
                            ticketId: position.TicketId + "_PARTIAL_" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                            symbol: position.Symbol,
                            direction: position.Direction,
                            volume: closeVolume,
                            entryPrice: position.EntryPrice,
                            currentPrice: closePrice,
                            stopLoss: position.StopLoss,
                            takeProfit: position.TakeProfit,
                            unrealizedPnl: RecalculatePnl(position.Symbol, position.Direction, closeVolume, position.EntryPrice, closePrice),
                            riskExposure: closeVolume * position.EntryPrice,
                            status: "CLOSED"
                        );
                        _closedPositions.Add(closedPortion);

                        double pips = CalculateRealizedPips(position.Symbol, position.Direction, position.EntryPrice, closePrice);
                        _ = RecordExperienceToAlphaGoEngineAsync(closedPortion, closePrice, pips, CancellationToken.None);
                    }
                }

                return result;
            }
            finally
            {
                _processingTickets.TryRemove(ticketId, out _);
            }
        }

        public async Task SynchronizePositionsAsync(CancellationToken cancellationToken = default)
        {
            var activeSnapshots = await _executionGateway.GetPositionsAsync(cancellationToken);
            var activeTickets = new HashSet<string>(activeSnapshots.Select(s => s.TicketId));

            // 1. Move open positions that are no longer active to closed list (Detect MT5 SL/TP hits)
            foreach (var ticketId in _openPositions.Keys.ToList())
            {
                if (!activeTickets.Contains(ticketId))
                {
                    if (_openPositions.TryRemove(ticketId, out var position))
                    {
                        position.Status = "CLOSED";
                        _closedPositions.Add(position);

                        _scaleOutStages.TryRemove(ticketId, out _);
                        _lastModifications.TryRemove(ticketId, out _);

                        double pips = CalculateRealizedPips(position.Symbol, position.Direction, position.EntryPrice, position.CurrentPrice);
                        _ = RecordExperienceToAlphaGoEngineAsync(position, position.CurrentPrice, pips, CancellationToken.None);
                    }
                }
            }

            // 2. Add or update open positions with the latest gateway state
            foreach (var snapshot in activeSnapshots)
            {
                _openPositions[snapshot.TicketId] = snapshot;
            }

            // 3. Resolve Scoped Account statistics dynamically to run high-precision risk calculations
            double currentEquity = 100000.0;
            double currentBalance = 100000.0;
            double freeMargin = 100000.0;
            double marginLevel = 1000.0;

            if (_scopeFactory != null)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var accountRepo = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
                    var account = await accountRepo.GetByIdAsync("DEFAULT_ACCOUNT", cancellationToken)
                        ?? await accountRepo.GetByIdAsync("ACC_12345", cancellationToken);

                    if (account != null)
                    {
                        currentEquity = (double)account.Equity;
                        currentBalance = (double)account.Balance;
                        freeMargin = (double)account.FreeMargin;
                        marginLevel = account.Margin > 0 ? (double)(account.Equity / account.Margin) * 100.0 : 1000.0;
                    }
                }
                catch { /* Fallback to safe defaults on startup */ }
            }

            // 4. Trigger Active Position risk management, trailing, and protections
            await ManageActivePositionsRiskAsync(currentEquity, currentBalance, freeMargin, marginLevel, cancellationToken);
        }
        #endregion

        #region Helper Math & AlphaGo Learning Integration
        private static decimal RecalculatePnl(string symbol, string direction, double volume, double entryPrice, double currentPrice)
        {
            double multiplier = symbol.ToUpperInvariant().Contains("XAU") || symbol.ToUpperInvariant().Contains("GOLD") ? 100.0 : 100000.0;
            double diff = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase)
                ? (currentPrice - entryPrice)
                : (entryPrice - currentPrice);
            return (decimal)Math.Round(diff * volume * multiplier, 4);
        }

        private static double CalculateRealizedPips(string symbol, string direction, double entryPrice, double closePrice)
        {
            double diff = direction.Equals("Buy", StringComparison.OrdinalIgnoreCase)
                ? (closePrice - entryPrice)
                : (entryPrice - closePrice);

            double pipMultiplier = GetPipSize(symbol) > 0 ? (1.0 / GetPipSize(symbol)) : 10000.0;
            return Math.Round(diff * pipMultiplier, 1);
        }

        private static double GetPipSize(string symbol)
        {
            string upper = symbol.ToUpperInvariant();
            if (upper.Contains("JPY")) return 0.01;
            if (upper.Contains("XAU") || upper.Contains("GOLD")) return 0.1;
            return 0.0001;
        }

        /// <summary>
        /// Orchestrates the transition from Execution to Deep Learning.
        /// Sends the completed physical trade data to the AlphaGo-style replay engine for critique and JSON episode storage.
        /// </summary>
        private async Task RecordExperienceToAlphaGoEngineAsync(PositionSnapshot pos, double closePrice, double realizedPips, CancellationToken cancellationToken)
        {
            try
            {
                // Reconstruct mock state to feed the critique engine
                double vol = _marketDashboard != null ? _marketDashboard.Volatility : 0.15;
                double mom = _marketDashboard != null ? _marketDashboard.Momentum : 0.0;
                string reg = _marketDashboard != null ? _marketDashboard.MarketRegime : "Unknown";

                MarketState stateAtClose = new MarketState(pos.Symbol, DateTime.UtcNow, vol, mom, 1.0, 0.5, 0.5, 0.1, 50.0, reg);

                await _replayEngine.EvaluateAndStoreEpisodeAsync(
                    pos.Symbol,
                    pos.Direction,
                    pos.EntryPrice,
                    closePrice,
                    realizedPips,
                    stateAtClose,
                    0.85, // AI Confidence fallback
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LEARNING ENGINE ERROR] Failed to record replay episode: {ex.Message}");
            }
        }
        #endregion
    }
}