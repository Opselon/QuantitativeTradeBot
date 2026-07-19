using Nexus.Core.Entities;
using Nexus.Core.Enums;
using Nexus.Core.ValueObjects;
using Nexus.MarketIntelligence;
using Nexus.MarketIntelligence.Aggregation;
using Nexus.MarketIntelligence.Features;
using Nexus.MarketIntelligence.Memory;
using Nexus.MarketIntelligence.MultiTimeframe;
using Nexus.MarketIntelligence.Quality;
using Nexus.MarketIntelligence.Regimes;

namespace Nexus.Tests.Unit.Intelligence
{
    public class MarketIntelligencePhase09Tests
    {
        private readonly Symbol _symbol = new Symbol("EURUSD");
        private readonly Timeframe _tfM1 = new Timeframe(TimeframeType.M1);

        #region Helper: Generate Dummy Candles

        private List<Candle> CreateCandles(int count, double startPrice, double trendFactor, double noiseAmplitude, double volumeValue)
        {
            var candles = new List<Candle>();
            var rnd = new Random(42);
            double currentPrice = startPrice;

            for (int i = 0; i < count; i++)
            {
                double change = (trendFactor * (i + 1)) + ((rnd.NextDouble() - 0.5) * noiseAmplitude);
                double open = currentPrice;
                double close = currentPrice + change;
                double high = Math.Max(open, close) + (rnd.NextDouble() * (noiseAmplitude * 0.2));
                double low = Math.Min(open, close) - (rnd.NextDouble() * (noiseAmplitude * 0.2));

                var candle = new Candle(
                    _symbol,
                    _tfM1,
                    DateTime.UtcNow.AddMinutes(-count + i),
                    new Price(open),
                    new Price(high),
                    new Price(low),
                    new Price(close),
                    new Volume(volumeValue)
                );
                candles.Add(candle);
                currentPrice = close;
            }

            return candles;
        }

        #endregion

        [Fact]
        public void MultiTimeframeEngine_Synchronize_AlignsTrendAndMomentum()
        {
            // Arrange
            var engine = new MultiTimeframeEngine();
            var tfData = new Dictionary<TimeframeType, IReadOnlyList<Candle>>();

            // Generate strong bullish candles for higher timeframes, mixed for smaller
            var bullCandles = CreateCandles(10, 1.1000, 0.0020, 0.0005, 1000.0);
            var neutralCandles = CreateCandles(10, 1.1000, 0.0000, 0.0005, 500.0);

            tfData[TimeframeType.D1] = bullCandles;
            tfData[TimeframeType.H4] = bullCandles;
            tfData[TimeframeType.H1] = bullCandles;
            tfData[TimeframeType.M30] = neutralCandles;
            tfData[TimeframeType.M15] = neutralCandles;
            tfData[TimeframeType.M5] = neutralCandles;
            tfData[TimeframeType.M1] = neutralCandles;

            // Act
            var state = engine.Synchronize("EURUSD", tfData);

            // Assert
            Assert.Equal("EURUSD", state.Symbol);
            Assert.Contains(state.TrendAlignment, new[] { "Bullish", "Mixed" });
            Assert.True(state.ConsensusScore > 50.0, "Consensus score should skew bullish under strong higher timeframe trends.");
            Assert.Equal(7, state.TimeframeAssessments.Count);
        }

        [Fact]
        public void MarketRegimeDetector_DetectRegimes_IdentifiesTrendingRegime()
        {
            // Arrange
            var detector = new MarketRegimeDetector();
            // Create strong upward trending candles
            var trendingCandles = CreateCandles(20, 1.1000, 0.0015, 0.0002, 800.0);

            // Act
            var regimes = detector.DetectRegimes(trendingCandles, 1.5);
            var dominant = detector.GetDominantRegime(regimes);

            // Assert
            Assert.True(regimes.ContainsKey("Trending"));
            Assert.True(regimes["Trending"].Confidence > 50.0, "Confidence should be strong for trending data.");
            Assert.Equal("Trending", dominant.Regime);
        }

        [Fact]
        public void MarketRegimeDetector_DetectRegimes_IdentifiesRangeRegime()
        {
            // Arrange
            var detector = new MarketRegimeDetector();
            // Create tight range candles (trendFactor is 0)
            var rangeCandles = CreateCandles(20, 1.1000, 0.0, 0.0001, 400.0);

            // Act
            var regimes = detector.DetectRegimes(rangeCandles, 2.0);
            var dominant = detector.GetDominantRegime(regimes);

            // Assert
            Assert.True(regimes.ContainsKey("Range"));
            Assert.True(regimes["Range"].Confidence > 60.0, "Sideways range confidence should be high.");
        }

