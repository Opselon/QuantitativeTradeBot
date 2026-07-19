using Nexus.Core.Enums;
using Nexus.Core.Exceptions;
using Nexus.Core.ValueObjects;

namespace Nexus.Tests.Unit.ValueObjects
{
    public class NewValueObjectTests
    {
        #region Price Tests

        [Fact]
        public void Price_Constructor_ShouldInitializeCorrectly_WhenValueIsPositive()
        {
            var price = new Price(1.0925);
            Assert.Equal(1.0925, price.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1.5)]
        public void Price_Constructor_ShouldThrowInvalidPriceException_WhenValueIsZeroOrNegative(double invalidValue)
        {
            Assert.Throws<InvalidPriceException>(() => new Price(invalidValue));
        }

        [Fact]
        public void Price_Equality_ShouldBeValueBased()
        {
            var p1 = new Price(100.5);
            var p2 = new Price(100.5);
            var p3 = new Price(101.5);

            Assert.True(p1.Equals(p2));
            Assert.True(p1 == p2);
            Assert.False(p1 == p3);
            Assert.True(p1 != p3);
            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
        }

        [Fact]
        public void Price_MathOperators_ShouldWorkAsExpected()
        {
            var p1 = new Price(10.0);
            var p2 = new Price(5.0);

            var sum = p1 + p2;
            var diff = p1 - p2;
            var prod = p1 * 2.0;
            var quot = p1 / 2.0;

            Assert.Equal(15.0, sum.Value);
            Assert.Equal(5.0, diff.Value);
            Assert.Equal(20.0, prod.Value);
            Assert.Equal(5.0, quot.Value);
        }

        [Fact]
        public void Price_Casts_ShouldWorkAsExpected()
        {
            Price p = 1.234;
            double d = p;

            Assert.Equal(1.234, p.Value);
            Assert.Equal(1.234, d);
        }

        #endregion

        #region Volume Tests

        [Fact]
        public void Volume_Constructor_ShouldInitializeCorrectly_WhenValueIsZeroOrPositive()
        {
            var volZero = new Volume(0.0);
            var volPositive = new Volume(1.5);

            Assert.Equal(0.0, volZero.Value);
            Assert.Equal(1.5, volPositive.Value);
        }

        [Fact]
        public void Volume_Constructor_ShouldThrowInvalidVolumeException_WhenValueIsNegative()
        {
            Assert.Throws<InvalidVolumeException>(() => new Volume(-0.1));
        }

        [Fact]
        public void Volume_EqualityAndMath_ShouldWorkAsExpected()
        {
            var v1 = new Volume(10.0);
            var v2 = new Volume(10.0);
            var v3 = new Volume(5.0);

            Assert.Equal(v1, v2);
            Assert.True(v1 == v2);
            Assert.True(v1 > v3);
            Assert.Equal(15.0, (v1 + v3).Value);
        }

        #endregion

        #region Percentage Tests

