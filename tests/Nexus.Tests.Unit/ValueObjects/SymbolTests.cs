using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.ValueObjects
{
    public class SymbolTests
    {
        [Fact]
        public void Symbol_Constructor_ShouldSetCorrectProperties()
        {
            var symbol = new Symbol("XAUUSD", digits: 2, tickSize: 0.01, minLot: 0.01, maxLot: 100.0, lotStep: 0.01);

            Assert.Equal("XAUUSD", symbol.Name);
            Assert.Equal(2, symbol.Digits);
            Assert.Equal(0.01, symbol.PointSize);
            Assert.Equal(0.01, symbol.TickSize);
            Assert.Equal(0.01, symbol.MinLot);
            Assert.Equal(100.0, symbol.MaxLot);
            Assert.Equal(0.01, symbol.LotStep);
        }

        [Theory]
        [InlineData(1.234567, 1.23457)] // Rounds to 5 digits for EURUSD
        [InlineData(1.234, 1.234)]
        public void NormalizePrice_ShouldRoundToSymbolDigits(double price, double expected)
        {
            var symbol = new Symbol("EURUSD", digits: 5);
            var normalized = symbol.NormalizePrice(price);
            Assert.Equal(expected, normalized, precision: 5);
        }

        [Theory]
        [InlineData(0.005, 0.01)] // Below minimum lot, should return min lot
        [InlineData(0.015, 0.02)] // Nearest step
        [InlineData(10.054, 10.05)] // Step rounding
        [InlineData(150.0, 100.0)] // Above maximum lot, should return max lot
        public void NormalizeLot_ShouldAdhereToLimitsAndSteps(double lot, double expected)
        {
            var symbol = new Symbol("EURUSD", digits: 5, tickSize: 0.00001, minLot: 0.01, maxLot: 100.0, lotStep: 0.01);
            var normalized = symbol.NormalizeLot(lot);
            Assert.Equal(expected, normalized);
        }

        [Fact]
        public void Equals_ShouldBeTrue_ForSameSymbolNames()
        {
            var sym1 = new Symbol("eurusd");
            var sym2 = new Symbol("EURUSD");

            Assert.Equal(sym1, sym2);
            Assert.True(sym1 == sym2);
        }
    }
}
