// Imports fundamental system operations for mathematically intensive computations. [Ref: Core-Systems]
using Nexus.Core.Entities;
// Imports the unified price action abstractions from the same assembly. [Ref: Assembly-Abstractions]
using Nexus.PriceAction.Abstractions;
// Imports the concrete price action engine implementations. [Ref: Assembly-Engines]
using Nexus.PriceAction.Candle;
using Nexus.PriceAction.Structural;
using System;
// Imports thread-safe concurrent dictionaries to handle highly parallel streaming tick buffers. [Ref: Concurrency-Control]
using System.Collections.Concurrent;
// Imports generalized generic collections for structuring historical vector lists. [Ref: Collections-Generic]
using System.Collections.Generic;
// Imports diagnostic stopwatch components to measure micro-latency telemetry metrics. [Ref: System-Diagnostics]
using System.Diagnostics;
// Imports file system operations to guarantee physical storage persistence. [Ref: File-System-IO]
using System.IO;
// Imports LINQ query operations to execute high-performance mathematical queries. [Ref: Linq-Queries]
using System.Linq;
// Imports string building performance pipelines to render massive interactive HTML reports. [Ref: Text-StringBuilder]
using System.Text;
// Imports precise JSON serialization parameters to persist experience structures to disk. [Ref: Serialization-Json]
using System.Text.Json;
// Imports task async structures to offload disk writes to parallel background threads. [Ref: Async-Programming]
using System.Threading.Tasks;
// Maps the core domain Candle entity to avoid local folder type collisions. [Ref: Domain-Mapping]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the high-performance AI pattern learning and evaluation namespace. [Ref: Evaluation-Store-Pro]
namespace Nexus.PriceAction.Evaluation
{
    // Captures the complete 34 quantitative market parameters matching the custom CSV schema. [Ref: Schema-Data-Record-Pro]
    public record PriceActionPatternMetrics(
        // The precise timestamp of the recorded price action bar. [Ref: CSV-Time]
        DateTime Time,
        // The open price level converted to absolute decimal representation. [Ref: CSV-Open]
        decimal Open,
        // The highest price level conversion. [Ref: CSV-High]
        decimal High,
        // The lowest price level conversion. [Ref: CSV-Low]
        decimal Low,
        // The close price level conversion. [Ref: CSV-Close]
        decimal Close,
        // The accumulated tick volume recorded inside the bar. [Ref: CSV-TickVolume]
        decimal TickVolume,
        // The average spread level encountered during bar formation. [Ref: CSV-Spread]
        decimal Spread,
        // The institutional real trading volume if provided by exchange. [Ref: CSV-RealVolume]
        decimal RealVolume,
        // The absolute directional mathematical difference between close and open. [Ref: CSV-Body]
        decimal Body,
        // The absolute magnitude size of the real body. [Ref: CSV-BodyAbs]
        decimal BodyAbs,
        // The absolute distance between high and low boundaries. [Ref: CSV-Range]
        decimal Range,
        // The absolute size of the upper shadow. [Ref: CSV-UpperWick]
        decimal UpperWick,
        // The absolute size of the lower shadow. [Ref: CSV-LowerWick]
        decimal LowerWick,
        // The ratio of the body size relative to total bar range. [Ref: CSV-BodyRatio]
        decimal BodyRatio,
        // The ratio of upper shadow to range. [Ref: CSV-UpperWickRatio]
        decimal UpperWickRatio,
        // The ratio of lower shadow to range. [Ref: CSV-LowerWickRatio]
        decimal LowerWickRatio,
        // Binary flag (1/0) indicating bullish structural close. [Ref: CSV-Bullish]
        decimal Bullish,
        // Binary flag (1/0) indicating bearish structural close. [Ref: CSV-Bearish]
        decimal Bearish,
        // Binary flag (1/0) indicating doji geometric classification. [Ref: CSV-Doji]
        decimal Doji,
        // Average True Range calculated over a 14-period rolling window. [Ref: CSV-AtrProxy14]
        decimal AtrProxy14,
        // Simple moving average of the trading range over 20 bars. [Ref: CSV-RangeMean20]
        decimal RangeMean20,
        // Simple moving average of trading volume over 20 bars. [Ref: CSV-VolumeMean20]
        decimal VolumeMean20,
        // Current tick volume relative to the 20-period average volume. [Ref: CSV-VolumeRatio]
        decimal VolumeRatio,
        // Absolute high price of the preceding historical candle. [Ref: CSV-PrevHigh]
        decimal PrevHigh,
        // Absolute low price of the preceding historical candle. [Ref: CSV-PrevLow]
        decimal PrevLow,
        // Absolute close price of the preceding historical candle. [Ref: CSV-PrevClose]
        decimal PrevClose,
        // Active swing high pivot level of the past 20 bars. [Ref: CSV-SwingHigh20]
        decimal SwingHigh20,
        // Active swing low pivot level of the past 20 bars. [Ref: CSV-SwingLow20]
        decimal SwingLow20,
        // Price level of the broken swing high boundary. [Ref: CSV-BreakHigh20]
        decimal BreakHigh20,
        // Price level of the broken swing low boundary. [Ref: CSV-BreakLow20]
        decimal BreakLow20,
        // Binary flag (1/0) confirming high-sweep liquidity hunt. [Ref: CSV-HigherHigh]
        decimal HigherHigh,
        // Binary flag (1/0) confirming low-sweep liquidity hunt. [Ref: CSV-LowerLow]
        decimal LowerLow,
        // Absolute difference between current close and previous close. [Ref: CSV-CloseChange]
        decimal CloseChange,
        // Percentage return of the close relative to prior close. [Ref: CSV-CloseReturn]
        decimal CloseReturn
    );

