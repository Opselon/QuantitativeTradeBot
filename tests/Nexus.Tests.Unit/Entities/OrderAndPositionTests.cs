using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.Entities
{
    public class OrderAndPositionTests
    {
        [Fact]
        public void Order_ShouldTransitionStateSuccessfully()
        {
            var symbol = new Symbol("EURUSD");
            var order = Order.CreateNew(symbol, OrderDirection.Buy, OrderType.Market, volume: 1.0, price: 1.09200);

            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Empty(order.TicketId);

            order.Fill("TICKET_12345", 1.09210);
            Assert.Equal(OrderStatus.Filled, order.Status);
            Assert.Equal("TICKET_12345", order.TicketId);

            // Once filled, it shouldn't transition again
            Assert.Throws<InvalidOperationException>(() => order.Cancel());
        }

        [Fact]
        public void Order_Reject_ShouldSetStatusAndReason()
        {
            var symbol = new Symbol("GBPUSD");
            var order = Order.CreateNew(symbol, OrderDirection.Sell, OrderType.Limit, volume: 0.1, price: 1.25000);

            order.Reject("Insufficient margin");
            Assert.Equal(OrderStatus.Rejected, order.Status);
            Assert.Equal("Insufficient margin", order.StatusReason);
        }

        [Fact]
        public void Position_PnlCalculation_ShouldBeCorrectForForex()
        {
            var symbol = new Symbol("EURUSD");
            // Standard Forex contract size multiplier is 100,000.
            // 1.0 standard lot. Buy entry at 1.09000. Current price is 1.09100 (+10 pips).
            // Expected PnL: (1.09100 - 1.09000) * 1.0 * 100,000 = 0.00100 * 100,000 = $100.00.
            var position = new Position(Guid.NewGuid(), "POS_01", symbol, OrderDirection.Buy, volume: 1.0, entryPrice: 1.09000, currentPrice: 1.09000);
            Assert.Equal(0m, position.UnrealizedPnl);

            position.UpdatePrice(1.09100);
            Assert.Equal(100.0m, position.UnrealizedPnl);

            // Sell position of 0.5 standard lots. Entry at 1.09000. Current price is 1.09200 (-20 pips, in loss).
            // Expected PnL: (1.09000 - 1.09200) * 0.5 * 100,000 = -0.00200 * 50,000 = -$100.00.
            var sellPos = new Position(Guid.NewGuid(), "POS_02", symbol, OrderDirection.Sell, volume: 0.5, entryPrice: 1.09000, currentPrice: 1.09200);
            Assert.Equal(-100.0m, sellPos.UnrealizedPnl);
        }

        [Fact]
        public void Position_PnlCalculation_ShouldBeCorrectForGold()
        {
            var symbol = new Symbol("XAUUSD");
            // Standard Gold contract size multiplier is 100.
            // 2.0 standard lots. Buy entry at 2000.00. Current price is 2010.00 (+$10 gold price move).
            // Expected PnL: (2010 - 2000) * 2.0 * 100 = 10 * 200 = $2,000.00.
            var position = new Position(Guid.NewGuid(), "POS_GOLD", symbol, OrderDirection.Buy, volume: 2.0, entryPrice: 2000.00, currentPrice: 2010.00);
            Assert.Equal(2000.0m, position.UnrealizedPnl);
        }
    }
}
