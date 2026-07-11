using System;
using System.Threading.Tasks;
using Xunit;
using Nexus.Application.Analytics;

namespace Nexus.Tests.Unit
{
    public class IndicatorEngineTests
    {
        [Fact]
        public async Task ManagedIndicatorEngine_CalculatesEmaCorrectly()
        {
            var engine = new ManagedIndicatorEngine();
            double[] values = { 10.0, 11.0, 12.0, 13.0, 14.0 };
            int period = 3;

            var results = await engine.CalculateEmaAsync(values, period);

            Assert.Equal(values.Length, results.Length);
            Assert.Equal(10.0, results[0], 5);
            Assert.Equal(10.5, results[1], 5);
            Assert.Equal(11.25, results[2], 5);
            Assert.Equal(12.125, results[3], 5);
            Assert.Equal(13.0625, results[4], 5);
        }

        [Fact]
        public async Task NativeIndicatorEngine_MatchesManagedEngine_WithinTolerance()
        {
            var nativeWrapper = new NativeAnalyticsEngine();
            if (!nativeWrapper.IsAvailable)
            {
                // If native library not compiled/available, skip the comparison test (or pass)
                Console.WriteLine("Native library not available in this environment. Skipping comparison.");
                return;
            }

            var nativeEngine = new NativeIndicatorEngine(nativeWrapper);
            var managedEngine = new ManagedIndicatorEngine();

            double[] values = { 1.0850, 1.0860, 1.0845, 1.0870, 1.0855, 1.0890, 1.0880 };
            int period = 5;

            var nativeResults = await nativeEngine.CalculateEmaAsync(values, period);
            var managedResults = await managedEngine.CalculateEmaAsync(values, period);

            Assert.Equal(managedResults.Length, nativeResults.Length);
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(managedResults[i], nativeResults[i], 5);
            }
        }

        [Fact]
        public async Task NativeIndicatorEngine_FallsBackToManaged_WhenNativeNotAvailable()
        {
            // Simulate missing native library
            var fakeNativeEngine = new FakeNativeAnalyticsEngine(isAvailable: false);
            var managedEngine = new ManagedIndicatorEngine();
            var hybridEngine = new NativeIndicatorEngine(fakeNativeEngine, managedEngine);

            double[] values = { 10.0, 20.0, 30.0 };
            var results = await hybridEngine.CalculateEmaAsync(values, 2);

            Assert.Equal(3, results.Length);
            Assert.Equal(10.0, results[0], 5);
            Assert.Equal(16.666666, results[1], 5);
        }

        [Fact]
        public async Task IndicatorEngine_Throws_OnInvalidInputs()
        {
            var engine = new ManagedIndicatorEngine();

            await Assert.ThrowsAsync<ArgumentNullException>(() => engine.CalculateEmaAsync(null!, 3));
            await Assert.ThrowsAsync<ArgumentException>(() => engine.CalculateEmaAsync(new double[] { 1.0 }, 0));
        }

        private class FakeNativeAnalyticsEngine : INativeAnalyticsEngine
        {
            public bool IsAvailable { get; }

            public FakeNativeAnalyticsEngine(bool isAvailable)
            {
                IsAvailable = isAvailable;
            }

            public int CalculateEma(double[] values, int count, int period, double[] outEma)
            {
                if (!IsAvailable) throw new InvalidOperationException("Not available.");
                return 0;
            }
        }
    }
}
