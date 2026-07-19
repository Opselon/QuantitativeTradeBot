using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.Entities
{
    public class TickAndBarTests
    {
        [Fact]
        public void Tick_ShouldCalculateSpreadAndHandleValuesCorrectly()
        {
            var symbol = new Symbol("EURUSD");
            var time = DateTime.UtcNow;
            var tick = new Tick(symbol, time, bid: 1.09250, ask: 1.09265);

            Assert.Equal(symbol, tick.Symbol);
            Assert.Equal(time, tick.Time);
            Assert.Equal(1.09250, tick.Bid);
            Assert.Equal(1.09265, tick.Ask);
            Assert.Equal(0.00015, tick.Spread, precision: 5);
        }

        [Fact]
        public void Bar_Update_ShouldAdjustHighLowAndCloseValues()
        {
            var symbol = new Symbol("EURUSD");
            var barTime = new DateTime(2025, 1, 1, 12, 0, 0);
            var bar = new Bar(symbol, "M1", barTime, open: 1.09200, high: 1.09200, low: 1.09200, close: 1.09200, volume: 1.0);

            // Update with a higher price
            bar.Update(1.09250, 2.0);
            Assert.Equal(1.09250, bar.High);
            Assert.Equal(1.09200, bar.Low);
            Assert.Equal(1.09250, bar.Close);
            Assert.Equal(3.0, bar.Volume);

            // Update with a lower price
            bar.Update(1.09150, 1.5);
            Assert.Equal(1.09250, bar.High);
            Assert.Equal(1.09150, bar.Low);
            Assert.Equal(1.09150, bar.Close);
            Assert.Equal(4.5, bar.Volume);
        }
    }
}