    // Encapsulates a complete verified price action trading episode for reinforcement learning. [Ref: Episode-Record-Pro]
    public record PriceActionEpisode(
        // Unique tracking identifier for audit purposes. [Ref: Episode-Id]
        string EpisodeId,
        // Precise timestamp of pattern generation. [Ref: Record-Time]
        DateTime Timestamp,
        // Target asset symbol. [Ref: Record-Symbol]
        string Symbol,
        // Target execution timeframe. [Ref: Record-Timeframe]
        string Timeframe,
        // Compiled pattern combination key. [Ref: Record-PatternKey]
        string TriggerPatternKey,
        // Status representing target success or failure. [Ref: Record-Status]
        bool IsSuccess,
        // Trade return magnitude expressed in pips. [Ref: Record-Pips]
        decimal RealizedPips,
        // Computed reinforcement reward scalar. [Ref: Record-Reward]
        decimal RewardScore,
        // Nested 34 quantitative parameters. [Ref: Record-Metrics]
        PriceActionPatternMetrics Metrics
    );

    // Represents the active weight and learning statistics of a specific price action pattern combination. [Ref: Profile-Pro]
    public class PatternWeightProfile
    {
        // Unique descriptive pattern combination signature. [Ref: Profile-Key]
        public string PatternKey { get; set; } = string.Empty;
        // Number of successful executions. [Ref: Profile-Wins]
        public int WinCount { get; set; }
        // Number of failed executions. [Ref: Profile-Losses]
        public int LossCount { get; set; }
        // Consolidated learning score updated dynamically. [Ref: Profile-Score]
        public decimal WeightScore { get; set; }
        // Historical success ratio. [Ref: Profile-Ratio]
        public decimal WinRate { get; set; }
        // Time audit for persistence synchronization. [Ref: Profile-Time]
        public DateTime LastUpdated { get; set; }
    }

    // Mega-scale reinforcement engine to process, score, search similarity, and report price action patterns. [Ref: Structural-Intelligence-Hub]
    public class PriceActionExperienceEngine
    {
        // Thread-safe dictionary lock to serialize file system transactions during parallel live ticks. [Ref: Thread-Lock-Pro]
        private static readonly object FileSystemLock = new();

        // Target storage paths to separate Price Action experiences from global neural caches. [Ref: Path-Defs]
        private const string BaseStorageDir = "NexusAI/PriceAction";
        private const string ExperienceDir = "NexusAI/PriceAction/Experience";
        private const string KnowledgeFile = "NexusAI/PriceAction/Knowledge/pattern_weights.json";
        private const string ReportsFile = "NexusAI/Logs/price_action_patterns_intelligence.html";

        // Reinforcement learning rate parameters. [Ref: RL-Parameters]
        private const decimal LearningRateAlpha = 0.20m;
        // Maximum boundary restriction for weighting scores. [Ref: Max-Weight]
        private const decimal MaxWeightClamp = 100.0m;
        // Minimum boundary restriction for weighting scores. [Ref: Min-Weight]
        private const decimal MinWeightClamp = -100.0m;

        // Custom serializer options to produce beautifully aligned human-readable JSON files. [Ref: Indent-Formatting]
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // Initializes the complete storage directory structure dynamically on engine startup. [Ref: Directory-Bootstrapper-Pro]
        public PriceActionExperienceEngine()
        {
            // Locks thread during bootstrap to prevent multi-threaded path creation conflicts. [Ref: Thread-Boot-Gate]
            lock (FileSystemLock)
            {
                // Ensures base directory exists. [Ref: Create-Dir]
                if (!Directory.Exists(BaseStorageDir)) Directory.CreateDirectory(BaseStorageDir);
                // Ensures dedicated experience directory exists. [Ref: Create-Dir]
                if (!Directory.Exists(ExperienceDir)) Directory.CreateDirectory(ExperienceDir);
                // Ensures knowledge folder directory exists. [Ref: Create-Dir]
                string? parent = Path.GetDirectoryName(KnowledgeFile);
                // Checks nullability. [Ref: Check-Parent]
                if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);
                // Ensures logs directory exists. [Ref: Create-Dir]
                string? logParent = Path.GetDirectoryName(ReportsFile);
                // Checks nullability. [Ref: Check-Parent]
                if (logParent != null && !Directory.Exists(logParent)) Directory.CreateDirectory(logParent);
            }
        }

