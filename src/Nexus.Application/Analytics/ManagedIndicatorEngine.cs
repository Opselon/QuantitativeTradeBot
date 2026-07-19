namespace Nexus.Application.Analytics
{
    public class ManagedIndicatorEngine : IIndicatorEngine
    {
        public string EngineName => "ManagedC#";

        public Task<double[]> CalculateEmaAsync(double[] values, int period)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (period < 1)
                throw new ArgumentException("Period must be greater than or equal to 1.", nameof(period));

            if (values.Length == 0)
                return Task.FromResult(Array.Empty<double>());

            var results = new double[values.Length];
            if (period == 1)
            {
                Array.Copy(values, results, values.Length);
                return Task.FromResult(results);
            }

            double alpha = 2.0 / (period + 1.0);
            results[0] = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                results[i] = (values[i] * alpha) + (results[i - 1] * (1.0 - alpha));
            }

            return Task.FromResult(results);
        }
    }
}
