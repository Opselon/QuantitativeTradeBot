// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   PRESENTATION LAYER (ViewModels)
// FILE:    TrainSkillsViewModel.cs
// ============================================================================

using Microsoft.Win32;
using Nexus.Application.Ports;
using Nexus.Desktop.Services;
using Nexus.Infrastructure.TorchSharp.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using TorchSharp;
using static global::TorchSharp.torch;
using Tensor = global::TorchSharp.torch.Tensor;

namespace Nexus.Desktop.ViewModels.Workspaces
{
    public sealed class ConsoleLogEntry
    {
        public string Text { get; set; } = string.Empty;

        public Brush ColorBrush { get; set; } = Brushes.LightGray;
    }

    public sealed class TrainSkillsViewModel :
        ViewModelBase,
        IDisposable
    {
        private const int SequenceLength = 30;
        private const int FeatureCount = 33;
        private const int OutputCount = 7;
        private const int TrainingEpochs = 5;
        private const int MaximumConsoleEntries = 1_000;

        private const double ExponentialDecayLambda = 0.5d;

        private const double BytesPerGigabyte =
            1024d * 1024d * 1024d;

        private const string DisplayAdapterRegistryPath =
            @"SYSTEM\CurrentControlSet\Control\Class\" +
            @"{4d36e968-e325-11ce-bfc1-08002be10318}";

        private readonly IPythonExecutionService _pythonService;
        private readonly IDiagnosticService _diagnosticService;

        private CancellationTokenSource? _cancellationTokenSource;

        private string _symbol = "XAUUSD";
        private int _candleCount = 10_000;
        private bool _isProcessing;
        private bool _isInstalling;
        private bool _disposed;

        private string _statusText =
            "Idle - Ready to prepare environment or build trading skills.";

        public TrainSkillsViewModel(
            IPythonExecutionService pythonService,
            IDiagnosticService diagnosticService)
        {
            _pythonService = pythonService
                ?? throw new ArgumentNullException(nameof(pythonService));

            _diagnosticService = diagnosticService
                ?? throw new ArgumentNullException(nameof(diagnosticService));

            InstallDependenciesCommand =
                new AsyncRelayCommand(OnInstallDependenciesAsync);

            TrainPriceActionCommand =
                new AsyncRelayCommand(OnTrainPriceActionAsync);

            TrainIctCommand =
                new AsyncRelayCommand(OnTrainIctAsync);

            StopTrainingCommand =
                new RelayCommand(OnStopTraining);

            ClearConsoleCommand =
                new RelayCommand(OnClearConsole);

            CopyConsoleCommand =
                new RelayCommand(OnCopyConsole);

            _pythonService.OutputReceived += OnPythonOutputReceived;
            _pythonService.ExecutionCompleted +=
                OnPythonExecutionCompleted;
        }

        public ObservableCollection<ConsoleLogEntry> ConsoleOutput { get; } =
            new ObservableCollection<ConsoleLogEntry>();

        public string Symbol
        {
            get => _symbol;

            set
            {
                string normalizedSymbol =
                    string.IsNullOrWhiteSpace(value)
                        ? "XAUUSD"
                        : value.Trim().ToUpperInvariant();

                SetProperty(ref _symbol, normalizedSymbol);
            }
        }

        public int CandleCount
        {
            get => _candleCount;

            set
            {
                int normalizedCount = Math.Max(100, value);
                SetProperty(ref _candleCount, normalizedCount);
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            private set => SetProperty(ref _isProcessing, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            private set => SetProperty(ref _isInstalling, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public ICommand InstallDependenciesCommand { get; }

        public ICommand TrainPriceActionCommand { get; }

        public ICommand TrainIctCommand { get; }

        public ICommand StopTrainingCommand { get; }

        public ICommand ClearConsoleCommand { get; }

        public ICommand CopyConsoleCommand { get; }

        public readonly struct HardwareProfile
        {
            public HardwareProfile(
                string cpuName,
                string gpuName,
                double totalRamGb,
                double freeRamGb,
                bool cudaAvailable,
                int recommendedBatchSize)
            {
                CpuName = cpuName;
                GpuName = gpuName;
                TotalRamGb = totalRamGb;
                FreeRamGb = freeRamGb;
                CudaAvailable = cudaAvailable;
                RecommendedBatchSize = recommendedBatchSize;
            }

            public string CpuName { get; }

            public string GpuName { get; }

            public double TotalRamGb { get; }

            public double FreeRamGb { get; }

            public bool CudaAvailable { get; }

            public int RecommendedBatchSize { get; }
        }

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Auto)]
        private sealed class MemoryStatusEx
        {
            public MemoryStatusEx()
            {
                Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            }

            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport(
            "kernel32.dll",
            CharSet = CharSet.Auto,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(
            [In, Out] MemoryStatusEx buffer);

        private async Task OnInstallDependenciesAsync()
        {
            ThrowIfDisposed();

            if (IsProcessing || IsInstalling)
            {
                return;
            }

            IsInstalling = true;
            IsProcessing = true;

            StatusText =
                "Setting up Python environment. Installing dependencies...";

            _diagnosticService.Log(
                "TrainSkills",
                "INFO",
                "Starting automated Python dependency installation.");

            StreamLogToUi(
                "[SYSTEM] Starting pip environment setup...");

            CancellationToken cancellationToken =
                CreateOperationCancellationToken();

            try
            {
                bool installationSucceeded =
                    await _pythonService.InstallDependenciesAsync(
                        cancellationToken);

                StatusText = installationSucceeded
                    ? "Environment is ready."
                    : "Environment setup failed. Check console output.";
            }
            catch (OperationCanceledException)
            {
                StatusText = "Environment setup cancelled.";

                StreamLogToUi(
                    "[SYSTEM] Environment setup cancelled.");
            }
            catch (Exception exception)
            {
                StatusText =
                    $"Setup Error: {exception.Message}";

                StreamLogToUi(
                    $"[ERROR] Setup exception: {exception.Message}");

                _diagnosticService.Log(
                    "TrainSkills",
                    "ERROR",
                    exception.ToString());
            }
            finally
            {
                IsInstalling = false;
                IsProcessing = false;

                DisposeCancellationTokenSource();
            }
        }

        private Task OnTrainPriceActionAsync()
        {
            return RunTrainingPipelineAsync(isIct: false);
        }

        private Task OnTrainIctAsync()
        {
            return RunTrainingPipelineAsync(isIct: true);
        }

        private async Task RunTrainingPipelineAsync(bool isIct)
        {
            ThrowIfDisposed();

            if (IsProcessing)
            {
                return;
            }

            IsProcessing = true;

            string datasetName = isIct
                ? "SMC / ICT Liquidity"
                : "Price Action Master";

            StatusText =
                $"Extracting {datasetName} features for {Symbol}...";

            _diagnosticService.Log(
                "TrainSkills",
                "INFO",
                $"{datasetName} feature engineering started.");

            StreamLogToUi(
                isIct
                    ? "[SYSTEM] Starting SMC / ICT Liquidity Master build..."
                    : "[SYSTEM] Starting Price Action Master build...");

            CancellationToken cancellationToken =
                CreateOperationCancellationToken();

            try
            {
                bool scriptSucceeded;

                if (isIct)
                {
                    scriptSucceeded =
                        await _pythonService.RunIctDatasetBuilderAsync(
                            Symbol,
                            CandleCount,
                            cancellationToken);
                }
                else
                {
                    scriptSucceeded =
                        await _pythonService.RunDatasetBuilderAsync(
                            Symbol,
                            CandleCount,
                            cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!scriptSucceeded)
                {
                    StatusText =
                        $"{datasetName} generation failed. " +
                        "Training cancelled.";

                    StreamLogToUi(
                        "[SYSTEM] Dataset generation failed. " +
                        "The C# training stage was not started.");

                    return;
                }

                StatusText =
                    "Features generated. Starting C# deep-learning loop...";

                await Task.Run(
                    () => TrainCsharpModel(
                        isIct,
                        cancellationToken),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Training cancelled.";

                StreamLogToUi(
                    "[C# DEEP LEARNING] [SYSTEM] Training cancelled.");
            }
            catch (Exception exception)
            {
                StatusText =
                    $"Error: {exception.Message}";

                StreamLogToUi(
                    "[C# DEEP LEARNING] [CRITICAL ERROR] " +
                    exception.Message);

                _diagnosticService.Log(
                    "TrainSkills",
                    "ERROR",
                    exception.ToString());
            }
            finally
            {
                IsProcessing = false;
                DisposeCancellationTokenSource();
            }
        }

        private void TrainCsharpModel(
            bool isIct,
            CancellationToken cancellationToken)
        {
            string appDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            string currentSymbol = Symbol;

            string fileName = isIct
                ? $"{currentSymbol}_M15_ICT.csv"
                : $"{currentSymbol}_M15.csv";

            string csvPath = Path.Combine(
                appDirectory,
                "NexusAI",
                "Data",
                "Raw",
                currentSymbol,
                "M15",
                fileName);

            StreamLogToUi(
                "[C# DEEP LEARNING] Ingesting local CSV file: " +
                csvPath);

            if (!File.Exists(csvPath))
            {
                StreamLogToUi(
                    "[C# DEEP LEARNING] [ERROR] Target CSV file " +
                    "was not found. Training aborted.");

                SetStatusOnUiThread(
                    "Training failed because the dataset was not found.");

                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<double[]> records =
                    ParseCsvFeatures(csvPath, cancellationToken);

                int samplesCount =
                    records.Count - SequenceLength;

                if (samplesCount <= 0)
                {
                    StreamLogToUi(
                        "[C# DEEP LEARNING] [ERROR] Insufficient rows " +
                        $"({records.Count}) for sequence length " +
                        $"{SequenceLength}.");

                    SetStatusOnUiThread(
                        "Training failed because the dataset is too small.");

                    return;
                }

                HardwareProfile hardware =
                    DetectHardwareProfile();

                LogHardwareProfile(hardware);

                Device trainingDevice = hardware.CudaAvailable
                    ? CUDA
                    : CPU;

                StreamLogToUi(
                    "[C# DEEP LEARNING] Execution device: " +
                    trainingDevice.type);

                int batchSize = Math.Min(
                    hardware.RecommendedBatchSize,
                    samplesCount);

                int totalBatches =
                    (samplesCount + batchSize - 1) / batchSize;

                StreamLogToUi(
                    $"[C# DEEP LEARNING] Parsed {records.Count:N0} " +
                    "historical intervals. Building tensors...");

                TrainingArrays arrays = BuildTrainingArrays(
                    records,
                    samplesCount,
                    cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                using (DisposeScope trainingScope = NewDisposeScope())
                {
                    Tensor inputTensor = tensor(
                        arrays.Features,
                        new long[]
                        {
                            samplesCount,
                            SequenceLength,
                            FeatureCount
                        },
                        dtype: ScalarType.Float32,
                        device: trainingDevice);

                    Tensor targetTensor = tensor(
                        arrays.Targets,
                        new long[]
                        {
                            samplesCount,
                            OutputCount
                        },
                        dtype: ScalarType.Float32,
                        device: trainingDevice);

                    /*
                     * The offset tensor must remain [N, 1].
                     *
                     * A one-dimensional [N] tensor cannot be safely
                     * multiplied by a [N, 7] loss tensor because PyTorch
                     * aligns broadcasting dimensions from the right.
                     */
                    Tensor offsetsTensor = tensor(
                        arrays.Offsets,
                        new long[]
                        {
                            samplesCount,
                            1
                        },
                        dtype: ScalarType.Float32,
                        device: trainingDevice);

                    using (var model =
                        new TemporalFusionTransformer(
                            FeatureCount,
                            1,
                            64,
                            0.1))
                    {
                        model.to(trainingDevice);
                        model.train();

                        using (var optimizer = optim.Adam(
                            model.parameters(),
                            lr: 0.001))
                        {
                            StreamLogToUi(
                                "[C# DEEP LEARNING] Launching adaptive " +
                                "mini-batch backpropagation...");

                            for (int epoch = 1;
                                 epoch <= TrainingEpochs;
                                 epoch++)
                            {
                                cancellationToken
                                    .ThrowIfCancellationRequested();

                                TrainEpoch(
                                    model,
                                    optimizer,
                                    inputTensor,
                                    targetTensor,
                                    offsetsTensor,
                                    epoch,
                                    batchSize,
                                    totalBatches,
                                    samplesCount,
                                    cancellationToken);
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        string checkpointDirectory = Path.Combine(
                            appDirectory,
                            "NexusAI",
                            "Checkpoints");

                        Directory.CreateDirectory(
                            checkpointDirectory);

                        string checkpointPath = Path.Combine(
                            checkpointDirectory,
                            "v1.0.0.dat");

                        model.save(checkpointPath);

                        StreamLogToUi(
                            "[C# DEEP LEARNING] [OK] Model trained " +
                            "successfully. Checkpoint: " +
                            checkpointPath);
                    }
                }

                SetStatusOnUiThread(
                    "Model trained successfully and checkpoint saved.");
            }
            catch (OperationCanceledException)
            {
                StreamLogToUi(
                    "[C# DEEP LEARNING] [SYSTEM] Training cancelled.");

                throw;
            }
            catch (Exception exception)
            {
                StreamLogToUi(
                    "[C# DEEP LEARNING] [CRITICAL ERROR] " +
                    $"Training loop failed: {exception.Message}");

                _diagnosticService.Log(
                    "TrainSkills",
                    "ERROR",
                    exception.ToString());

                throw;
            }

            /*
             * No cuda.empty_cache() call is made here.
             *
             * The installed TorchSharp version does not expose that API.
             * Tensor cleanup is performed deterministically by DisposeScope.
             */
        }

        private void TrainEpoch(
            TemporalFusionTransformer model,
            global::TorchSharp.torch.optim.Optimizer optimizer,
            Tensor inputTensor,
            Tensor targetTensor,
            Tensor offsetsTensor,
            int epoch,
            int batchSize,
            int totalBatches,
            int samplesCount,
            CancellationToken cancellationToken)
        {
            var stopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            double epochLossSum = 0d;
            int completedBatches = 0;

            for (int batchIndex = 0;
                 batchIndex < totalBatches;
                 batchIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int startIndex =
                    batchIndex * batchSize;

                int currentBatchSize = Math.Min(
                    batchSize,
                    samplesCount - startIndex);

                using (DisposeScope batchScope = NewDisposeScope())
                {
                    optimizer.zero_grad();

                    Tensor batchInput = inputTensor.narrow(
                        0,
                        startIndex,
                        currentBatchSize);

                    Tensor batchTarget = targetTensor.narrow(
                        0,
                        startIndex,
                        currentBatchSize);

                    Tensor batchOffsets = offsetsTensor.narrow(
                        0,
                        startIndex,
                        currentBatchSize);

                    ValidateBatchShapes(
                        batchInput,
                        batchTarget,
                        batchOffsets,
                        currentBatchSize);

                    Tensor prediction =
                        model.forward(batchInput);

                    ValidatePredictionShape(
                        prediction,
                        currentBatchSize);

                    /*
                     * CalculateExponentialDecayLoss already returns a
                     * scalar tensor. Do not use number_of_elements and
                     * do not call mean() a second time.
                     */
                    Tensor loss =
                        model.CalculateExponentialDecayLoss(
                            prediction,
                            batchTarget,
                            batchOffsets,
                            ExponentialDecayLambda);

                    loss.backward();
                    optimizer.step();

                    float lossValue =
                        loss.item<float>();

                    if (float.IsNaN(lossValue) ||
                        float.IsInfinity(lossValue))
                    {
                        throw new InvalidOperationException(
                            "Training produced a non-finite loss value.");
                    }

                    epochLossSum += lossValue;
                    completedBatches++;

                    LogBatchProgress(
                        epoch,
                        batchIndex,
                        totalBatches,
                        currentBatchSize,
                        batchSize,
                        lossValue,
                        stopwatch);
                }
            }

            stopwatch.Stop();

            double averageLoss =
                completedBatches == 0
                    ? 0d
                    : epochLossSum / completedBatches;

            StreamLogToUi(
                "[C# DEEP LEARNING] [EPOCH COMPLETED] " +
                $"Epoch {epoch:00}/{TrainingEpochs:00} completed in " +
                $"{stopwatch.Elapsed.TotalSeconds:F2} sec. " +
                $"Average Loss: {averageLoss:F6}");
        }

        private static void ValidateBatchShapes(
            Tensor input,
            Tensor target,
            Tensor offsets,
            int expectedBatchSize)
        {
            long[] inputShape = input.shape;
            long[] targetShape = target.shape;
            long[] offsetShape = offsets.shape;

            if (inputShape.Length != 3 ||
                inputShape[0] != expectedBatchSize ||
                inputShape[1] != SequenceLength ||
                inputShape[2] != FeatureCount)
            {
                throw new InvalidOperationException(
                    "Invalid input tensor shape: [" +
                    string.Join(", ", inputShape) +
                    $"]. Expected [{expectedBatchSize}, " +
                    $"{SequenceLength}, {FeatureCount}].");
            }

            if (targetShape.Length != 2 ||
                targetShape[0] != expectedBatchSize ||
                targetShape[1] != OutputCount)
            {
                throw new InvalidOperationException(
                    "Invalid target tensor shape: [" +
                    string.Join(", ", targetShape) +
                    $"]. Expected [{expectedBatchSize}, " +
                    $"{OutputCount}].");
            }

            if (offsetShape.Length != 2 ||
                offsetShape[0] != expectedBatchSize ||
                offsetShape[1] != 1)
            {
                throw new InvalidOperationException(
                    "Invalid offset tensor shape: [" +
                    string.Join(", ", offsetShape) +
                    $"]. Expected [{expectedBatchSize}, 1].");
            }
        }

        private static void ValidatePredictionShape(
            Tensor prediction,
            int expectedBatchSize)
        {
            long[] predictionShape = prediction.shape;

            if (predictionShape.Length != 2 ||
                predictionShape[0] != expectedBatchSize ||
                predictionShape[1] != OutputCount)
            {
                throw new InvalidOperationException(
                    "TemporalFusionTransformer returned unexpected " +
                    "shape [" +
                    string.Join(", ", predictionShape) +
                    $"]. Expected [{expectedBatchSize}, " +
                    $"{OutputCount}].");
            }
        }

        private void LogBatchProgress(
            int epoch,
            int batchIndex,
            int totalBatches,
            int currentBatchSize,
            int configuredBatchSize,
            float lossValue,
            System.Diagnostics.Stopwatch stopwatch)
        {
            if (batchIndex % 5 != 0 &&
                batchIndex != totalBatches - 1)
            {
                return;
            }

            int completedBatches =
                batchIndex + 1;

            int percent = Math.Clamp(
                completedBatches * 100 / totalBatches,
                0,
                100);

            int processedSamples =
                (batchIndex * configuredBatchSize) +
                currentBatchSize;

            double elapsedSeconds = Math.Max(
                stopwatch.Elapsed.TotalSeconds,
                0.001d);

            double samplesPerSecond =
                processedSamples / elapsedSeconds;

            string progressBar =
                CreateProgressBar(percent);

            StreamLogToUi(
                "[C# DEEP LEARNING] " +
                $"Epoch {epoch:00}/{TrainingEpochs:00} | " +
                $"[{progressBar}] {percent,3}% | " +
                $"Batch {completedBatches}/{totalBatches} | " +
                $"Loss: {lossValue:F6} | " +
                $"Speed: {samplesPerSecond:F0} samples/sec");
        }

        private static string CreateProgressBar(int percent)
        {
            int completedSections = Math.Clamp(
                percent / 10,
                0,
                10);

            return
                new string('=', completedSections) +
                new string('.', 10 - completedSections);
        }

        private TrainingArrays BuildTrainingArrays(
            IReadOnlyList<double[]> records,
            int samplesCount,
            CancellationToken cancellationToken)
        {
            var features = new float[
                samplesCount *
                SequenceLength *
                FeatureCount];

            var targets = new float[
                samplesCount *
                OutputCount];

            var offsets = new float[samplesCount];

            int featureWindowSize =
                SequenceLength * FeatureCount;

            for (int sampleIndex = 0;
                 sampleIndex < samplesCount;
                 sampleIndex++)
            {
                if ((sampleIndex & 255) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                for (int sequenceIndex = 0;
                     sequenceIndex < SequenceLength;
                     sequenceIndex++)
                {
                    double[] sourceRecord =
                        records[sampleIndex + sequenceIndex];

                    int destinationOffset =
                        (sampleIndex * featureWindowSize) +
                        (sequenceIndex * FeatureCount);

                    for (int featureIndex = 0;
                         featureIndex < FeatureCount;
                         featureIndex++)
                    {
                        features[
                            destinationOffset + featureIndex] =
                            ToFiniteFloat(
                                sourceRecord[featureIndex]);
                    }
                }

                double[] futureRecord =
                    records[sampleIndex + SequenceLength];

                double closeReturn =
                    futureRecord[32];

                int targetOffset =
                    sampleIndex * OutputCount;

                targets[targetOffset] =
                    closeReturn > 0.0005d
                        ? 1f
                        : 0f;

                targets[targetOffset + 1] =
                    closeReturn < -0.0005d
                        ? 1f
                        : 0f;

                targets[targetOffset + 2] =
                    Math.Abs(closeReturn) <= 0.0005d
                        ? 1f
                        : 0f;

                targets[targetOffset + 3] =
                    ToFiniteFloat(closeReturn);

                targets[targetOffset + 4] =
                    ToFiniteFloat(futureRecord[9]);

                targets[targetOffset + 5] =
                    ToFiniteFloat(futureRecord[13]);

                targets[targetOffset + 6] = 0.5f;

                /*
                 * Older observations receive a larger time offset and
                 * therefore a smaller exponential weight.
                 */
                offsets[sampleIndex] =
                    (float)(samplesCount - sampleIndex) /
                    samplesCount;
            }

            return new TrainingArrays(
                features,
                targets,
                offsets);
        }

        private static float ToFiniteFloat(double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return 0f;
            }

            if (value > float.MaxValue)
            {
                return float.MaxValue;
            }

            if (value < float.MinValue)
            {
                return float.MinValue;
            }

            return (float)value;
        }

        private List<double[]> ParseCsvFeatures(
            string csvPath,
            CancellationToken cancellationToken)
        {
            var records = new List<double[]>();

            using (var reader = new StreamReader(csvPath))
            {
                string? header = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(header))
                {
                    return records;
                }

                int lineNumber = 1;

                while (reader.ReadLine() is string line)
                {
                    lineNumber++;

                    if ((lineNumber & 511) == 0)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] tokens =
                        line.Split(',');

                    if (tokens.Length < FeatureCount + 1)
                    {
                        _diagnosticService.Log(
                            "TrainSkills",
                            "WARN",
                            $"Ignored short CSV row {lineNumber}.");

                        continue;
                    }

                    var rowFeatures =
                        new double[FeatureCount];

                    bool validRow = true;

                    for (int featureIndex = 0;
                         featureIndex < FeatureCount;
                         featureIndex++)
                    {
                        string token =
                            tokens[featureIndex + 1];

                        bool parsed = double.TryParse(
                            token,
                            NumberStyles.Float |
                            NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture,
                            out double value);

                        if (!parsed)
                        {
                            validRow = false;
                            break;
                        }

                        rowFeatures[featureIndex] =
                            double.IsNaN(value) ||
                            double.IsInfinity(value)
                                ? 0d
                                : value;
                    }

                    if (validRow)
                    {
                        records.Add(rowFeatures);
                    }
                    else
                    {
                        _diagnosticService.Log(
                            "TrainSkills",
                            "WARN",
                            $"Ignored invalid CSV row {lineNumber}.");
                    }
                }
            }

            return records;
        }

        private HardwareProfile DetectHardwareProfile()
        {
            string cpuName =
                DetectCpuName();

            string gpuName =
                DetectPreferredGpuName();

            (double totalRamGb, double freeRamGb) =
                DetectMemory();

            bool cudaAvailable =
                IsCudaAvailableSafely();

            int recommendedBatchSize =
                DetermineBatchSize(
                    freeRamGb,
                    gpuName,
                    cudaAvailable);

            return new HardwareProfile(
                cpuName,
                gpuName,
                totalRamGb,
                freeRamGb,
                cudaAvailable,
                recommendedBatchSize);
        }

        private static string DetectCpuName()
        {
            const string fallback =
                "Unknown CPU Processor";

            try
            {
                using (RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
                        @"HARDWARE\DESCRIPTION\System\" +
                        @"CentralProcessor\0"))
                {
                    return key?
                        .GetValue("ProcessorNameString")?
                        .ToString()?
                        .Trim()
                        ?? fallback;
                }
            }
            catch
            {
                return fallback;
            }
        }

        private static string DetectPreferredGpuName()
        {
            var adapters = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            try
            {
                using (RegistryKey? displayClass =
                    Registry.LocalMachine.OpenSubKey(
                        DisplayAdapterRegistryPath))
                {
                    if (displayClass != null)
                    {
                        foreach (string subKeyName in
                            displayClass.GetSubKeyNames())
                        {
                            using (RegistryKey? adapterKey =
                                displayClass.OpenSubKey(subKeyName))
                            {
                                string? description = adapterKey?
                                    .GetValue("DriverDesc")?
                                    .ToString()?
                                    .Trim();

                                if (!string.IsNullOrWhiteSpace(
                                    description))
                                {
                                    adapters.Add(description);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Hardware reporting must never stop training.
            }

            if (adapters.Count == 0)
            {
                return "Unknown Graphics Adapter";
            }

            return adapters
                .OrderByDescending(GetGpuPriority)
                .ThenBy(
                    adapter => adapter,
                    StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private static int GetGpuPriority(string gpuName)
        {
            string normalizedName =
                gpuName.ToUpperInvariant();

            if (normalizedName.Contains("NVIDIA") &&
                normalizedName.Contains("RTX"))
            {
                return 1_000;
            }

            if (normalizedName.Contains("NVIDIA"))
            {
                return 900;
            }

            if (normalizedName.Contains("RADEON") ||
                normalizedName.Contains("AMD"))
            {
                return 800;
            }

            if (normalizedName.Contains("ARC"))
            {
                return 700;
            }

            if (normalizedName.Contains("INTEL"))
            {
                return 100;
            }

            return 10;
        }

        private static (
            double TotalRamGb,
            double FreeRamGb) DetectMemory()
        {
            const double fallbackTotalRamGb = 16d;
            const double fallbackFreeRamGb = 4d;

            try
            {
                var memoryStatus =
                    new MemoryStatusEx();

                if (!GlobalMemoryStatusEx(memoryStatus))
                {
                    return (
                        fallbackTotalRamGb,
                        fallbackFreeRamGb);
                }

                return (
                    memoryStatus.TotalPhysical / BytesPerGigabyte,
                    memoryStatus.AvailablePhysical / BytesPerGigabyte);
            }
            catch
            {
                return (
                    fallbackTotalRamGb,
                    fallbackFreeRamGb);
            }
        }

        private static bool IsCudaAvailableSafely()
        {
            try
            {
                return cuda.is_available();
            }
            catch
            {
                return false;
            }
        }

        private static int DetermineBatchSize(
            double freeRamGb,
            string gpuName,
            bool cudaAvailable)
        {
            bool isRtx = gpuName.Contains(
                "RTX",
                StringComparison.OrdinalIgnoreCase);

            if (cudaAvailable && isRtx)
            {
                if (freeRamGb >= 12d)
                {
                    return 256;
                }

                if (freeRamGb >= 6d)
                {
                    return 128;
                }

                if (freeRamGb >= 3d)
                {
                    return 64;
                }

                return 32;
            }

            if (freeRamGb >= 12d)
            {
                return 128;
            }

            if (freeRamGb >= 6d)
            {
                return 64;
            }

            if (freeRamGb >= 3d)
            {
                return 32;
            }

            return 16;
        }

        private void LogHardwareProfile(
            HardwareProfile hardware)
        {
            StreamLogToUi(
                "[SYSTEM] Active Hardware Profile Analyzed:");

            StreamLogToUi(
                $"[SYSTEM] -> Processor CPU: {hardware.CpuName}");

            StreamLogToUi(
                $"[SYSTEM] -> Preferred GPU: {hardware.GpuName}");

            StreamLogToUi(
                "[SYSTEM] -> TorchSharp CUDA Available: " +
                hardware.CudaAvailable);

            StreamLogToUi(
                "[SYSTEM] -> System Memory: " +
                $"{hardware.FreeRamGb:F2} GB Available / " +
                $"{hardware.TotalRamGb:F2} GB Total");

            StreamLogToUi(
                "[SYSTEM] [STRATEGY CHOSEN] Adaptive batch size: " +
                $"{hardware.RecommendedBatchSize} samples.");

            bool rtxDetected = hardware.GpuName.Contains(
                "RTX",
                StringComparison.OrdinalIgnoreCase);

            if (rtxDetected &&
                !hardware.CudaAvailable)
            {
                StreamLogToUi(
                    "[SYSTEM] [WARNING] RTX GPU was detected, but " +
                    "TorchSharp CUDA is unavailable. Training will " +
                    "fall back to CPU. Install a CUDA-enabled " +
                    "TorchSharp/LibTorch package.");
            }
        }

        private void SetStatusOnUiThread(string status)
        {
            /*
             * Do not declare an Application variable here.
             *
             * Nexus.Application is a project namespace and collides with
             * System.Windows.Application. Accessing Dispatcher directly
             * also avoids nullable Application comparison errors.
             */
            var dispatcher =
                global::System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null ||
                dispatcher.CheckAccess())
            {
                StatusText = status;
                return;
            }

            dispatcher.Invoke(
                new Action(
                    () => StatusText = status));
        }

        private void StreamLogToUi(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            SolidColorBrush brush =
                CreateLogBrush(line);

            var dispatcher =
                global::System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null ||
                dispatcher.CheckAccess())
            {
                AddConsoleEntry(line, brush);
                return;
            }

            dispatcher.BeginInvoke(
                new Action(
                    () => AddConsoleEntry(line, brush)));
        }

        private static SolidColorBrush CreateLogBrush(
            string line)
        {
            Color color;

            if (ContainsAny(
                line,
                "[ERROR]",
                "[CRITICAL]",
                "[STDERR]"))
            {
                color = Color.FromRgb(
                    0xFF,
                    0x5F,
                    0x56);
            }
            else if (line.Contains(
                "[WARNING]",
                StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromRgb(
                    0xFF,
                    0x9F,
                    0x0A);
            }
            else if (line.Contains(
                "[OK]",
                StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromRgb(
                    0x27,
                    0xC9,
                    0x3F);
            }
            else if (line.Contains(
                "[C# DEEP LEARNING]",
                StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromRgb(
                    0xFF,
                    0xBD,
                    0x2E);
            }
            else if (line.Contains(
                "[SYSTEM]",
                StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromRgb(
                    0x00,
                    0xD2,
                    0xFF);
            }
            else if (line.Contains(
                "[INFO]",
                StringComparison.OrdinalIgnoreCase))
            {
                color = Color.FromRgb(
                    0xA0,
                    0xAE,
                    0xC0);
            }
            else
            {
                color = Color.FromRgb(
                    0xD3,
                    0xD3,
                    0xD3);
            }

            var brush =
                new SolidColorBrush(color);

            brush.Freeze();

            return brush;
        }

        private static bool ContainsAny(
            string source,
            params string[] values)
        {
            foreach (string value in values)
            {
                if (source.Contains(
                    value,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddConsoleEntry(
            string line,
            Brush brush)
        {
            if (_disposed)
            {
                return;
            }

            ConsoleOutput.Add(
                new ConsoleLogEntry
                {
                    Text = line,
                    ColorBrush = brush
                });

            while (ConsoleOutput.Count >
                MaximumConsoleEntries)
            {
                ConsoleOutput.RemoveAt(0);
            }
        }

        private void OnCopyConsole()
        {
            if (_disposed)
            {
                return;
            }

            string fullText = string.Join(
                Environment.NewLine,
                ConsoleOutput.Select(
                    entry => entry.Text));

            if (string.IsNullOrWhiteSpace(fullText))
            {
                return;
            }

            try
            {
                global::System.Windows.Clipboard.SetText(fullText);

                _diagnosticService.Log(
                    "TrainSkills",
                    "INFO",
                    "Console logs copied to Clipboard.");
            }
            catch (Exception exception)
            {
                _diagnosticService.Log(
                    "TrainSkills",
                    "WARN",
                    "Could not copy console logs: " +
                    exception.Message);
            }
        }

        private void OnStopTraining()
        {
            if (_disposed)
            {
                return;
            }

            StatusText =
                "Aborting active task...";

            _cancellationTokenSource?.Cancel();
            _pythonService.StopExecution();
        }

        private void OnClearConsole()
        {
            if (_disposed)
            {
                return;
            }

            var dispatcher =
                global::System.Windows.Application.Current?.Dispatcher;

            if (dispatcher == null ||
                dispatcher.CheckAccess())
            {
                ConsoleOutput.Clear();
                return;
            }

            dispatcher.BeginInvoke(
                new Action(ConsoleOutput.Clear));
        }

        private void OnPythonOutputReceived(string line)
        {
            if (_disposed)
            {
                return;
            }

            StreamLogToUi(line);
        }

        private void OnPythonExecutionCompleted(bool success)
        {
            if (_disposed)
            {
                return;
            }

            string line =
                $"[{DateTime.Now:HH:mm:ss}] [SYSTEM] " +
                "Pipeline process closed. " +
                $"Status Succeeded={success} " +
                (success ? "[OK]" : "[ERROR]");

            StreamLogToUi(line);
        }

        private CancellationToken CreateOperationCancellationToken()
        {
            DisposeCancellationTokenSource();

            _cancellationTokenSource =
                new CancellationTokenSource();

            return _cancellationTokenSource.Token;
        }

        private void DisposeCancellationTokenSource()
        {
            CancellationTokenSource? source =
                _cancellationTokenSource;

            _cancellationTokenSource = null;

            source?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(TrainSkillsViewModel));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _pythonService.OutputReceived -=
                OnPythonOutputReceived;

            _pythonService.ExecutionCompleted -=
                OnPythonExecutionCompleted;

            CancellationTokenSource? source =
                _cancellationTokenSource;

            _cancellationTokenSource = null;

            try
            {
                source?.Cancel();
                _pythonService.StopExecution();
            }
            catch
            {
                // Disposal must continue even if cancellation fails.
            }
            finally
            {
                source?.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        private sealed record TrainingArrays(
            float[] Features,
            float[] Targets,
            float[] Offsets);
    }
}
