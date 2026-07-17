using System.Collections.Generic;

namespace Nexus.Core.AI.Interfaces
{
    public interface IFeatureDriftDetector
    {
        bool DetectDrift(IReadOnlyList<double[]> baselineDistribution, IReadOnlyList<double[]> currentDistribution);
    }

    public interface IConceptDriftDetector
    {
        bool DetectDrift(IReadOnlyList<double> historicalLoss, IReadOnlyList<double> recentLoss);
    }
}