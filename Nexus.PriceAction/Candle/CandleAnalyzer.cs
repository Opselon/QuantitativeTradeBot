// Imports system types and exceptions. [Ref: Core-Sys]
// Imports threading components for cancellation tokens. [Ref: Core-Threading]
// Imports task parallelism types for asynchronous operations. [Ref: Core-Tasks]
// Imports abstraction interfaces for the engine. [Ref: Proj-Dependency]
using Nexus.PriceAction.Abstractions;
// Imports the calculators required for candle logic. [Ref: Proj-Dependency]
using Nexus.PriceAction.Candle.Calculators;
// Imports the models holding the engine results. [Ref: Proj-Dependency]
using Nexus.PriceAction.Candle.Models;
// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]

// Defines the namespace for the main candle analysis engine. [Ref: Arch-Layer]
namespace Nexus.PriceAction.Candle
{
    // Declares the main orchestrator engine class for raw candles, implementing the pipeline interface. [Ref: Engine-Def]
    public class CandleAnalyzer : IPriceActionEngine
    {
        // Implements the asynchronous Analyze method required by the IPriceActionEngine interface. [Ref: Interface-Impl]
        public Task<PriceActionContext> AnalyzeAsync(PriceActionContext context, CancellationToken cancellationToken = default)
        {
            // Validates that the input context is not null to prevent null reference exceptions. [Ref: Guard-Clause]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Begins iterating over the immutable collection of raw domain candles in the context. [Ref: Iteration]
            foreach (var candle in context.RawCandles)
            {
                // Checks if cancellation was requested and throws an exception to safely halt the loop. [Ref: Task-Cancellation]
                cancellationToken.ThrowIfCancellationRequested();

                // Calls the RangeCalculator to determine the total price distance of the current candle. [Ref: Math-Call]
                decimal range = RangeCalculator.Calculate(candle);

                // Calls the BodyCalculator to determine the absolute size of the candle body. [Ref: Math-Call]
                decimal body = BodyCalculator.Calculate(candle);

                // Calls the ShadowCalculator to determine the size of the upper wick. [Ref: Math-Call]
                decimal upperWick = ShadowCalculator.CalculateUpperShadow(candle);

                // Calls the ShadowCalculator to determine the size of the lower wick. [Ref: Math-Call]
                decimal lowerWick = ShadowCalculator.CalculateLowerShadow(candle);

                // Determines if the current candle is structurally bullish. [Ref: Math-Call]
                bool isBullish = BodyCalculator.IsBullish(candle);

                // Determines if the current candle is structurally bearish. [Ref: Math-Call]
                bool isBearish = BodyCalculator.IsBearish(candle);

                // Safely calculates the ratio of the body relative to the range, protecting against zero division. [Ref: Math-Logic]
                decimal bodyRatio = range > 0 ? body / range : 0m;

                // Calculates the normalized directional momentum using the range and prices. [Ref: Math-Call]
                decimal momentum = MomentumCalculator.CalculateDirectionalMomentum(candle, range);

                // Classifies the geometrical shape of the candle based on the calculated mathematical parameters. [Ref: Logic-Call]
                var candleType = CandleClassifier.Classify(body, upperWick, lowerWick, range, isBullish);

                // Instantiates a new immutable result record holding all the computed properties. [Ref: Object-Creation]
                var result = new CandleAnalysisResult(
                    // Maps the original domain candle. [Ref: Prop-Map]
                    SourceCandle: candle,
                    // Maps the calculated body size. [Ref: Prop-Map]
                    BodySize: body,
                    // Maps the calculated upper shadow size. [Ref: Prop-Map]
                    UpperShadowSize: upperWick,
                    // Maps the calculated lower shadow size. [Ref: Prop-Map]
                    LowerShadowSize: lowerWick,
                    // Maps the total calculated range. [Ref: Prop-Map]
                    TotalRange: range,
                    // Maps the ratio of body to range. [Ref: Prop-Map]
                    BodyToRangeRatio: bodyRatio,
                    // Maps the directional momentum scalar. [Ref: Prop-Map]
                    MomentumDirectional: momentum,
                    // Maps the classified candle type enum. [Ref: Prop-Map]
                    Type: candleType,
                    // Maps the bullish boolean flag. [Ref: Prop-Map]
                    IsBullish: isBullish,
                    // Maps the bearish boolean flag. [Ref: Prop-Map]
                    IsBearish: isBearish
                );

                // Thread-safely adds or updates the result in the concurrent dictionary keyed by candle timestamp. [Ref: State-Update]
                context.CandleResults.AddOrUpdate(candle.Timestamp, result, (key, oldValue) => result);
            }

            // Wraps the modified context in a completed task to satisfy the async signature. [Ref: Async-Return]
            return Task.FromResult(context);
        }
    }
}