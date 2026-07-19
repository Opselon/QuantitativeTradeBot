// Imports base system types and diagnostics telemetry wrappers. [Ref: Core-Libraries]
// Imports thread-safe concurrent dictionaries to record high-performance diagnostic maps. [Ref: Concurrency-Control]
// Imports multi-threading cancellation signals. [Ref: Thread-Abort]
// Imports task parallelism abstractions for non-blocking async execution. [Ref: Async-Execution]
// Imports core project abstractions and pipeline engine contracts. [Ref: Project-Contracts]
using Nexus.PriceAction.Abstractions;
// Imports the concrete engines constructed in previous phases. [Ref: Price-Action-Engines]
using Nexus.PriceAction.Candle;
using Nexus.PriceAction.Structural;
using System.Collections.Concurrent;
// Imports system collections to hold the execution chains. [Ref: Collections-Generic]
// Imports stopwatch mechanics to profile latency metrics down to microsecond thresholds. [Ref: Diagnostics-Timing]
using System.Diagnostics;
// Imports compiler runtime optimization annotations. [Ref: Native-Optimizations]
using System.Runtime.CompilerServices;

// Defines the master orchestrator namespace for price action execution chains. [Ref: Pipeline-Pro]
namespace Nexus.PriceAction.Pipeline
{
    // Encapsulates high-performance runtime diagnostics detailing engine execution speeds. [Ref: Telemetry-Record]
    public record PipelineDiagnostics(
        // Total ticks elapsed during pipeline orchestration. [Ref: Elapsed-Ticks]
        long TotalExecutionTimeTicks,
        // Normalized execution time mapped to milliseconds. [Ref: Elapsed-Milliseconds]
        double TotalExecutionTimeMilliseconds,
        // Individual engine execution latencies mapped by their type name. [Ref: Engine-Latencies]
        IReadOnlyDictionary<string, double> EngineLatenciesMs
    );

    // Master pipeline class to sequentially orchestrate and profile advanced price action analysis. [Ref: SRP-Pipeline]
    public class PriceActionPipeline
    {
        // Internal chronological sequence of execution engines. [Ref: Engine-Chain]
        private readonly List<IPriceActionEngine> _engines;

        // Thread-safely caches dynamic telemetry records bound to specific context lifespans. [Ref: Diagnostics-Cache]
        private static readonly ConditionalWeakTable<PriceActionContext, PipelineDiagnostics> DiagnosticsCache = new();

        // Default constructor to register the six core Price Action Pro engines in precise dependency order. [Ref: Constructor]
        public PriceActionPipeline()
        {
            // Instantiates the engine sequence collection. [Ref: List-Instantiation]
            _engines = new List<IPriceActionEngine>
            {
                // Phase 1: Calculates basic candle geometry, body ratios, and classifications. [Ref: Register-Phase-1]
                new CandleAnalyzer(),
                // Phase 2: Resolves multi-depth structural swing high and swing low points. [Ref: Register-Phase-2]
                new SwingPointDetector(),
                // Phase 3: Identifies institutional Fair Value Gaps and mitigation wicks. [Ref: Register-Phase-3]
                new FairValueGapDetector(),
                // Phase 4: Determines key Order Block zones using FVG and candle parameters. [Ref: Register-Phase-4]
                new OrderBlockDetector(),
                // Phase 5: Tracks market trends, Breaks of Structure (BOS), and CHoCH events. [Ref: Register-Phase-5]
                new MarketStructureDetector(),
                // Phase 6: Identifies stop-hunt sweeps above/below confirmed swing pivots. [Ref: Register-Phase-6]
                new LiquiditySweepDetector()
            };
        }

        // Custom constructor allowing runtime DI dependency injection of custom engines. [Ref: DI-Constructor]
        public PriceActionPipeline(IEnumerable<IPriceActionEngine> customEngines)
        {
            // Guards against null collections to ensure operational stability. [Ref: DI-Guard]
            if (customEngines == null) throw new ArgumentNullException(nameof(customEngines));
            // populates custom engine configurations. [Ref: DI-Assignment]
            _engines = new List<IPriceActionEngine>(customEngines);
        }

