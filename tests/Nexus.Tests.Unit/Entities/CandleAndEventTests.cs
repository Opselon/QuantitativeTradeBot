using System;
using Xunit;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;
using Nexus.Core.Enums;
using Nexus.Core.Exceptions;
using Nexus.Core.DomainEvents;

namespace Nexus.Tests.Unit.Entities
{
    public class CandleAndEventTests
    {
        #region Candle Tests

        [Fact]
        public void Candle_Constructor_ShouldInitializeAndValidate_WhenPricesAreValid()
        {
            var symbol = new Symbol("EURUSD");
            var tf = new Timeframe(TimeframeType.H1);
            var time = DateTime.UtcNow;

            var candle = new Candle(
                symbol,
                tf,
                time,
                open: 1.1000,
                high: 1.1050,
                low: 1.0950,
                close: 1.1020,
                volume: 500.0
            );

            Assert.Equal(symbol, candle.Symbol);
            Assert.Equal(tf, candle.Timeframe);
            Assert.Equal(time, candle.Timestamp);
            Assert.Equal(1.1000, candle.Open.Value);
            Assert.Equal(1.1050, candle.High.Value);
            Assert.Equal(1.0950, candle.Low.Value);
            Assert.Equal(1.1020, candle.Close.Value);
            Assert.Equal(500.0, candle.Volume.Value);
        }

        [Fact]
        public void Candle_Constructor_ShouldThrowInvalidPriceException_WhenHighIsLessThanLow()
        {
            var symbol = new Symbol("EURUSD");
            var tf = new Timeframe(TimeframeType.H1);
            var time = DateTime.UtcNow;

            Assert.Throws<InvalidPriceException>(() => new Candle(
                symbol, tf, time, open: 1.1000, high: 1.0900, low: 1.0950, close: 1.1020, volume: 500.0
            ));
        }

        [Fact]
        public void Candle_Constructor_ShouldThrowInvalidPriceException_WhenOpenIsLessThanLow()
        {
            var symbol = new Symbol("EURUSD");
            var tf = new Timeframe(TimeframeType.H1);
            var time = DateTime.UtcNow;

            Assert.Throws<InvalidPriceException>(() => new Candle(
                symbol, tf, time, open: 1.0900, high: 1.1050, low: 1.0950, close: 1.1020, volume: 500.0
            ));
        }

        [Fact]
        public void Candle_Constructor_ShouldThrowInvalidPriceException_WhenCloseIsLessThanLow()
        {
            var symbol = new Symbol("EURUSD");
            var tf = new Timeframe(TimeframeType.H1);
            var time = DateTime.UtcNow;

            Assert.Throws<InvalidPriceException>(() => new Candle(
                symbol, tf, time, open: 1.1000, high: 1.1050, low: 1.0950, close: 1.0900, volume: 500.0
            ));
        }

        [Fact]
        public void Candle_Update_ShouldModifyValuesAndValidateState()
        {
            var symbol = new Symbol("EURUSD");
            var tf = new Timeframe(TimeframeType.H1);
            var time = DateTime.UtcNow;

            var candle = new Candle(
                symbol, tf, time, open: 1.1000, high: 1.1050, low: 1.0950, close: 1.1020, volume: 100.0
            );

            // New higher high
            candle.Update(price: 1.1100, volumeIncrement: 50.0);
            Assert.Equal(1.1100, candle.High.Value);
            Assert.Equal(1.1100, candle.Close.Value);
            Assert.Equal(150.0, candle.Volume.Value);

            // New lower low
            candle.Update(price: 1.0900, volumeIncrement: 50.0);
            Assert.Equal(1.0900, candle.Low.Value);
            Assert.Equal(1.0900, candle.Close.Value);
            Assert.Equal(200.0, candle.Volume.Value);
        }

        #endregion

        #region Domain Event Tests

        [Fact]
        public void PositionOpenedEvent_ShouldRecordFieldsCorrectly()
        {
            var symbol = new Symbol("EURUSD");
            var position = new Position(
                id: Guid.NewGuid(),
                ticketId: "TICKET123",
                symbol: symbol,
                direction: OrderDirection.Buy,
                volume: 1.0,
                entryPrice: 1.1000,
                currentPrice: 1.1000
            );

            var @event = new PositionOpenedEvent(position);

            Assert.NotEqual(Guid.Empty, @event.EventId);
            Assert.Equal(position, @event.Position);
            Assert.True((DateTime.UtcNow - @event.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public void PositionClosedEvent_ShouldRecordFieldsCorrectly()
        {
            var symbol = new Symbol("EURUSD");
            var position = new Position(
                id: Guid.NewGuid(),
                ticketId: "TICKET123",
                symbol: symbol,
                direction: OrderDirection.Buy,
                volume: 1.0,
                entryPrice: 1.1000,
                currentPrice: 1.1000
            );

            var @event = new PositionClosedEvent(position, closePrice: 1.1050, realizedPnl: 500m);

            Assert.NotEqual(Guid.Empty, @event.EventId);
            Assert.Equal(position, @event.Position);
            Assert.Equal(1.1050, @event.ClosePrice.Value);
            Assert.Equal(500m, @event.RealizedPnl);
            Assert.True((DateTime.UtcNow - @event.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public void RiskLimitReachedEvent_ShouldRecordFieldsCorrectly()
        {
            var riskState = new RiskState(
                marginLevel: 80.0,
                maxDrawdown: 10.0,
                currentDrawdown: 12.5,
                openTradeCount: 5,
                totalExposure: 500000.0,
                isTradingBlocked: true
            );

            var @event = new RiskLimitReachedEvent("Drawdown limit breached.", riskState);

            Assert.NotEqual(Guid.Empty, @event.EventId);
            Assert.Equal("Drawdown limit breached.", @event.ViolationReason);
            Assert.Equal(riskState, @event.RiskState);
            Assert.True((DateTime.UtcNow - @event.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public void MarketStateUpdatedEvent_ShouldRecordFieldsCorrectly()
        {
            var marketState = new MarketState(
                symbol: "EURUSD",
                lastUpdatedUtc: DateTime.UtcNow,
                volatility: 1.2,
                momentum: 0.8,
                liquidity: 500.0,
                priceStructure: 0.1,
                probability: 0.75,
                risk: 0.2,
                currencyStrength: 85.0,
                marketRegime: "TrendingBullish"
            );

            var @event = new MarketStateUpdatedEvent(marketState);

            Assert.NotEqual(Guid.Empty, @event.EventId);
            Assert.Equal(marketState, @event.MarketState);
            Assert.True((DateTime.UtcNow - @event.Timestamp).TotalSeconds < 5);
        }

        #endregion
    }
}
