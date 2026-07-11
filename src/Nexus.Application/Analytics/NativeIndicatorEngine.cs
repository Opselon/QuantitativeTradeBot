using System;
using System.Threading.Tasks;

namespace Nexus.Application.Analytics
{
    public class NativeIndicatorEngine : IIndicatorEngine
    {
        private readonly INativeAnalyticsEngine _nativeEngine;
        private readonly IIndicatorEngine _fallbackEngine;

        public string EngineName => _nativeEngine.IsAvailable ? "NativeC++" : $"NativeC++(Fallback:{_fallbackEngine.EngineName})";

        public NativeIndicatorEngine(INativeAnalyticsEngine nativeEngine, IIndicatorEngine? fallbackEngine = null)
        {
            _nativeEngine = nativeEngine ?? throw new ArgumentNullException(nameof(nativeEngine));
            _fallbackEngine = fallbackEngine ?? new ManagedIndicatorEngine();
        }

        public async Task<double[]> CalculateEmaAsync(double[] values, int period)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (period < 1)
                throw new ArgumentException("Period must be greater than or equal to 1.", nameof(period));

            if (values.Length == 0)
                return Array.Empty<double>();

            if (!_nativeEngine.IsAvailable)
            {
                // Fallback gracefully to managed implementation
                return await _fallbackEngine.CalculateEmaAsync(values, period);
            }

            var results = new double[values.Length];
            int result = _nativeEngine.CalculateEma(values, values.Length, period, results);
            if (result != 0)
            {
                throw new InvalidOperationException($"Native calculation failed with error code: {result}");
            }

            return results;
        }
    }
}