        // Public query API to retrieve performance telemetry metadata from context memory safely. [Ref: Telemetry-Query]
        public static PipelineDiagnostics? GetDiagnostics(PriceActionContext context)
        {
            // Tries to locate the context instance within the thread-safe dynamic weak reference table. [Ref: WeakTable-Query-Diagnostics]
            if (DiagnosticsCache.TryGetValue(context, out var diagnostics))
            {
                // Returns the successfully retrieved telemetry data object. [Ref: Return-Diagnostics]
                return diagnostics;
            }
            // Returns null if the specified context has no valid diagnostics registered yet. [Ref: Fallback-Diagnostics-Null]
            return null;
        }

        // Sequential execution engine with nanosecond performance profiling. [Ref: Pipeline-Orchestrate]
        public async Task<PriceActionContext> ExecuteAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Enforces strict input validation to protect system execution boundaries. [Ref: Input-Guard-Pipeline]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Instantiates the stopwatch profile tracker. [Ref: Profiler-Init]
            var totalStopwatch = Stopwatch.StartNew();

            // Prepares dictionary to map isolated latencies per engine. [Ref: Latency-Map-Init]
            var engineLatencies = new ConcurrentDictionary<string, double>();

            // Sequentially executes the registered engine pipeline chain. [Ref: Loop-Invariance]
            foreach (var engine in _engines)
            {
                // Interruption check prior to dispatching the next engine phase. [Ref: Interruption-Safety]
                cancellationToken.ThrowIfCancellationRequested();

                // Obtains name representation for performance logging. [Ref: Get-Engine-Name]
                string engineName = engine.GetType().Name;

                // Instantiates the phase-specific stopwatch. [Ref: Phase-Profiler-Init]
                var phaseStopwatch = Stopwatch.StartNew();

                try
                {
                    // Asynchronously dispatches the current price action engine phase. [Ref: Async-Dispatch-Phase]
                    await engine.AnalyzeAsync(context, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Captures and wraps the exception with engine context to assist quantum logging layers. [Ref: Exception-Shielding]
                    throw new InvalidOperationException($"Price Action Pipeline execution failed at phase: {engineName}.", ex);
                }
                finally
                {
                    // Halts the phase-specific stopwatch. [Ref: Stop-Phase-Stopwatch]
                    phaseStopwatch.Stop();
                    // Converts ticks to milliseconds and registers the latency. [Ref: Log-Phase-Latency]
                    double latencyMs = (double)phaseStopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
                    // Adds the computed metrics to the temporary map safely. [Ref: Commit-Phase-Latency]
                    engineLatencies.TryAdd(engineName, latencyMs);
                }
            }

            // Halts total pipeline stopwatch. [Ref: Stop-Total-Stopwatch]
            totalStopwatch.Stop();

            // Computes total pipeline latency milliseconds. [Ref: Total-Latency-Math]
            double totalMs = (double)totalStopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

            // Compiles total pipeline metrics into an immutable record object. [Ref: Compile-Diagnostics]
            // Compiles total pipeline metrics into an immutable record object. [Ref: Compile-Diagnostics]
            var diagnostics = new PipelineDiagnostics(
                TotalExecutionTimeTicks: totalStopwatch.ElapsedTicks,
                TotalExecutionTimeMilliseconds: totalMs,
                EngineLatenciesMs: engineLatencies
            );

            // Commits performance diagnostics data to context storage by overwriting any existing key. [Ref: Commit-Store-Diagnostics-Fix]
            DiagnosticsCache.AddOrUpdate(context, diagnostics);

            // Returns the structurally enriched context with diagnostic profiles. [Ref: Pipeline-Complete]
            return context;
        }
    }
}