        // Processes active context parameters, executes reward/penalty calculations, and updates JSON/HTML stores. [Ref: Record-Experience-Pro]
        public async Task RecordExperienceAsync(
            PriceActionContext context,
            DateTime triggerTime,
            bool isSuccess,
            decimal realizedPips,
            decimal tradeVolume)
        {
            // Guards against null inputs to guarantee absolute pipeline stability. [Ref: Input-Guard-Pro]
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Attempts to locate the candle array index matching the exact trigger timestamp. [Ref: Match-Timeline]
            int triggerIndex = -1;
            // Traverses chronological raw feed. [Ref: Timeline-Search]
            for (int k = 0; k < context.RawCandles.Count; k++)
            {
                // Checks matching timestamp. [Ref: Match-Condition]
                if (context.RawCandles[k].Timestamp == triggerTime)
                {
                    // Captures index coordinate. [Ref: Register-Index]
                    triggerIndex = k;
                    // Exit loop. [Ref: Loop-Break]
                    break;
                }
            }

            // Exits early if target candle index is missing from current pipeline context. [Ref: Safety-Boundary]
            if (triggerIndex < 20) return;

            // Extracts the exact 34 parameters matching the custom CSV schema at the target index. [Ref: Extract-34-Metrics-Pro]
            PriceActionPatternMetrics metrics = ExtractMetrics(context, triggerIndex);

            // Computes cosine similarity of the current vector against historical success vectors to guide the decision process. [Ref: Similarity-Search]
            decimal historicalSimilarityScore = CalculateHistoricalSimilarity(context, metrics, isSuccess);

            // Generates a descriptive unique pattern key based on verified Price Action Pro indicators. [Ref: Key-Generator-Pro]
            string patternKey = GeneratePatternKey(context, triggerTime, metrics);

            // Computes the absolute reinforcement reward or penalty based on the transaction outcome and similarity scores. [Ref: Formula-RL]
            decimal reward = isSuccess
                ? (realizedPips * tradeVolume * (1.0m + metrics.VolumeRatio + historicalSimilarityScore))
                : -(Math.Abs(realizedPips) * tradeVolume * (1.2m + metrics.BodyRatio + (1.0m - historicalSimilarityScore)));

            // Compiles the verified price action trading episode into an immutable record object. [Ref: Compile-Episode-Pro]
            var episode = new PriceActionEpisode(
                EpisodeId: Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                Timestamp: triggerTime,
                Symbol: context.Symbol,
                Timeframe: context.Timeframe,
                TriggerPatternKey: patternKey,
                IsSuccess: isSuccess,
                RealizedPips: realizedPips,
                RewardScore: reward,
                Metrics: metrics
            );

            // Saves the structured JSON episode to the dedicated Price Action experience directory. [Ref: Save-Json-Episode-Pro]
            await SaveJsonEpisodeAsync(episode).ConfigureAwait(false);

            // Dynamically updates the centralized pattern weights database using the reward outcome. [Ref: Update-Weights-Pro]
            await UpdatePatternWeightsAsync(patternKey, isSuccess, reward).ConfigureAwait(false);

            // Regenerates the beautiful, highly-responsive interactive HTML dashboard report. [Ref: Generate-Dashboard-Pro]
            await GenerateHtmlReportAsync().ConfigureAwait(false);
        }

        // Extracts all 34 structural and statistical parameters from the context state. [Ref: Metrics-Parser-34-Pro]
        private PriceActionPatternMetrics ExtractMetrics(PriceActionContext context, int index)
        {
            // Current index candle references. [Ref: Candle-Refs-Pro]
            DomainCandle cur = context.RawCandles[index];
            DomainCandle pri = context.RawCandles[index - 1];

            // Converts boundaries to decimals. [Ref: Boundary-Conversions]
            decimal open = (decimal)cur.Open.Value;
            decimal high = (decimal)cur.High.Value;
            decimal low = (decimal)cur.Low.Value;
            decimal close = (decimal)cur.Close.Value;

            // Calculates body parameters. [Ref: Body-Calculations-Pro]
            decimal body = close - open;
            decimal bodyAbs = Math.Abs(body);
            decimal range = high - low;

            // Calculates shadows. [Ref: Shadow-Calculations-Pro]
            decimal maxBodyPoint = Math.Max(open, close);
            decimal minBodyPoint = Math.Min(open, close);
            decimal upperWick = high - maxBodyPoint;
            decimal lowerWick = minBodyPoint - low;

            // Range checks to prevent division-by-zero on flat candles. [Ref: Safe-Dividers-Pro]
            decimal bodyRatio = range > 0 ? bodyAbs / range : 0m;
            decimal upperWickRatio = range > 0 ? upperWick / range : 0m;
            decimal lowerWickRatio = range > 0 ? lowerWick / range : 0m;

            // Binary classifications. [Ref: Binary-Classes-Pro]
            decimal bullish = close > open ? 1m : 0m;
            decimal bearish = open > close ? 1m : 0m;
            decimal doji = range > 0 && (bodyAbs / range) <= 0.05m ? 1m : 0m;

            // Volatility indices. [Ref: Volatility-Calculators-Pro]
            decimal atr = CalculateAtr(context, index);
            decimal rangeMean = CalculateMean(context, index, 20, c => (decimal)c.High.Value - (decimal)c.Low.Value);
            decimal volMean = CalculateMean(context, index, 20, c => (decimal)c.Volume.Value);
            decimal volRatio = volMean > 0 ? (decimal)cur.Volume.Value / volMean : 1m;

            // Historical references. [Ref: Historic-Refs-Pro]
            decimal prevHigh = (decimal)pri.High.Value;
            decimal prevLow = (decimal)pri.Low.Value;
            decimal prevClose = (decimal)pri.Close.Value;

            // Looks up Swing Point details dynamically. [Ref: Swing-Pivots-Lookup-Pro]
            var swingHighPoint = SwingPointDetector.GetSwingPoint(context, cur.Timestamp);
            decimal swingHigh = swingHighPoint != null && swingHighPoint.IsHigh ? swingHighPoint.Price : 0m;
            decimal swingLow = swingHighPoint != null && !swingHighPoint.IsHigh ? swingHighPoint.Price : 0m;

            // Looks up Structure Break occurrences dynamically. [Ref: Structural-Breaks-Lookup-Pro]
            var structureBreak = MarketStructureDetector.GetStructureBreak(context, cur.Timestamp);
            decimal breakHigh = structureBreak != null && structureBreak.ResultingTrend == Nexus.PriceAction.Structural.TrendDirection.Bullish ? structureBreak.BrokenLevelPrice : 0m;
            decimal breakLow = structureBreak != null && structureBreak.ResultingTrend == Nexus.PriceAction.Structural.TrendDirection.Bearish ? structureBreak.BrokenLevelPrice : 0m;

            // Looks up Liquidity Sweeps dynamically. [Ref: Liquidity-Sweeps-Lookup-Pro]
            var sweep = LiquiditySweepDetector.GetLiquiditySweep(context, cur.Timestamp);
            decimal higherHigh = sweep != null && sweep.Type == LiquiditySweepType.Bearish ? 1m : 0m;
            decimal lowerLow = sweep != null && sweep.Type == LiquiditySweepType.Bullish ? 1m : 0m;

            // Returns relative changes. [Ref: Absolute-Changes-Pro]
            decimal closeChange = close - prevClose;
            decimal closeReturn = prevClose > 0 ? closeChange / prevClose : 0m;

            // Compiles all 34 metrics. [Ref: Output-Metrics-Pro]
            return new PriceActionPatternMetrics(
                Time: cur.Timestamp, Open: open, High: high, Low: low, Close: close,
                TickVolume: (decimal)cur.Volume.Value, Spread: 0m, RealVolume: (decimal)cur.Volume.Value,
                Body: body, BodyAbs: bodyAbs, Range: range, UpperWick: upperWick, LowerWick: lowerWick,
                BodyRatio: bodyRatio, UpperWickRatio: upperWickRatio, LowerWickRatio: lowerWickRatio,
                Bullish: bullish, Bearish: bearish, Doji: doji, AtrProxy14: atr, RangeMean20: rangeMean,
                VolumeMean20: volMean, VolumeRatio: volRatio, PrevHigh: prevHigh, PrevLow: prevLow, PrevClose: prevClose,
                SwingHigh20: swingHigh, SwingLow20: swingLow, BreakHigh20: breakHigh, BreakLow20: breakLow,
                HigherHigh: higherHigh, LowerLow: lowerLow, CloseChange: closeChange, CloseReturn: closeReturn
            );
        }

