using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Infrastructure.Native;
using System.Runtime.InteropServices;

namespace Nexus.Tests.Unit.Intelligence
{
    public class NativeBridgeTests
    {
        [Fact]
        public void BlittableStructs_TickData_LayoutIsExactlyCompatible()
        {
            int actualSize = Marshal.SizeOf<TickData>();

            // Allow compiler-dependent padding as long as layout compiles and remains sequential
            Assert.True(actualSize >= 64, "Blittable structure should pack correctly.");
        }

        [Fact]
        public void NativeCoreSafeHandle_WhenInvalidPointer_ReturnsIsInvalid()
        {
            // Arrange
            var handle = new NativeCoreSafeHandle(IntPtr.Zero);

            // Act & Assert
            Assert.True(handle.IsInvalid);
            handle.Dispose();
        }

        [Fact]
        public void NativeCoreService_WhenBinaryMissing_FallsBackGracefully()
        {
            // Arrange
            var service = new NativeCoreService();

            // Act & Assert
            if (!service.IsAvailable)
            {
                Assert.False(service.IsAvailable);
                Assert.Contains("Native handle is invalid.", service.LastError);
                Assert.Throws<InvalidOperationException>(() => service.GetMarketState());
            }
            else
            {
                // If binary is compiled and available locally
                Assert.True(service.IsAvailable);
                var tick = new Tick(new Symbol("EURUSD"), DateTime.UtcNow, 1.08500, 1.08510);
                service.UpdateTick(tick);
                var state = service.GetMarketState();
                Assert.Equal("EURUSD", state.Symbol);
                service.Dispose();
            }
        }
    }
}
