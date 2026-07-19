using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.ValueObjects
{
    public class MoneyAndLotSizeTests
    {
        [Fact]
        public void Money_Arithmetic_ShouldSupportAddingAndSubtractingSameCurrency()
        {
            var m1 = new Money(100.50m, "USD");
            var m2 = new Money(50.25m, "USD");

            var result = m1 + m2;
            Assert.Equal(150.75m, result.Amount);
            Assert.Equal("USD", result.Currency);

            var diff = m1 - m2;
            Assert.Equal(50.25m, diff.Amount);
        }

        [Fact]
        public void Money_Arithmetic_ShouldThrow_OnCurrencyMismatch()
        {
            var usd = new Money(100m, "USD");
            var eur = new Money(100m, "EUR");

            Assert.Throws<InvalidOperationException>(() => usd + eur);
        }

        [Fact]
        public void Money_Multiplication_ShouldScaleCorrectly()
        {
            var m = new Money(10.50m, "USD");
            var result = m * 2.5m;

            Assert.Equal(26.25m, result.Amount);
        }

        [Fact]
        public void LotSize_Constructor_ShouldThrow_OnNegativeValue()
        {
            Assert.Throws<ArgumentException>(() => new LotSize(-0.1));
        }

        [Fact]
        public void LotSize_Comparison_ShouldWork()
        {
            var lot1 = new LotSize(0.1);
            var lot2 = new LotSize(0.5);

            Assert.True(lot1 < lot2);
            Assert.True(lot2 >= lot1);
        }
    }
}
