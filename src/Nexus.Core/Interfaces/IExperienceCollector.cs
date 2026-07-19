using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for the Experience Engine, responsible for collecting decisions
    /// and out-of-sample physical trading outcomes to train future intelligence versions.
    /// </summary>
    public interface IExperienceCollector
    {
        /// <summary>
        /// Records an initial decision event with its surrounding context.
        /// </summary>
        void RecordDecision(ExperienceSample sample);

        /// <summary>
        /// Updates an existing recorded decision with out-of-sample physical results (exit price, realized P/L, max drawdown, etc.) once resolved.
        /// </summary>
        Task UpdateOutcomeAsync(System.Guid sampleId, double exitPrice, double maxDrawdown, double holdingTimeMinutes, double outcomeScore, string mistakeClassification);
    }
}