        [Fact]
        public void MarketRegimeDetector_DetectRegimes_IdentifiesBreakoutRegime()
        {
            // Arrange
            var detector = new MarketRegimeDetector();
            var candles = CreateCandles(10, 1.1000, 0.0, 0.0001, 100.0);

            // Add a massive breakout candle at the end
            var breakoutCandle = new Candle(
                _symbol,
                _tfM1,
                DateTime.UtcNow,
                new Price(1.1000),
                new Price(1.1250),
                new Price(1.0990),
                new Price(1.1240),
                new Volume(5000.0) // high volume
            );
            candles.Add(breakoutCandle);

            // Act
            var regimes = detector.DetectRegimes(candles, 2.0);

            // Assert
            Assert.True(regimes.ContainsKey("Breakout"));
            Assert.True(regimes["Breakout"].Confidence > 30.0, "Breakout confidence should trigger on extreme price close.");
        }

        [Fact]
        public void FeatureExtractor_Extract_IsDeterministic()
        {
            // Arrange
            var extractor = new FeatureExtractor();
            var candles = CreateCandles(15, 1.1000, 0.0005, 0.0001, 500.0);
            var tick = new Tick(_symbol, DateTime.UtcNow, 1.1050, 1.1052);
            var session = new MarketSession("London Session", TimeSpan.FromHours(8), TimeSpan.FromHours(16));

            var mtfState = new MultiTimeframeState(
                "EURUSD",
                DateTime.UtcNow,
                "Bullish",
                "Bullish",
                "Stable",
                "Neutral",
                82.5,
                new Dictionary<TimeframeType, TimeframeAssessment>()
            );

            // Act
            var featuresA = extractor.Extract("EURUSD", candles, tick, session, mtfState);
            var featuresB = extractor.Extract("EURUSD", candles, tick, session, mtfState);

            float[] arrayA = featuresA.ToFloatArray();
            float[] arrayB = featuresB.ToFloatArray();

            double[] doubleA = featuresA.ToDoubleArray();
            double[] doubleB = featuresB.ToDoubleArray();

            // Assert
            Assert.Equal(featuresA.Features.Count, featuresB.Features.Count);
            Assert.Equal(arrayA.Length, arrayB.Length);
            for (int i = 0; i < arrayA.Length; i++)
            {
                Assert.Equal(arrayA[i], arrayB[i]);
                Assert.Equal(doubleA[i], doubleB[i]);
            }

            // Verify cross-timeframe integration
            Assert.True(featuresA.Features.ContainsKey("cross_trend_alignment_score"));
            Assert.Equal(100.0, featuresA.Features["cross_trend_alignment_score"]);
        }

        [Fact]
        public void MarketQualityEvaluator_Evaluate_GeneratesCorrectQualityScores()
        {
            // Arrange
            var evaluator = new MarketQualityEvaluator();
            var candles = CreateCandles(15, 1.1000, 0.0005, 0.0002, 600.0);

            // Act
            var score = evaluator.EvaluateQuality(candles, 1.5, 2.0);

            // Assert
            Assert.True(score.OverallScore > 0.0 && score.OverallScore <= 100.0);
            Assert.True(score.Liquidity > 0.0);
            Assert.True(score.Spread > 0.0);
            Assert.True(score.ExecutionRisk > 0.0);
        }

