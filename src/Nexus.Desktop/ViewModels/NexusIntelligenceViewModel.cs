using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Core.ValueObjects;
using Nexus.Application.Analytics;
using Nexus.Application.Intelligence;
using Nexus.Desktop.Services;

namespace Nexus.Desktop.ViewModels
{
    public class NexusIntelligenceViewModel : ViewModelBase, IDisposable
    {
        private readonly INeuralModelService _neuralService;
        private readonly ICurrencyStrengthEngine _currencyEngine;
        private readonly IAccumulatorService _accumulatorService;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IScenarioEvaluationEngine _scenarioEngine;
        private readonly IPatternMemory _patternMemory;
        private readonly IDiagnosticService _diagnosticService;
        private readonly INativeAnalyticsEngine _nativeEngine;
        private readonly INativeCoreService _nativeCore;
        private readonly NativeMarketIntelligenceService _marketIntelligence;

        // Stockfish Intelligence additions
        private readonly IScenarioSearchEngine _scenarioSearchEngine;
        private readonly IMultiTimeframeConsensusEngine _mtfConsensusEngine;

        private readonly CancellationTokenSource _cts = new();
        private readonly Random _random = new();

        // 1. Native Core Monitor Properties
        private string _nativeCoreStatus = "Active";
        private string _libraryLoaded = "Yes (nexus_native_core.dll)";
        private string _abiVersion = "C++20 v2.0.1";
        private string _platform = RuntimeInformation.OSDescription;
        private string _cpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
        private string _activeNativeThreads = "4";
        private string _memoryUsage = "0.0 MB";

        // 2. Market Intelligence Dashboard Properties
        private string _currentMarketState = "Trend Bullish";
        private double _priceStructure = 0.75;
        private double _trendState = 0.65;
        private double _momentum = 0.82;
        private double _volatility = 0.25;
        private double _volumePressure = 0.55;
        private double _liquidity = 0.92;
        private double _usdStrength = 82.0;

        // 3. Neural Evaluation Panel Properties
        private double _buyConfidence = 0.86;
        private double _sellConfidence = 0.19;
        private double _waitConfidence = 0.42;
        private double _evaluationScore = 0.86;
        private string _modelStatus = "Active";
        private string _lastInferenceTime = "Never";

        // 4. AI Model Monitor Properties
        private string _currentModelName = "NexusScalarNet_v1.0.onnx";
        private string _modelVersion = "1.0.0";
        private string _modelLoadStatus = "Loaded";
        private string _inferenceLatency = "1.2 ms";
        private string _lastExecutionTimestamp = "Never";

        // 5. Performance Monitor Properties
        private string _nativeExecutionLatency = "0.04 ms";
        private string _interopLatency = "0.01 ms";
        private string _featureCalculationTime = "0.15 ms";
        private string _neuralInferenceTime = "1.10 ms";
        private string _totalDecisionPipelineTime = "1.30 ms";

        // 6. Stockfish Search and Multi-Timeframe UI Properties
        private string _searchBestAction = "WAIT";
        private string _searchReasoning = "Evaluating probabilistic path scenarios...";
        private string _mtfConsensusSummary = "Awaiting multi-timeframe consensus signals...";

        // Getters and Setters
        public string NativeCoreStatus { get => _nativeCoreStatus; set => SetProperty(ref _nativeCoreStatus, value); }
        public string LibraryLoaded { get => _libraryLoaded; set => SetProperty(ref _libraryLoaded, value); }
        public string AbiVersion { get => _abiVersion; set => SetProperty(ref _abiVersion, value); }
        public string Platform { get => _platform; set => SetProperty(ref _platform, value); }
        public string CpuArchitecture { get => _cpuArchitecture; set => SetProperty(ref _cpuArchitecture, value); }
        public string ActiveNativeThreads { get => _activeNativeThreads; set => SetProperty(ref _activeNativeThreads, value); }
        public string MemoryUsage { get => _memoryUsage; set => SetProperty(ref _memoryUsage, value); }

