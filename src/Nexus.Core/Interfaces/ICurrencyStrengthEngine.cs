using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for calculating relative strength scores across major currencies locally from real-time tick streams.
    /// </summary>
    public interface ICurrencyStrengthEngine
    {
        double GetStrengthScore(string currency);
        IReadOnlyDictionary<string, double> GetAllStrengthScores();
        void UpdateFromTick(Tick tick);
    }
}
