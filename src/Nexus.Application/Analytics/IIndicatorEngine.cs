using System.Threading.Tasks;

namespace Nexus.Application.Analytics
{
    public interface IIndicatorEngine
    {
        string EngineName { get; }
        Task<double[]> CalculateEmaAsync(double[] values, int period);
    }
}