        [Fact]
        public void Percentage_Constructor_ShouldInitializeCorrectly()
        {
            var pct = new Percentage(15.5);
            Assert.Equal(15.5, pct.Value);
            Assert.Equal(0.155, pct.AsFraction, precision: 3);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Percentage_Constructor_ShouldThrowInvalidPercentageException_WhenValueIsInvalid(double invalidPct)
        {
            Assert.Throws<InvalidPercentageException>(() => new Percentage(invalidPct));
        }

        [Fact]
        public void Percentage_Of_ShouldCalculateCorrectPercentageValue()
        {
            var pct = new Percentage(5.0); // 5%

            Assert.Equal(5.0, pct.Of(100.0));
            Assert.Equal(10.0, pct.Of(200.0));
            Assert.Equal(5.0m, pct.Of(100.0m));
        }

        #endregion

        #region RiskAmount Tests

        [Fact]
        public void RiskAmount_Constructor_ShouldInitializeCorrectly_WhenValueIsNonNegative()
        {
            var risk = new RiskAmount(100m, "USD");
            Assert.Equal(100m, risk.Amount.Amount);
            Assert.Equal("USD", risk.Amount.Currency);
        }

        [Fact]
        public void RiskAmount_Constructor_ShouldThrowInvalidRiskException_WhenValueIsNegative()
        {
            Assert.Throws<InvalidRiskException>(() => new RiskAmount(-10m, "USD"));
        }

        [Fact]
        public void RiskAmount_EqualityAndOperators_ShouldBeValueBased()
        {
            var r1 = new RiskAmount(50m, "EUR");
            var r2 = new RiskAmount(50m, "EUR");
            var r3 = new RiskAmount(60m, "EUR");

            Assert.Equal(r1, r2);
            Assert.True(r1 == r2);
            Assert.True(r3 > r1);
            Assert.Equal(110m, (r1 + r3).Amount.Amount);
        }

        #endregion

        #region Timeframe Tests

        [Fact]
        public void Timeframe_Constructor_ShouldInitializeCorrectly_FromEnum()
        {
            var tf = new Timeframe(TimeframeType.M15);
            Assert.Equal(TimeframeType.M15, tf.Type);
            Assert.Equal(TimeSpan.FromMinutes(15), tf.Duration);
        }

        [Fact]
        public void Timeframe_Constructor_ShouldInitializeCorrectly_FromString()
        {
            var tf = new Timeframe("H4");
            Assert.Equal(TimeframeType.H4, tf.Type);
            Assert.Equal(TimeSpan.FromHours(4), tf.Duration);
        }

        [Fact]
        public void Timeframe_Constructor_ShouldThrowException_WhenInvalidStringProvided()
        {
            Assert.Throws<DomainException>(() => new Timeframe("INVALID_TF"));
        }

        [Fact]
        public void Timeframe_Equality_ShouldBeCorrect()
        {
            var t1 = new Timeframe(TimeframeType.H1);
            var t2 = new Timeframe("H1");
            var t3 = new Timeframe(TimeframeType.D1);

            Assert.Equal(t1, t2);
            Assert.True(t1 == t2);
            Assert.False(t1 == t3);
        }

        #endregion

        #region MarketSession Tests

        [Fact]
        public void MarketSession_Constructor_ShouldInitializeCorrectly_WithValidRanges()
        {
            var session = new MarketSession("London", TimeSpan.FromHours(8), TimeSpan.FromHours(16));
            Assert.Equal("London", session.Name);
            Assert.Equal(TimeSpan.FromHours(8), session.StartTimeUtc);
            Assert.Equal(TimeSpan.FromHours(16), session.EndTimeUtc);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void MarketSession_Constructor_ShouldThrowException_WhenNameIsWhitespace(string? invalidName)
        {
            Assert.Throws<DomainException>(() => new MarketSession(invalidName!, TimeSpan.FromHours(8), TimeSpan.FromHours(16)));
        }

        [Fact]
        public void MarketSession_IsActive_ShouldReturnTrue_WhenTimeIsInIntradaySession()
        {
            // Standard day session: 08:00 to 16:00
            var session = new MarketSession("London", TimeSpan.FromHours(8), TimeSpan.FromHours(16));

            // 12:00:00 UTC
            var activeTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            // 07:00:00 UTC
            var inactiveTime = new DateTime(2025, 1, 1, 7, 0, 0, DateTimeKind.Utc);

            Assert.True(session.IsActive(activeTime));
            Assert.False(session.IsActive(inactiveTime));
        }

        [Fact]
        public void MarketSession_IsActive_ShouldReturnTrue_WhenTimeIsInOvernightSession()
        {
            // Overnight session: 22:00 to 06:00
            var session = new MarketSession("Overnight", TimeSpan.FromHours(22), TimeSpan.FromHours(6));

            // 23:00:00 UTC
            var activeTimeLate = new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc);
            // 03:00:00 UTC
            var activeTimeEarly = new DateTime(2025, 1, 1, 3, 0, 0, DateTimeKind.Utc);
            // 12:00:00 UTC
            var inactiveTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.True(session.IsActive(activeTimeLate));
            Assert.True(session.IsActive(activeTimeEarly));
            Assert.False(session.IsActive(inactiveTime));
        }

        #endregion
    }
}
