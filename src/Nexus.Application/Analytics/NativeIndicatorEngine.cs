using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexus.Application.Observability;

namespace Nexus.Application.Analytics
{
    public class NativeIndicatorEngine : IIndicatorEngine
    {
        private readonly INativeAnalyticsEngine _nativeEngine;
        private readonly IIndicatorEngine _fallbackEngine;
        private readonly ILogger<NativeIndicatorEngine> _logger;

        public string EngineName => _nativeEngine.IsAvailable ? "NativeC++" : $"NativeC++(Fallback:{_fallbackEngine.EngineName})";

        public NativeIndicatorEngine(
            INativeAnalyticsEngine nativeEngine,
            IIndicatorEngine? fallbackEngine = null,
            ILogger<NativeIndicatorEngine>? logger = null)
        {
            _nativeEngine = nativeEngine ?? throw new ArgumentNullException(nameof(nativeEngine));
            _fallbackEngine = fallbackEngine ?? new ManagedIndicatorEngine();
            _logger = logger ?? NullLogger<NativeIndicatorEngine>.Instance;
        }

        public async Task<double[]> CalculateEmaAsync(double[] values, int period)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (period < 1)
                throw new ArgumentException("Period must be greater than or equal to 1.", nameof(period));

            if (values.Length == 0)
                return Array.Empty<double>();

            var context = WorkflowContext.Create("IndicatorCalculation", subsystem: "Analytics");
            using var scope = _logger.BeginWorkflowScope(context);

            if (!_nativeEngine.IsAvailable)
            {
                _logger.LogStructured(LogLevel.Warning, LogEventIds.NativeFallbackUsed,
                    "Native engine is not available. Falling back gracefully to managed implementation ({FallbackName}).",
                    _fallbackEngine.EngineName);

                // Fallback gracefully to managed implementation
                return await _fallbackEngine.CalculateEmaAsync(values, period);
            }

            _logger.LogStructured(LogLevel.Debug, LogEventIds.NativeComputeInvoked,
                "Invoking native C++ EMA calculation. Period={Period}, Size={Size}", period, values.Length);

            var results = new double[values.Length];
            int result = _nativeEngine.CalculateEma(values, values.Length, period, results);
            if (result != 0)
            {
                _logger.LogStructured(LogLevel.Error, LogEventIds.NativeFallbackUsed,
                    "Native calculation failed with error code: {ErrorCode}. Falling back to managed engine.", result);
                return await _fallbackEngine.CalculateEmaAsync(values, period);
            }

            return results;
        }
    }
}
