namespace Nexus.Application.Analytics
{
    public interface INativeAnalyticsEngine
    {
        bool IsAvailable { get; }
        int CalculateEma(double[] values, int count, int period, double[] outEma);
    }
}
