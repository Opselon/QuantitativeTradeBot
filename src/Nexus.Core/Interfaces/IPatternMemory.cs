using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for historical market pattern indexing and associative similarity searches.
    /// </summary>
    public interface IPatternMemory
    {
        void Store(MarketVector vector, string conditions, string outcome, double performance);
        IReadOnlyList<PatternMatchResult> Search(MarketVector queryVector, double similarityThreshold);
        int Count { get; }
    }
}