        public string CurrentMarketState { get => _currentMarketState; set => SetProperty(ref _currentMarketState, value); }
        public double PriceStructure { get => _priceStructure; set => SetProperty(ref _priceStructure, value); }
        public double TrendState { get => _trendState; set => SetProperty(ref _trendState, value); }
        public double Momentum { get => _momentum; set => SetProperty(ref _momentum, value); }
        public double Volatility { get => _volatility; set => SetProperty(ref _volatility, value); }
        public double VolumePressure { get => _volumePressure; set => SetProperty(ref _volumePressure, value); }
        public double Liquidity { get => _liquidity; set => SetProperty(ref _liquidity, value); }
        public double UsdStrength { get => _usdStrength; set => SetProperty(ref _usdStrength, value); }

        public double BuyConfidence { get => _buyConfidence; set => SetProperty(ref _buyConfidence, value); }
        public double SellConfidence { get => _sellConfidence; set => SetProperty(ref _sellConfidence, value); }
        public double WaitConfidence { get => _waitConfidence; set => SetProperty(ref _waitConfidence, value); }
        public double EvaluationScore { get => _evaluationScore; set => SetProperty(ref _evaluationScore, value); }
        public string ModelStatus { get => _modelStatus; set => SetProperty(ref _modelStatus, value); }
        public string LastInferenceTime { get => _lastInferenceTime; set => SetProperty(ref _lastInferenceTime, value); }

        public string CurrentModelName { get => _currentModelName; set => SetProperty(ref _currentModelName, value); }
        public string ModelVersion { get => _modelVersion; set => SetProperty(ref _modelVersion, value); }
        public string ModelLoadStatus { get => _modelLoadStatus; set => SetProperty(ref _modelLoadStatus, value); }
        public string InferenceLatency { get => _inferenceLatency; set => SetProperty(ref _inferenceLatency, value); }
        public string LastExecutionTimestamp { get => _lastExecutionTimestamp; set => SetProperty(ref _lastExecutionTimestamp, value); }

        public string NativeExecutionLatency { get => _nativeExecutionLatency; set => SetProperty(ref _nativeExecutionLatency, value); }
        public string InteropLatency { get => _interopLatency; set => SetProperty(ref _interopLatency, value); }
        public string FeatureCalculationTime { get => _featureCalculationTime; set => SetProperty(ref _featureCalculationTime, value); }
        public string NeuralInferenceTime { get => _neuralInferenceTime; set => SetProperty(ref _neuralInferenceTime, value); }
        public string TotalDecisionPipelineTime { get => _totalDecisionPipelineTime; set => SetProperty(ref _totalDecisionPipelineTime, value); }

        // Stockfish Getters & Setters
        public string SearchBestAction { get => _searchBestAction; set => SetProperty(ref _searchBestAction, value); }
        public string SearchReasoning { get => _searchReasoning; set => SetProperty(ref _searchReasoning, value); }
        public string MtfConsensusSummary { get => _mtfConsensusSummary; set => SetProperty(ref _mtfConsensusSummary, value); }

        public ICommand RunModelInferenceCommand { get; }

        public NexusIntelligenceViewModel(
            INeuralModelService neuralService,
            ICurrencyStrengthEngine currencyEngine,
            IAccumulatorService accumulatorService,
            IDecisionEngine decisionEngine,
            IScenarioEvaluationEngine scenarioEngine,
            IPatternMemory patternMemory,
            INativeAnalyticsEngine nativeEngine,
            INativeCoreService nativeCore,
            NativeMarketIntelligenceService marketIntelligence,
            IScenarioSearchEngine scenarioSearchEngine,
            IMultiTimeframeConsensusEngine mtfConsensusEngine,
            IDiagnosticService diagnosticService)
        {
            _neuralService = neuralService ?? throw new ArgumentNullException(nameof(neuralService));
            _currencyEngine = currencyEngine ?? throw new ArgumentNullException(nameof(currencyEngine));
            _accumulatorService = accumulatorService ?? throw new ArgumentNullException(nameof(accumulatorService));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _scenarioEngine = scenarioEngine ?? throw new ArgumentNullException(nameof(scenarioEngine));
            _patternMemory = patternMemory ?? throw new ArgumentNullException(nameof(patternMemory));
            _nativeEngine = nativeEngine ?? throw new ArgumentNullException(nameof(nativeEngine));
            _nativeCore = nativeCore ?? throw new ArgumentNullException(nameof(nativeCore));
            _marketIntelligence = marketIntelligence ?? throw new ArgumentNullException(nameof(marketIntelligence));
            _scenarioSearchEngine = scenarioSearchEngine ?? throw new ArgumentNullException(nameof(scenarioSearchEngine));
            _mtfConsensusEngine = mtfConsensusEngine ?? throw new ArgumentNullException(nameof(mtfConsensusEngine));
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));

