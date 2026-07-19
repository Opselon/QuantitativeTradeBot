using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for the incremental accumulator processor. Maintains prior state matrices and updates them with newly ingested market deltas.
    /// </summary>
    public interface IAccumulatorService
    {
        AccumulatorState GetState(string symbol);
        AccumulatorState UpdateState(FeatureDelta delta);
        void ResetState(string symbol);
    }
}