        [Fact]
        public async Task LocalStateMemory_FindSimilarStates_ReturnsClosestMatches()
        {
            // Arrange
            var memory = new LocalStateMemory();
            var state1 = new MarketState("EURUSD", DateTime.UtcNow, 0.2, 0.5, 0.8, 0.7, 0.9, 0.1, 50.0, "Trending");
            var features1 = new ExtractedFeatures("EURUSD", DateTime.UtcNow, new Dictionary<string, double>
            {
                { "feat1", 1.0 },
                { "feat2", 2.0 }
            });

            var state2 = new MarketState("EURUSD", DateTime.UtcNow, 0.3, -0.4, 0.7, 0.6, 0.8, 0.2, 50.0, "Range");
            var features2 = new ExtractedFeatures("EURUSD", DateTime.UtcNow, new Dictionary<string, double>
            {
                { "feat1", -1.0 },
                { "feat2", -2.0 }
            });

            await memory.StoreStateAsync(state1, features1, "Success");
            await memory.StoreStateAsync(state2, features2, "Failure");

            var queryFeatures = new ExtractedFeatures("EURUSD", DateTime.UtcNow, new Dictionary<string, double>
            {
                { "feat1", 0.9 },
                { "feat2", 1.9 }
            });

            // Act
            var matches = await memory.FindSimilarStatesAsync("EURUSD", queryFeatures, 0.90, CancellationToken.None);

            // Assert
            Assert.Single(matches);
            Assert.Equal("Success", matches[0].Outcome);
            Assert.True(matches[0].Similarity > 0.99, "Similarity should be close to 1.0.");
        }

        [Fact]
        public async Task MarketIntelligenceEngine_ProcessMarketData_ProducesCompleteSnapshot()
        {
            // Arrange
            var mtf = new MultiTimeframeEngine();
            var regime = new MarketRegimeDetector();
            var quality = new MarketQualityEvaluator();
            var extractor = new FeatureExtractor();
            var memory = new LocalStateMemory();

            var engine = new MarketIntelligenceEngine(mtf, regime, quality, extractor, memory);

            var tfCandles = new Dictionary<TimeframeType, IReadOnlyList<Candle>>();
            var candles = CreateCandles(10, 1.1000, 0.0002, 0.0001, 600.0);
            foreach (TimeframeType type in Enum.GetValues(typeof(TimeframeType)))
            {
                tfCandles[type] = candles;
            }

            var tick = new Tick(_symbol, DateTime.UtcNow, 1.1020, 1.1022);
            var session = new MarketSession("US Session", TimeSpan.FromHours(13), TimeSpan.FromHours(21));

            // Act
            var snapshot = await engine.ProcessMarketDataAsync(
                "EURUSD",
                tfCandles,
                tick,
                session,
                2.0,
                2.0,
                CancellationToken.None
            );

            // Assert
            Assert.NotNull(snapshot);
            Assert.Equal("EURUSD", snapshot.Symbol);
            Assert.NotNull(snapshot.MarketState);
            Assert.NotNull(snapshot.MultiTimeframeState);
            Assert.NotNull(snapshot.DominantRegime);
            Assert.NotNull(snapshot.QualityScore);
            Assert.NotNull(snapshot.Features);

            // Check that the domain MarketState is correctly generated and populated
            Assert.Equal("EURUSD", snapshot.MarketState.Symbol);
            Assert.Equal(snapshot.DominantRegime.Regime, snapshot.MarketState.MarketRegime);
            Assert.True(snapshot.MarketState.Volatility > 0.0);
            Assert.True(snapshot.MarketState.Liquidity > 0.0);
        }

        [Fact]
        public void TickAggregator_CompilesTicks_AndTriggersRollover()
        {
            // Arrange
            var aggregator = new TickAggregator();
            var timeframe = new Timeframe(TimeframeType.M1);
            var baseTime = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

            int eventCount = 0;
            aggregator.CandleCompleted += (sender, candle) =>
            {
                eventCount++;
                Assert.Equal("EURUSD", candle.Symbol.Name);
            };

            // Ingest ticks inside the same minute
            var t1 = new Tick(_symbol, baseTime.AddSeconds(10), 1.1000, 1.1002);
            var t2 = new Tick(_symbol, baseTime.AddSeconds(30), 1.1010, 1.1012);

            // Act
            var r1 = aggregator.IngestTick(t1, timeframe);
            var r2 = aggregator.IngestTick(t2, timeframe);

            // Assert
            Assert.Null(r1);
            Assert.Null(r2);

            // Now cross into the next minute (rollover)
            var t3 = new Tick(_symbol, baseTime.AddMinutes(1).AddSeconds(5), 1.1020, 1.1022);
            var r3 = aggregator.IngestTick(t3, timeframe);

            Assert.NotNull(r3);
            Assert.Equal(1, eventCount);
            Assert.Equal(1.1000, r3.Open.Value);
            Assert.Equal(1.1010, r3.High.Value);
            Assert.Equal(1.1000, r3.Low.Value);
            Assert.Equal(1.1010, r3.Close.Value);
            Assert.Equal(2.0, r3.Volume.Value); // 2 accumulated volume units
        }
    }
}