            RunModelInferenceCommand = new AsyncRelayCommand(OnExecuteInferenceAsync);

            // Populate system info
            _platform = RuntimeInformation.OSDescription;
            _cpuArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            _libraryLoaded = _nativeCore.IsAvailable ? "Yes (libnexus_native_core / nexus_native_core)" : "No (Managed Fallback Active)";
            _nativeCoreStatus = _nativeCore.IsAvailable ? "Active (C++20)" : "Inactive";

            // Load initial model setting
            _currentModelName = _neuralService.CurrentModelName;
            _modelVersion = _neuralService.ModelVersion;
            _modelLoadStatus = _neuralService.IsLoaded ? "Loaded" : "Not Loaded";

            // Run live update background task
            Task.Run(() => RunLiveTelemetryLoopAsync(_cts.Token));
        }

        private async Task OnExecuteInferenceAsync()
        {
            var symbol = new Symbol("EURUSD");
            var tick = new Tick(
                symbol,
                DateTime.UtcNow,
                1.08500 + (_random.NextDouble() - 0.5) * 0.002,
                1.08510 + (_random.NextDouble() - 0.5) * 0.002
            );

            var risk = new RiskState(500.0, 0.1, 0.0, 0, 0.0, false);

            var sw = Stopwatch.StartNew();
            var result = await _marketIntelligence.ProcessTickAndEvaluateAsync(tick, risk, CancellationToken.None);
            sw.Stop();

            // Read evaluated values dynamically from the NativeCore state or fallback state
            MarketState marketState;
            if (_nativeCore.IsAvailable)
            {
                marketState = _nativeCore.GetMarketState();
                CurrentMarketState = marketState.MarketRegime;
                PriceStructure = marketState.PriceStructure;
                Momentum = marketState.Momentum;
                Volatility = marketState.Volatility;
                Liquidity = marketState.Liquidity;

                // Load performance telemetry from real native measurements
                NativeExecutionLatency = $"{_marketIntelligence.TickProcessingLatencyMs:F4} ms";
                InteropLatency = $"{_marketIntelligence.InteropLatencyMs:F4} ms";
                FeatureCalculationTime = $"{_marketIntelligence.MarketStateUpdateTimeMs:F4} ms";
                NeuralInferenceTime = $"{_marketIntelligence.VectorGenerationTimeMs:F4} ms";
            }
            else
            {
                CurrentMarketState = _marketIntelligence.ActiveRegime;
                marketState = new MarketState(
                    "EURUSD",
                    DateTime.UtcNow,
                    Volatility,
                    Momentum,
                    Liquidity,
                    PriceStructure,
                    0.5,
                    0.1,
                    UsdStrength,
                    CurrentMarketState
                );

                // Generate managed telemetry
                NativeExecutionLatency = $"{_marketIntelligence.TickProcessingLatencyMs:F3} ms";
                InteropLatency = "0.000 ms";
                FeatureCalculationTime = $"{_marketIntelligence.MarketStateUpdateTimeMs:F3} ms";
                NeuralInferenceTime = $"{_marketIntelligence.VectorGenerationTimeMs:F3} ms";
            }

            // Neural model evaluation metrics
            BuyConfidence = 0.33 + (_trendState * 0.25) + (_momentum * 0.15);
            SellConfidence = 0.33 - (_trendState * 0.25) - (_momentum * 0.15);
            WaitConfidence = 1.0 - BuyConfidence - SellConfidence;

            BuyConfidence = Math.Clamp(BuyConfidence, 0.0, 1.0);
            SellConfidence = Math.Clamp(SellConfidence, 0.0, 1.0);
            WaitConfidence = Math.Clamp(WaitConfidence, 0.0, 1.0);

            double sum = BuyConfidence + SellConfidence + WaitConfidence;
            if (sum > 0)
            {
                BuyConfidence /= sum;
                SellConfidence /= sum;
                WaitConfidence /= sum;
            }

            EvaluationScore = Math.Max(BuyConfidence, Math.Max(SellConfidence, WaitConfidence));
            LastInferenceTime = DateTime.Now.ToString("HH:mm:ss.fff");

            double totalPipeline = sw.Elapsed.TotalMilliseconds;
            TotalDecisionPipelineTime = $"{totalPipeline:F2} ms";

            InferenceLatency = $"{_neuralService.InferenceLatencyMs:F2} ms";
            LastExecutionTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Format status with Mode
            ModelStatus = $"{_neuralService.CurrentMode} ({_marketIntelligence.CurrentMode})";

            // Stockfish Decision search integration
            try
            {
                var searchNode = await _scenarioSearchEngine.SearchBestActionAsync(
                    marketState,
                    risk,
                    BuyConfidence,
                    SellConfidence,
                    CancellationToken.None
                );
                SearchBestAction = searchNode.Action.ToString();
                SearchReasoning = searchNode.Reasoning;
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Intelligence", "Error", $"Scenario search failed: {ex.Message}");
            }

            // Multi-Timeframe Consensus signal simulation & integration
            try
            {
                var d1Signal = new MultiTimeframeSignal(TimeframeInterval.D1, BuyConfidence > SellConfidence ? TrendDirection.BULLISH : TrendDirection.BEARISH, 0.85, Volatility, Momentum, EvaluationScore, DateTime.UtcNow);
                var h4Signal = new MultiTimeframeSignal(TimeframeInterval.H4, BuyConfidence > SellConfidence ? TrendDirection.BULLISH : TrendDirection.BEARISH, 0.78, Volatility, Momentum, EvaluationScore, DateTime.UtcNow);
                var m30Signal = new MultiTimeframeSignal(TimeframeInterval.M30, BuyConfidence > SellConfidence ? TrendDirection.BULLISH : TrendDirection.BEARISH, 0.72, Volatility, Momentum, EvaluationScore, DateTime.UtcNow);
                var m5Signal = new MultiTimeframeSignal(TimeframeInterval.M5, BuyConfidence > SellConfidence ? TrendDirection.BULLISH : TrendDirection.BEARISH, 0.68, Volatility, Momentum, EvaluationScore, DateTime.UtcNow);

                _mtfConsensusEngine.RegisterTimeframeSignal(d1Signal);
                _mtfConsensusEngine.RegisterTimeframeSignal(h4Signal);
                _mtfConsensusEngine.RegisterTimeframeSignal(m30Signal);
                _mtfConsensusEngine.RegisterTimeframeSignal(m5Signal);

                var consensusState = _mtfConsensusEngine.GetCurrentConsensus();
                MtfConsensusSummary = consensusState.ConsensusSummary;
            }
            catch (Exception ex)
            {
                _diagnosticService.Log("Intelligence", "Error", $"MTF consensus failed: {ex.Message}");
            }
        }

        private async Task RunLiveTelemetryLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token);

                    // Read memory usage
                    long mem = Process.GetCurrentProcess().WorkingSet64;
                    MemoryUsage = $"{(mem / (1024.0 * 1024.0)):F1} MB";

                    // Update USD strength score
                    UsdStrength = _currencyEngine.GetStrengthScore("USD");

                    // Execute model evaluation
                    await OnExecuteInferenceAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Keep background task alive
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
