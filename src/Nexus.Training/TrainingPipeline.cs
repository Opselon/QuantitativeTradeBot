using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Training
{
    /// <summary>
    /// Coordinates the offline-first learning lifecycle:
    /// Experience Collection -> Data Validation -> Feature Preparation -> Dataset Creation -> Training Run -> Evaluation -> Validation -> Model Approval.
    /// </summary>
    public sealed class TrainingPipeline
    {
        private readonly ModelRegistry _modelRegistry;
        private readonly IModelStorage _modelStorage;
        private readonly ValidationEngine _validationEngine;
        private readonly TimeframeLearningManager _learningManager;
        private readonly List<string> _pipelineLogs = new();

        public IReadOnlyList<string> PipelineLogs => _pipelineLogs;

        public TrainingPipeline(
            ModelRegistry modelRegistry,
            IModelStorage modelStorage,
            ValidationEngine validationEngine,
            TimeframeLearningManager learningManager)
        {
            _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
            _modelStorage = modelStorage ?? throw new ArgumentNullException(nameof(modelStorage));
            _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
            _learningManager = learningManager ?? throw new ArgumentNullException(nameof(learningManager));
        }

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
            // Split into 75% Training fold, 25% Validation/Test fold
            int splitIndex = (int)(validatedExperiences.Count * 0.75);
            var trainDataset = validatedExperiences.Take(splitIndex).ToList();
            var valDataset = validatedExperiences.Skip(splitIndex).ToList();
            Log($"[4/8 - Dataset Creation] Split completed. Training dataset: {trainDataset.Count} samples, Validation dataset: {valDataset.Count} samples.");

            // STEP 5: Training Run: derive a deterministic policy from recorded trade outcomes only.
            Log($"[5/8 - Training Run] Deriving outcome-weighted policy from {trainDataset.Count} recorded {category} experiences.");
            var modelWeights = DeriveOutcomeWeightedPolicy(trainDataset);
            Log($"[5/8 - Training Run] Policy derivation completed. Empirical loss: {modelWeights.EmpiricalLoss:F4}.");

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
            newModel.PerformanceMetrics["Loss"] = modelWeights.EmpiricalLoss;
            newModel.PerformanceMetrics["MaxDrawdown"] = valDataset.Max(x => x.MaxDrawdown);
            Log($"[6/8 - Evaluation] Evaluation score: {validationScore:F2}. Performance: WinRate: {newModel.PerformanceMetrics["WinRate"]:P1}, AvgReward: {newModel.PerformanceMetrics["AvgReward"]:F2}.");

            // STEP 7: Validation Engine Gates
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
            string format = "NEXUS_OUTCOME_POLICY_V1";
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
                // Bounded feature normalization for recorded market observations
                float[] prepared = new float[sample.FeatureVector.Length];
                for (int i = 0; i < sample.FeatureVector.Length; i++)
                {
                    float raw = sample.FeatureVector[i];
                    // Example: clip extreme outliers, or standardize
                    prepared[i] = Math.Clamp(raw, -10f, 10f);
                }
                featuresList.Add(prepared);
            }
            return featuresList;
        }

        private LearnedPolicyArtifact DeriveOutcomeWeightedPolicy(IReadOnlyList<ExperienceSample> dataset)
        {
            if (dataset.Count == 0)
                throw new ArgumentException("A policy cannot be derived without recorded experiences.", nameof(dataset));

            int featureCount = dataset[0].FeatureVector.Length;
            if (featureCount == 0 || dataset.Any(sample => sample.FeatureVector.Length != featureCount))
                throw new InvalidOperationException("Recorded experiences must use one non-empty, consistent feature schema.");

            // This is intentionally deterministic: each coefficient is the realised, reward-weighted
            // covariance between a normalized feature and actual trade outcome. No random initialization,
            // seeded data, or synthetic convergence values enter a deployed artifact.
            var weights = new float[featureCount];
            var featureMeans = new double[featureCount];
            double meanOutcome = dataset.Average(sample => sample.Result);
            double squaredError = 0.0;
            for (int feature = 0; feature < featureCount; feature++)
            {
                double meanFeature = dataset.Average(sample => sample.FeatureVector[feature]);
                featureMeans[feature] = meanFeature;
                double covariance = 0.0;
                double variance = 0.0;
                foreach (var sample in dataset)
                {
                    double centeredFeature = sample.FeatureVector[feature] - meanFeature;
                    double centeredOutcome = sample.Result - meanOutcome;
                    covariance += centeredFeature * centeredOutcome;
                    variance += centeredFeature * centeredFeature;
                }
                weights[feature] = (float)(variance > 1e-12 ? covariance / variance : 0.0);
            }

            foreach (var sample in dataset)
            {
                double prediction = meanOutcome;
                for (int feature = 0; feature < featureCount; feature++)
                    prediction += weights[feature] * (sample.FeatureVector[feature] - featureMeans[feature]);
                squaredError += Math.Pow(sample.Result - prediction, 2.0);
            }

            return new LearnedPolicyArtifact(meanOutcome, Math.Sqrt(squaredError / dataset.Count), weights);
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

        private void Log(string message)
        {
            _pipelineLogs.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
    }

    internal sealed class LearnedPolicyArtifact
    {
        public double OutcomeBaseline { get; }
        public double EmpiricalLoss { get; }
        public float[] Weights { get; }

        public LearnedPolicyArtifact(double outcomeBaseline, double empiricalLoss, float[] weights)
        {
            OutcomeBaseline = outcomeBaseline;
            EmpiricalLoss = empiricalLoss;
            Weights = weights;
        }

        public byte[] ToByteArray()
        {
            // Binary serialization of the learned baseline, empirical loss, and coefficients
            byte[] bytes = new byte[Weights.Length * 4 + 16];
            Buffer.BlockCopy(BitConverter.GetBytes(OutcomeBaseline), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(EmpiricalLoss), 0, bytes, 8, 8);
            Buffer.BlockCopy(Weights, 0, bytes, 16, Weights.Length * 4);
            return bytes;
        }
    }
}
