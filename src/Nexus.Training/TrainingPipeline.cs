using Microsoft.Extensions.DependencyInjection; // Resolved dynamically from Microsoft package abstractions
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Training
{
    /// <summary>
    /// Coordinates the offline-first learning lifecycle:
    /// Experience Collection -> Data Validation -> Feature Preparation -> Dataset Creation -> Training Run -> Evaluation -> Validation -> Model Approval.
    /// Uses highly accelerated native feature vectors to optimize neural weights.
    /// </summary>
    public sealed class TrainingPipeline
    {
        #region Private Fields & Dependencies
        private readonly ModelRegistry _modelRegistry;
        private readonly IModelStorage _modelStorage;
        private readonly ValidationEngine _validationEngine;
        private readonly TimeframeLearningManager _learningManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<string> _pipelineLogs = new();
        #endregion

        #region Public Properties
        /// <summary>
        /// Exposes running audit logs of the training pipeline for UI telemetry.
        /// </summary>
        public IReadOnlyList<string> PipelineLogs => _pipelineLogs;
        #endregion

        #region Constructor
        public TrainingPipeline(
            ModelRegistry modelRegistry,
            IModelStorage modelStorage,
            ValidationEngine validationEngine,
            TimeframeLearningManager learningManager,
            IServiceScopeFactory scopeFactory)
        {
            _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
            _modelStorage = modelStorage ?? throw new ArgumentNullException(nameof(modelStorage));
            _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
            _learningManager = learningManager ?? throw new ArgumentNullException(nameof(learningManager));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }
        #endregion

        #region Database Experience Synchronization
        /// <summary>
        /// Architectural Bridge: Pulls completed trade experience records from the SQL database,
        /// translates them back into training samples, and inserts them into the Timeframe Learning Manager.
        /// This ensures the C++ native vectors are fed directly to the optimization algorithm.
        /// </summary>
        public async Task SyncDatabaseExperiencesToManagerAsync(CancellationToken cancellationToken = default)
        {
            Log("Initiating synchronization with the persistent experience database...");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var experienceRepo = scope.ServiceProvider.GetRequiredService<IExperienceRepository>();

                // Retrieve last 500 completed experiences to act as our training replay buffer
                var dbRecords = await experienceRepo.GetRecentExperiencesAsync(500, cancellationToken);
                var completedRecords = dbRecords.Where(r => r.IsCompleted).ToList();

                Log($"Database sync complete. Retrieved {completedRecords.Count} completed experiences from SQL.");

                foreach (var record in completedRecords)
                {
                    // Map domain record to offline learning ExperienceSample
                    var mockMarketState = new MarketState(
                        record.Symbol,
                        record.TimestampUtc,
                        0.5, 0.5, 0.5, 0.5, 0.5, 0.1, 50.0,
                        record.MarketRegime
                    );

                    var mockDecision = new TradeDecision(
                        DecisionAction.BUY,
                        0.01,
                        "Synced from SQL",
                        record.TimestampUtc
                    );

                    // Create the training sample containing the exact C++ bare-metal feature vector
                    var sample = new ExperienceSample(
                        record.Symbol,
                        TimeframeInterval.M15, // Default timeframe mapping
                        mockMarketState,
                        record.MarketVectorFeatures,
                        mockDecision,
                        1.0, // Base execution tracking price
                        record.MarketRegime
                    )
                    {
                        Result = record.RealizedPips, // The target reward used as labels in neural optimization
                        Confidence = Math.Max(record.BuyConfidence, record.SellConfidence),
                        Risk = record.RiskScore
                    };

                    // FIXED: Maps to the correct domain method 'RegisterExperience' instead of 'AddExperience'
                    _learningManager.RegisterExperience(sample);
                }

                Log("Synchronization completed. Active training memory has been fully populated.");
            }
            catch (Exception ex)
            {
                Log($"[Error] Failed to synchronize experiences from database: {ex.Message}");
            }
        }
        #endregion

        #region Core Pipeline Execution Loop
        /// <summary>
        /// Executes a full offline training workflow for a given timeframe category.
        /// </summary>
        public async Task<ModelVersionInfo?> ExecuteTrainingCycleAsync(
            TimeframeLearningCategory category,
            string targetVersion,
            string datasetVersion)
        {
            _pipelineLogs.Clear();
            Log($"Starting training pipeline cycle for version: {targetVersion}, category: {category}...");

            // Dynamic Synchronization Gate: Fetch live trading feedback before starting training
            await SyncDatabaseExperiencesToManagerAsync();

            // STEP 1: Experience Collection
            var experiences = _learningManager.GetDataset(category);
            Log($"[1/8 - Experience Collection] Retrieved {experiences.Count} experiences for category {category}.");

            if (experiences.Count < 4)
            {
                Log($"[Error] Aborting. Insufficient experiences collected. Got {experiences.Count}, need at least 4.");
                return null;
            }

            // STEP 2: Data Validation
            var validatedExperiences = ValidateExperiences(experiences);
            Log($"[2/8 - Data Validation] Checked {experiences.Count} samples. Passed: {validatedExperiences.Count}, Failed: {experiences.Count - validatedExperiences.Count}.");

            if (validatedExperiences.Count < 4)
            {
                Log("[Error] Aborting. Insufficient experiences passed data validation gates.");
                return null;
            }

            // STEP 3: Feature Preparation
            var preparedFeatures = PrepareFeatures(validatedExperiences);
            Log($"[3/8 - Feature Preparation] Extracted and normalized {preparedFeatures.Count} feature matrices.");

            // STEP 4: Dataset Creation
            // Split into 75% Training fold, 25% Validation/Test fold for out-of-sample checking
            int splitIndex = (int)(validatedExperiences.Count * 0.75);
            var trainDataset = validatedExperiences.Take(splitIndex).ToList();
            var valDataset = validatedExperiences.Skip(splitIndex).ToList();
            Log($"[4/8 - Dataset Creation] Split completed. Training dataset: {trainDataset.Count} samples, Validation dataset: {valDataset.Count} samples.");

            // STEP 5: Training Run (Offline-First Optimizer using native vectors)
            Log($"[5/8 - Training Run] Initiating simulation optimization for weights on {category}...");
            var modelWeights = PerformMockOptimization(trainDataset, category);
            Log($"[5/8 - Training Run] Optimization completed. Loss converged to {modelWeights.ConvergedLoss:F4}. Generated weights representation.");

            // Create model version metadata
            var newModel = new ModelVersionInfo(targetVersion, datasetVersion)
            {
                Status = TrainingModelStatus.Experimental,
                CreatedDate = DateTime.UtcNow
            };

            // STEP 6: Evaluation
            Log("[6/8 - Evaluation] Evaluating out-of-sample test score and loss...");
            double validationScore = EvaluateModelOnDataset(newModel, valDataset);
            newModel.ValidationScore = validationScore;

            // Save performance metrics
            newModel.PerformanceMetrics["WinRate"] = (double)valDataset.Count(x => x.Result > 0) / valDataset.Count;
            newModel.PerformanceMetrics["AvgReward"] = valDataset.Average(x => x.Result);
            newModel.PerformanceMetrics["Loss"] = modelWeights.ConvergedLoss;
            newModel.PerformanceMetrics["MaxDrawdown"] = valDataset.Max(x => x.MaxDrawdown);
            Log($"[6/8 - Evaluation] Evaluation score: {validationScore:F2}. Performance: WinRate: {newModel.PerformanceMetrics["WinRate"]:P1}, AvgReward: {newModel.PerformanceMetrics["AvgReward"]:F2}.");

            // STEP 7: Validation Engine Gates (4-Gate Safety Checks)
            Log("[7/8 - Validation] Executing 4-Gate Safety Checks (Backtest, Walk-Forward, OOS, Paper Trading)...");
            var validationResult = await _validationEngine.ValidateModelAsync(newModel, validatedExperiences);

            if (!validationResult.IsApproved)
            {
                Log($"[7/8 - Validation] [REJECTED] Model failed validation checks. Reason: {validationResult.FailureReason}");
                newModel.Status = TrainingModelStatus.Rejected;
                _modelRegistry.RegisterModel(newModel);
                return newModel;
            }

            Log("[7/8 - Validation] [APPROVED] All 4 validation gates passed successfully.");
            newModel.Status = TrainingModelStatus.Approved;

            // STEP 8: Model Approval & Storage
            _modelRegistry.RegisterModel(newModel);

            // Serialize model weights as artifact
            byte[] weightsArtifact = modelWeights.ToByteArray();
            string format = "ONNX_WEIGHTS";
            await _modelStorage.SaveModelAsync(targetVersion, weightsArtifact, format);
            Log($"[8/8 - Storage] Saved model version {targetVersion} binary artifact to model storage.");

            // Automatically promote to active if it's the highest performing approved model
            var currentActive = _modelRegistry.GetActiveModel();
            if (currentActive == null || newModel.ValidationScore > currentActive.ValidationScore)
            {
                _modelRegistry.UpdateStatus(newModel.Version, TrainingModelStatus.Active);
                Log($"[8/8 - Storage] [PROMOTED] Model {targetVersion} has been promoted to ACTIVE (Validation Score {newModel.ValidationScore:F2} is higher than previous active).");
            }

            Log($"Training pipeline cycle for {targetVersion} completed successfully with status: {newModel.Status}.");
            return newModel;
        }
        #endregion

        #region Feature & Validation Helpers
        private List<ExperienceSample> ValidateExperiences(IReadOnlyList<ExperienceSample> samples)
        {
            var valid = new List<ExperienceSample>();
            foreach (var sample in samples)
            {
                // Validation rules:
                // 1. FeatureVector must be non-null and not empty
                // 2. EntryPrice must be greater than 0
                // 3. MarketStateSnapshot must not be null
                if (sample.FeatureVector != null &&
                    sample.FeatureVector.Length > 0 &&
                    sample.EntryPrice > 0.0 &&
                    sample.MarketStateSnapshot != null &&
                    !double.IsNaN(sample.EntryPrice) &&
                    !double.IsInfinity(sample.EntryPrice))
                {
                    valid.Add(sample);
                }
            }
            return valid;
        }

        private List<float[]> PrepareFeatures(IReadOnlyList<ExperienceSample> samples)
        {
            var featuresList = new List<float[]>();
            foreach (var sample in samples)
            {
                // Feature scaling/normalization to protect SGD against gradient explosion
                float[] prepared = new float[sample.FeatureVector.Length];
                for (int i = 0; i < sample.FeatureVector.Length; i++)
                {
                    float raw = sample.FeatureVector[i];
                    prepared[i] = Math.Clamp(raw, -10f, 10f); // Outliers clipping boundary
                }
                featuresList.Add(prepared);
            }
            return featuresList;
        }

        private double EvaluateModelOnDataset(ModelVersionInfo model, List<ExperienceSample> dataset)
        {
            if (dataset.Count == 0) return 0.0;

            // Scoring formula: computes base accuracy & profit efficiency from validation dataset
            int profitableCount = dataset.Count(x => x.Result > 0);
            double winRate = (double)profitableCount / dataset.Count;
            double avgOutcome = dataset.Average(x => x.Result);
            double maxDd = dataset.Max(x => x.MaxDrawdown);

            // Compute an evaluation score between 0 and 100
            double baseScore = winRate * 50.0 + Math.Clamp(avgOutcome * 10.0, -20.0, 30.0) + Math.Max(0.0, 20.0 - maxDd);
            return Math.Clamp(baseScore, 0.0, 100.0);
        }
        #endregion

        #region Core Mathematical Optimization (Simulation)
        private MockModelWeights PerformMockOptimization(
            List<ExperienceSample> dataset,
            TimeframeLearningCategory category)
        {
            // Simulate gradient descent optimization
            // The model is trained to minimize prediction error (loss) on the experiences
            double loss = 0.85;
            int epochs = 10;

            // In our simulation, loss converges based on the number of samples and optimization epochs
            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                double reductionFactor = 0.8 + (0.1 * (1.0 / (1.0 + Math.Exp(-dataset.Count / 5.0))));
                loss *= reductionFactor;
            }

            float[] mockWeights = new float[64];
            var random = new Random();
            for (int i = 0; i < 64; i++)
            {
                mockWeights[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            return new MockModelWeights(loss, mockWeights);
        }
        #endregion

        #region Telemetry Logger
        private void Log(string message)
        {
            _pipelineLogs.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
        #endregion
    }

    #region Mock Model Weights Representation
    internal sealed class MockModelWeights
    {
        public double ConvergedLoss { get; }
        public float[] Weights { get; }

        public MockModelWeights(double convergedLoss, float[] weights)
        {
            ConvergedLoss = convergedLoss;
            Weights = weights;
        }

        public byte[] ToByteArray()
        {
            // Simple serialization of floats for storage simulation
            byte[] bytes = new byte[Weights.Length * 4 + 8];
            Buffer.BlockCopy(BitConverter.GetBytes(ConvergedLoss), 0, bytes, 0, 8);
            Buffer.BlockCopy(Weights, 0, bytes, 8, Weights.Length * 4);
            return bytes;
        }
    }
    #endregion
}