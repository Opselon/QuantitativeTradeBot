namespace Nexus.Application.Dashboard
{
    public sealed class TrainingDashboardService : ITrainingDashboardService
    {
        public string CurrentModelName { get; private set; } = "UNKNOWN";
        public string ModelVersion { get; private set; } = "UNKNOWN";
        public string ModelStatus { get; private set; } = "UNKNOWN";
        public int ExperienceCount { get; private set; } = 0;
        public string TrainingStatus { get; private set; } = "UNKNOWN";
        public string ValidationStatus { get; private set; } = "UNKNOWN";

        public double WinRate { get; private set; } = 0.0;
        public double AvgReward { get; private set; } = 0.0;
        public double MaxDrawdown { get; private set; } = 0.0;
        public double ProfitFactor { get; private set; } = 0.0;
        public double LossConvergence { get; private set; } = 0.0;

        private readonly List<string> _modelHistory = new();
        public IReadOnlyList<string> ModelHistory => _modelHistory;

        public event Action<TrainingDashboardData>? OnTrainingUpdated;

        public void PushTrainingUpdate(
            string modelName,
            string version,
            string status,
            int expCount,
            string trainingStatus,
            string validationStatus,
            double winRate,
            double avgReward,
            double maxDrawdown,
            double profitFactor,
            double lossConvergence,
            List<string> modelHistory)
        {
            CurrentModelName = modelName;
            ModelVersion = version;
            ModelStatus = status;
            ExperienceCount = expCount;
            TrainingStatus = trainingStatus;
            ValidationStatus = validationStatus;

            WinRate = winRate;
            AvgReward = avgReward;
            MaxDrawdown = maxDrawdown;
            ProfitFactor = profitFactor;
            LossConvergence = lossConvergence;

            _modelHistory.Clear();
            _modelHistory.AddRange(modelHistory);

            OnTrainingUpdated?.Invoke(new TrainingDashboardData
            {
                ModelName = modelName,
                Version = version,
                Status = status,
                ExperienceCount = expCount,
                TrainingStatus = trainingStatus,
                ValidationStatus = validationStatus,
                WinRate = winRate,
                AvgReward = avgReward,
                MaxDrawdown = maxDrawdown,
                ProfitFactor = profitFactor,
                LossConvergence = lossConvergence,
                ModelHistory = new List<string>(modelHistory)
            });
        }
    }
}
