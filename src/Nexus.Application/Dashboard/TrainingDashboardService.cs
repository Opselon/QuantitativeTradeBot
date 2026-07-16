using System;
using System.Collections.Generic;

namespace Nexus.Application.Dashboard
{
    public sealed class TrainingDashboardService : ITrainingDashboardService
    {
        public string CurrentModelName { get; private set; } = "Nexus AI v1.x";
        public string ModelVersion { get; private set; } = "1.0.4";
        public string ModelStatus { get; private set; } = "Active (Live Ingress)";
        public int ExperienceCount { get; private set; } = 4120;
        public string TrainingStatus { get; private set; } = "Idle (Model fully trained)";
        public string ValidationStatus { get; private set; } = "PASSED (4/4 gates verified)";

        public double WinRate { get; private set; } = 64.2;
        public double AvgReward { get; private set; } = 8.4;
        public double MaxDrawdown { get; private set; } = 4.2;
        public double ProfitFactor { get; private set; } = 2.1;
        public double LossConvergence { get; private set; } = 0.015;

        private readonly List<string> _modelHistory = new()
        {
            "v1.0.0 (Experimental) - Accuracy: 54%, WinRate: 51.2% - Status: Rejected",
            "v1.0.1 (Experimental) - Accuracy: 58%, WinRate: 53.0% - Status: Approved",
            "v1.0.2 (Approved) - Accuracy: 62%, WinRate: 59.8% - Status: Approved",
            "v1.0.3 (Approved) - Accuracy: 64%, WinRate: 61.2% - Status: Deprecated",
            "v1.0.4 (Active) - Accuracy: 67%, WinRate: 64.2% - Status: Active"
        };
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