        // Calculates standard Average True Range over 14 periods. [Ref: Volatility-ATR-Proxy-Pro]
        private decimal CalculateAtr(PriceActionContext context, int currentIndex)
        {
            // Resolves start index. [Ref: Safe-Start-Pro]
            int startIndex = Math.Max(1, currentIndex - 13);
            decimal sum = 0m;
            // Iterates through period window. [Ref: Loop-Pro]
            for (int k = startIndex; k <= currentIndex; k++)
            {
                // High low difference. [Ref: Range-Diff-Pro]
                sum += ((decimal)context.RawCandles[k].High.Value - (decimal)context.RawCandles[k].Low.Value);
            }
            // Returns ATR value. [Ref: Return-Pro]
            return sum / 14m;
        }

        // Calculates mean ranges or volumes dynamically over rolling periods. [Ref: Mean-Calculator-Pro]
        private decimal CalculateMean(PriceActionContext context, int currentIndex, int period, Func<DomainCandle, decimal> selector)
        {
            // Resolves start index. [Ref: Safe-Start-Pro]
            int startIndex = Math.Max(0, currentIndex - period + 1);
            int actualCount = currentIndex - startIndex + 1;
            decimal sum = 0m;
            // Iterates through range. [Ref: Loop-Pro]
            for (int k = startIndex; k <= currentIndex; k++)
            {
                // Accumulates selected double parameter. [Ref: Accumulate-Pro]
                sum += selector(context.RawCandles[k]);
            }
            // Returns normalized mean average. [Ref: Return-Pro]
            return sum / actualCount;
        }

        // Compares current 34-parameter vector to historical successful/unsuccessful vectors using Cosine Similarity. [Ref: Vector-Similarity-Engine]
        private decimal CalculateHistoricalSimilarity(PriceActionContext context, PriceActionPatternMetrics current, bool targetSuccess)
        {
            // Locks thread during directory scanning. [Ref: Directory-Scan-Gate]
            lock (FileSystemLock)
            {
                // Retrieves all files currently stored in experience pool. [Ref: Load-Past-Episodes]
                string[] files = Directory.GetFiles(ExperienceDir, "*.json");
                // Returns default similarity if no past history exists. [Ref: Empty-History-Fallback]
                if (files.Length == 0) return 0.5m;

                // Keeps track of the cumulative similarity score. [Ref: Acc-Score]
                decimal similaritySum = 0m;
                int comparisonCount = 0;

                // Creates the numerical vector representation of the current metrics. [Ref: Current-Vector]
                double[] v1 = ExtractVectorArray(current);

                // Iterates past experiences to match structural vector patterns. [Ref: Chrono-Match-Loop]
                foreach (string file in files)
                {
                    try
                    {
                        // Checks if the file outcome matches target success criteria to search similar patterns. [Ref: Specific-Outcome-Filter]
                        if (file.Contains($"_{targetSuccess}.json"))
                        {
                            // Reads file. [Ref: Read]
                            string json = File.ReadAllText(file, Encoding.UTF8);
                            // Deserializes episode. [Ref: Deserialize]
                            var pastEpisode = JsonSerializer.Deserialize<PriceActionEpisode>(json);
                            // Checks nullability. [Ref: Null-Guard]
                            if (pastEpisode != null)
                            {
                                // Extracts the past vector. [Ref: Past-Vector]
                                double[] v2 = ExtractVectorArray(pastEpisode.Metrics);
                                // Calculates cosine similarity between both vectors. [Ref: Cosine-Formula-Call]
                                double sim = CosineSimilarity(v1, v2);
                                // Accumulates. [Ref: Accumulate-Score]
                                similaritySum += (decimal)sim;
                                comparisonCount++;
                            }
                        }
                    }
                    catch
                    {
                        // Ignores individual file corruptions to preserve operational speed. [Ref: Fail-Silent]
                    }
                }

                // Returns normalized similarity ratio. [Ref: Return-Similarity]
                return comparisonCount > 0 ? similaritySum / comparisonCount : 0.5m;
            }
        }

