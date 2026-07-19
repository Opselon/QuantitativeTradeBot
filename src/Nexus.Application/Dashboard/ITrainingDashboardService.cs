namespace Nexus.Application.Dashboard
{
    public interface ITrainingDashboardService
    {
        string CurrentModelName { get; }
        string ModelVersion { get; }
        string ModelStatus { get; }
        int ExperienceCount { get; }
        string TrainingStatus { get; }
        string ValidationStatus { get; }

        // Backtest metrics
        double WinRate { get; }
        double AvgReward { get; }
        double MaxDrawdown { get; }
        double ProfitFactor { get; }
        double LossConvergence { get; }

        IReadOnlyList<string> ModelHistory { get; }

        event Action<TrainingDashboardData>? OnTrainingUpdated;

        void PushTrainingUpdate(
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
            List<string> modelHistory);
    }

    public class TrainingDashboardData
    {
        public string ModelName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ExperienceCount { get; set; }
        public string TrainingStatus { get; set; } = string.Empty;
        public string ValidationStatus { get; set; } = string.Empty;
        public double WinRate { get; set; }
        public double AvgReward { get; set; }
        public double MaxDrawdown { get; set; }
        public double ProfitFactor { get; set; }
        public double LossConvergence { get; set; }
        public List<string> ModelHistory { get; set; } = new();
    }
}