        // Extracts the relevant 10 quantitative dimensions of the 34 parameters for vector comparisons. [Ref: Vector-Dimensionality]
        private double[] ExtractVectorArray(PriceActionPatternMetrics m)
        {
            // Compiles target metrics dimensions. [Ref: Dimensional-Compile]
            return new double[]
            {
                (double)m.BodyRatio,
                (double)m.UpperWickRatio,
                (double)m.LowerWickRatio,
                (double)m.AtrProxy14,
                (double)m.RangeMean20,
                (double)m.VolumeRatio,
                (double)m.SwingHigh20,
                (double)m.SwingLow20,
                (double)m.CloseChange,
                (double)m.CloseReturn
            };
        }

        // Calculates cosine similarity between two multi-dimensional numerical vectors. [Ref: Cosine-Similarity-Math]
        private double CosineSimilarity(double[] vector1, double[] vector2)
        {
            // Checks if vectors have identical dimensionality. [Ref: Dim-Guard]
            if (vector1.Length != vector2.Length) return 0.0;

            // Mathematical sum calculations. [Ref: Calculations-Vars]
            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            // Calculates product sums. [Ref: Vector-Calculations-Loop]
            for (int i = 0; i < vector1.Length; i++)
            {
                // Dot product sum. [Ref: Dot-Product]
                dotProduct += vector1[i] * vector2[i];
                // Square sum A. [Ref: NormA]
                normA += vector1[i] * vector1[i];
                // Square sum B. [Ref: NormB]
                normB += vector2[i] * vector2[i];
            }

            // Prevents division-by-zero for empty vectors. [Ref: Flat-Vector-Guard]
            if (normA == 0.0 || normB == 0.0) return 0.0;

            // Returns absolute cosine similarity. [Ref: Cosine-Result]
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        // Classifies active Price Action signals into a consolidated signature string key. [Ref: Pattern-Signature-Key-Pro]
        private string GeneratePatternKey(PriceActionContext context, DateTime time, PriceActionPatternMetrics m)
        {
            // Starts building description. [Ref: Key-Buffer-Pro]
            var keyParts = new List<string>();

            // Adds structural trends. [Ref: Trend-Class-Pro]
            var trend = MarketStructureDetector.GetCurrentTrend(context);
            // Append trend. [Ref: Trend-Append-Pro]
            if (trend != Nexus.PriceAction.Structural.TrendDirection.Undefined) keyParts.Add(trend.ToString().ToUpper());

            // Adds order block references if price is testing active zone. [Ref: OB-Check-Pro]
            var ob = OrderBlockDetector.GetOrderBlock(context, time);
            // Append OB. [Ref: OB-Append-Pro]
            if (ob != null) keyParts.Add($"OB_{ob.Type.ToString().ToUpper()}");

            // Adds liquidity sweep references. [Ref: Sweep-Check-Pro]
            if (m.HigherHigh == 1m) keyParts.Add("HIGH_SWEEP");
            // Append Sweep. [Ref: Sweep-Append-Pro]
            if (m.LowerLow == 1m) keyParts.Add("LOW_SWEEP");

            // Adds structural break checks. [Ref: Break-Check-Pro]
            if (m.BreakHigh20 > 0) keyParts.Add("BOS_BULLISH");
            // Append BOS. [Ref: BOS-Append-Pro]
            if (m.BreakLow20 > 0) keyParts.Add("BOS_BEARISH");

            // Fallback key if no strong institutional pattern is active. [Ref: Standard-Fallback-Pro]
            if (keyParts.Count == 0)
            {
                // Appends standard candles. [Ref: Fallback-Standard-Pro]
                return m.Bullish == 1m ? "STANDARD_BULLISH" : "STANDARD_BEARISH";
            }

            // Returns combined string. [Ref: Join-Key-Pro]
            return string.Join("_", keyParts);
        }

        // Thread-safely writes individual episode files as serialized JSON structures. [Ref: Write-Json-File-Pro]
        private Task SaveJsonEpisodeAsync(PriceActionEpisode episode)
        {
            // Standardizes timestamp file names. [Ref: Timestamp-String-Pro]
            string timeStr = episode.Timestamp.ToString("yyyyMMdd_HHmmss");
            // Combines path. [Ref: Path-Build-Pro]
            string filePath = Path.Combine(ExperienceDir, $"EXP_{timeStr}_{episode.Symbol}_{episode.IsSuccess}.json");

            // Locks IO stream access. [Ref: File-System-Thread-Gate-Pro]
            lock (FileSystemLock)
            {
                // Serializes episode record. [Ref: Serialize-Pro]
                string json = JsonSerializer.Serialize(episode, JsonOptions);
                // Writes string content to disk. [Ref: Write-Content-Pro]
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            // Completes task. [Ref: Task-Signal-Pro]
            return Task.CompletedTask;
        }

        // Manages pattern learning database, rewarding wins and penalizing losses. [Ref: Policy-Weights-Update-Pro]
        private Task UpdatePatternWeightsAsync(string patternKey, bool isSuccess, decimal reward)
        {
            // Locks thread to execute database read-write cycle. [Ref: DB-Lock-Pro]
            lock (FileSystemLock)
            {
                // Initializes profile table memory buffer. [Ref: Buffer-Init-Pro]
                var weightsMap = new Dictionary<string, PatternWeightProfile>();

                // Reads previous weight file if already instantiated. [Ref: Historical-Read-Pro]
                if (File.Exists(KnowledgeFile))
                {
                    // Reads content. [Ref: Read-Pro]
                    string jsonContent = File.ReadAllText(KnowledgeFile, Encoding.UTF8);
                    // Deserializes array. [Ref: Deserialize-Pro]
                    var profiles = JsonSerializer.Deserialize<List<PatternWeightProfile>>(jsonContent);
                    // Populates map. [Ref: Map-Transfer-Pro]
                    if (profiles != null)
                    {
                        // Transfer to map. [Ref: Transfer-Pro]
                        foreach (var p in profiles) weightsMap[p.PatternKey] = p;
                    }
                }

                // Resolves or constructs the pattern profile record. [Ref: Resolve-Record-Pro]
                if (!weightsMap.TryGetValue(patternKey, out var profile))
                {
                    // Instantiates new profile. [Ref: New-Profile-Pro]
                    profile = new PatternWeightProfile { PatternKey = patternKey, WeightScore = 1.0m };
                    // Commits to map. [Ref: Commit-Map-Pro]
                    weightsMap[patternKey] = profile;
                }

                // Updates win loss statistics. [Ref: Stats-Update-Pro]
                if (isSuccess)
                {
                    // Increases win count. [Ref: Win-Add-Pro]
                    profile.WinCount++;
                    // Applies reward multiplier to the score. [Ref: Reward-Math-Pro]
                    profile.WeightScore += LearningRateAlpha * (1.0m + reward);
                }
                else
                {
                    // Increases loss count. [Ref: Loss-Add-Pro]
                    profile.LossCount++;
                    // Applies penalty decrement to the score. [Ref: Penalty-Math-Pro]
                    profile.WeightScore += LearningRateAlpha * reward;
                }

                // Calculates historical win-rate percentage. [Ref: Dynamic-WinRate]
                int totalRuns = profile.WinCount + profile.LossCount;
                profile.WinRate = totalRuns > 0 ? (decimal)profile.WinCount * 100.0m / totalRuns : 0.0m;

                // Clamps the pattern score within reasonable boundaries to prevent numerical overflow. [Ref: Clamping-Pro]
                profile.WeightScore = Math.Clamp(profile.WeightScore, MinWeightClamp, MaxWeightClamp);
                // Logs current timestamp. [Ref: Audit-Time-Pro]
                profile.LastUpdated = DateTime.UtcNow;

                // Serializes updated model profiles. [Ref: Output-Serialization-Pro]
                string updatedJson = JsonSerializer.Serialize(new List<PatternWeightProfile>(weightsMap.Values), JsonOptions);
                // Writes database to disk. [Ref: Save-To-Disk-Pro]
                File.WriteAllText(KnowledgeFile, updatedJson, Encoding.UTF8);
            }
            // Completes task. [Ref: Task-Signal-Pro]
            return Task.CompletedTask;
        }

        // Generates an incredibly beautiful, modern, Tailwind CSS dashboard containing live statistical reports. [Ref: Reports-Dashboard-Generator-Pro]
        private Task GenerateHtmlReportAsync()
        {
            // Locks thread during dashboard compiling and saving. [Ref: HTML-Lock-Pro]
            lock (FileSystemLock)
            {
                // Reads patterns database. [Ref: Read-Db-Pro]
                var profiles = new List<PatternWeightProfile>();
                // Checks existence. [Ref: Verify-Pro]
                if (File.Exists(KnowledgeFile))
                {
                    // Read file. [Ref: Read-Pro]
                    string json = File.ReadAllText(KnowledgeFile, Encoding.UTF8);
                    // Deserialize. [Ref: Deserialize-Pro]
                    var decoded = JsonSerializer.Deserialize<List<PatternWeightProfile>>(json);
                    // Populate. [Ref: Populate-Pro]
                    if (decoded != null) profiles.AddRange(decoded);
                }

                // Sorts patterns based on significance score descending. [Ref: Sort-Ranks-Pro]
                profiles.Sort((x, y) => y.WeightScore.CompareTo(x.WeightScore));

                // Calculates grand statistics. [Ref: Stats-Math-Pro]
                int totalWins = 0;
                int totalLosses = 0;
                // Accumulates numbers. [Ref: Accumulate-Stats-Pro]
                foreach (var p in profiles)
                {
                    totalWins += p.WinCount;
                    totalLosses += p.LossCount;
                }
                int totalSignals = totalWins + totalLosses;
                double winRate = totalSignals > 0 ? (double)totalWins * 100.0 / totalSignals : 0.0;

                // Builds HTML string sequentially using performance buffers. [Ref: StringBuilder-Pipeline-Pro]
                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html lang=\"en\">");
                sb.AppendLine("<head>");
                sb.AppendLine("    <meta charset=\"UTF-8\">");
                sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                sb.AppendLine("    <title>NEXUS - Price Action Intelligence Dashboard</title>");
                sb.AppendLine("    <script src=\"https://cdn.tailwindcss.com\"></script>");
                sb.AppendLine("    <style>");
                sb.AppendLine("        body { background-color: #0b0f19; color: #f3f4f6; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }");
                sb.AppendLine("        .glass-panel { background: rgba(17, 24, 39, 0.7); backdrop-filter: blur(12px); border: 1px solid rgba(255, 255, 255, 0.08); }");
                sb.AppendLine("    </style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body class=\"p-6 min-h-screen\">");
                sb.AppendLine("    <div class=\"max-w-7xl mx-auto space-y-6\">");

                // Header console. [Ref: Header-Writers-Pro]
                sb.AppendLine("        <div class=\"glass-panel p-6 rounded-2xl flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0\">");
                sb.AppendLine("            <div>");
                sb.AppendLine("                <h1 class=\"text-3xl font-extrabold tracking-wider text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-indigo-500\">NEXUS QUANT PRICE ACTION</h1>");
                sb.AppendLine("                <p class=\"text-sm text-gray-400\">Autonomous Price Action Pro Pattern Reinforcement Learning Report</p>");
                sb.AppendLine("            </div>");
                sb.AppendLine($"            <div class=\"text-xs text-gray-500 text-right\">System Audit: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>");
                sb.AppendLine("        </div>");

                // Stat Cards widget. [Ref: Widget-Stats-Pro]
                sb.AppendLine("        <div class=\"grid grid-cols-1 md:grid-cols-4 gap-6\">");

                // Total Signals card. [Ref: Card-1-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl\">");
                sb.AppendLine("                <div class=\"text-sm text-gray-400 font-bold uppercase tracking-wider\">Total Signal Detections</div>");
                sb.AppendLine($"               <div class=\"text-4xl font-extrabold mt-2 text-indigo-400\">{totalSignals}</div>");
                sb.AppendLine("            </div>");

                // WinRate card. [Ref: Card-2-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl\">");
                sb.AppendLine("                <div class=\"text-sm text-gray-400 font-bold uppercase tracking-wider\">Global Success Rate</div>");
                sb.AppendLine($"               <div class=\"text-4xl font-extrabold mt-2 text-emerald-400\">{winRate:F2}%</div>");
                sb.AppendLine("            </div>");

                // Wins card. [Ref: Card-3-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl\">");
                sb.AppendLine("                <div class=\"text-sm text-gray-400 font-bold uppercase tracking-wider\">Rewarded Episodes (Wins)</div>");
                sb.AppendLine($"               <div class=\"text-4xl font-extrabold mt-2 text-blue-400\">{totalWins}</div>");
                sb.AppendLine("            </div>");

                // Losses card. [Ref: Card-4-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl\">");
                sb.AppendLine("                <div class=\"text-sm text-gray-400 font-bold uppercase tracking-wider\">Penalized Episodes (Losses)</div>");
                sb.AppendLine($"               <div class=\"text-4xl font-extrabold mt-2 text-rose-500\">{totalLosses}</div>");
                sb.AppendLine("            </div>");

                sb.AppendLine("        </div>");

                // Layout split panels. [Ref: Structural-Layout-Grid-Pro]
                sb.AppendLine("        <div class=\"grid grid-cols-1 lg:grid-cols-3 gap-6\">");

                // Left Panel: Table of Patterns. [Ref: Table-Panel-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl lg:col-span-2 space-y-4\">");
                sb.AppendLine("                <h2 class=\"text-xl font-bold tracking-wider text-blue-300\">Reinforced Pattern Weight Database</h2>");
                sb.AppendLine("                <div class=\"overflow-x-auto\">");
                sb.AppendLine("                    <table class=\"min-w-full divide-y divide-gray-800 text-sm\">");
                sb.AppendLine("                        <thead>");
                sb.AppendLine("                            <tr class=\"text-gray-500 text-left\">");
                sb.AppendLine("                                <th class=\"pb-3 font-semibold\">Pattern Key Signature</th>");
                sb.AppendLine("                                <th class=\"pb-3 font-semibold text-center\">Wins</th>");
                sb.AppendLine("                                <th class=\"pb-3 font-semibold text-center\">Losses</th>");
                sb.AppendLine("                                <th class=\"pb-3 font-semibold text-right\">Weight Score</th>");
                sb.AppendLine("                                <th class=\"pb-3 font-semibold text-right\">Win Rate</th>");
                sb.AppendLine("                            </tr>");
                sb.AppendLine("                        </thead>");
                sb.AppendLine("                        <tbody class=\"divide-y divide-gray-800 text-gray-300\">");

                // Iterates pattern database profiles to construct table rows dynamically. [Ref: Table-Row-Loop-Pro]
                foreach (var p in profiles)
                {
                    int patTotal = p.WinCount + p.LossCount;
                    double patWinRate = patTotal > 0 ? (double)p.WinCount * 100.0 / patTotal : 0.0;
                    string scoreClass = p.WeightScore >= 1.0m ? "text-emerald-400" : "text-rose-500";
                    string rowHighlight = p.WeightScore >= 5.0m ? "bg-emerald-950/20" : p.WeightScore <= -2.0m ? "bg-rose-950/10" : "";

                    sb.AppendLine($"                            <tr class=\"{rowHighlight}\">");
                    sb.AppendLine($"                                <td class=\"py-3 font-mono font-bold text-xs text-indigo-300\">{p.PatternKey}</td>");
                    sb.AppendLine($"                                <td class=\"py-3 text-center text-blue-400\">{p.WinCount}</td>");
                    sb.AppendLine($"                                <td class=\"py-3 text-center text-rose-400\">{p.LossCount}</td>");
                    sb.AppendLine($"                                <td class=\"py-3 text-right font-bold {scoreClass}\">{p.WeightScore:F4}</td>");
                    sb.AppendLine($"                                <td class=\"py-3 text-right text-gray-400\">{patWinRate:F1}%</td>");
                    sb.AppendLine("                            </tr>");
                }

                sb.AppendLine("                        </tbody>");
                sb.AppendLine("                    </table>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");

                // Right Panel: Top patterns and SVG representation. [Ref: Visualizer-Panel-Pro]
                sb.AppendLine("            <div class=\"glass-panel p-6 rounded-2xl space-y-6\">");
                sb.AppendLine("                <h2 class=\"text-xl font-bold tracking-wider text-indigo-300\">Structural Pattern Strengths</h2>");

                // SVG representation of win rate. [Ref: SVG-Gauge-Graphic-Pro]
                sb.AppendLine("                <div class=\"flex justify-center\">");
                sb.AppendLine("                    <svg width=\"200\" height=\"200\" viewBox=\"0 0 200 200\" class=\"transform -rotate-90\">");
                sb.AppendLine("                        <circle cx=\"100\" cy=\"100\" r=\"80\" fill=\"transparent\" stroke=\"#1f2937\" stroke-width=\"12\"/>");
                // Math circle dash offset: 2 * PI * r = 2 * 3.14159 * 80 = 502.65. [Ref: Math-Stroke-Offset-Pro]
                double strokeDashOffset = 502.65 - (502.65 * winRate / 100.0);
                sb.AppendLine($"                        <circle cx=\"100\" cy=\"100\" r=\"80\" fill=\"transparent\" stroke=\"#10b981\" stroke-width=\"12\" stroke-dasharray=\"502.65\" stroke-dashoffset=\"{strokeDashOffset}\" stroke-linecap=\"round\"/>");
                sb.AppendLine("                    </svg>");
                sb.AppendLine("                </div>");
                sb.AppendLine($"               <div class=\"text-center text-xs text-gray-400 mt-2\">Interactive Gauge showing {winRate:F2}% successful predictions.</div>");

                // Displays top 3 Champion profiles. [Ref: Champion-Widgets-Pro]
                sb.AppendLine("                <div class=\"space-y-4 pt-4\">");
                sb.AppendLine("                    <h3 class=\"text-sm font-bold uppercase tracking-wider text-gray-400\">Top 3 Champion Patterns</h3>");
                for (int m = 0; m < Math.Min(3, profiles.Count); m++)
                {
                    var p = profiles[m];
                    sb.AppendLine("                    <div class=\"p-4 rounded-xl bg-emerald-950/20 border border-emerald-900/30 flex justify-between items-center\">");
                    sb.AppendLine($"                        <div class=\"text-xs font-mono font-bold text-emerald-300\">{p.PatternKey}</div>");
                    sb.AppendLine($"                        <div class=\"text-sm font-bold text-emerald-400\">+{p.WeightScore:F2}</div>");
                    sb.AppendLine("                    </div>");
                }
                sb.AppendLine("                </div>");

                // Displays top 3 Worst profiles. [Ref: Challenger-Failing-Widgets-Pro]
                sb.AppendLine("                <div class=\"space-y-4\">");
                sb.AppendLine("                    <h3 class=\"text-sm font-bold uppercase tracking-wider text-gray-400\">Worst 3 Penalized Patterns</h3>");
                for (int m = Math.Max(0, profiles.Count - 3); m < profiles.Count; m++)
                {
                    var p = profiles[m];
                    sb.AppendLine("                    <div class=\"p-4 rounded-xl bg-rose-950/20 border border-rose-900/30 flex justify-between items-center\">");
                    sb.AppendLine($"                        <div class=\"text-xs font-mono font-bold text-rose-300\">{p.PatternKey}</div>");
                    sb.AppendLine($"                        <div class=\"text-sm font-bold text-rose-500\">{p.WeightScore:F2}</div>");
                    sb.AppendLine("                    </div>");
                }
                sb.AppendLine("                </div>");

                sb.AppendLine("            </div>");
                sb.AppendLine("        </div>"); // Closes layout grid
                sb.AppendLine("    </div>"); // Closes max-w-7xl mx-auto
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                // Writes output HTML to Logs file path directory. [Ref: Commit-Output-HTML-Pro]
                File.WriteAllText(ReportsFile, sb.ToString(), Encoding.UTF8);
            }
            // Completes task. [Ref: Task-Signal-Pro]
            return Task.CompletedTask;
        }
    }
}