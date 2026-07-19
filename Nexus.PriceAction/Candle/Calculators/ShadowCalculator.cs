// Imports the System namespace for mathematical Max/Min functions. [Ref: Math-Lib]
// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace for calculating the upper and lower wicks (shadows). [Ref: SRP]
namespace Nexus.PriceAction.Candle.Calculators
{
    // Declares a static class to isolate shadow measurement logic. [Ref: Stateless-Engine]
    public static class ShadowCalculator
    {
        // Calculates the size of the upper shadow (distance from high to the highest body point). [Ref: Method-Def]
        public static decimal CalculateUpperShadow(DomainCandle candle)
        {
            // Extracts the double value from Open Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Extracts the double value from Close Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the double value from High Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal highPrice = (decimal)candle.High.Value;

            // Finds the maximum value between the open and close prices (the top of the body). [Ref: Math-Logic]
            decimal maxBodyPoint = Math.Max(openPrice, closePrice);

            // Returns the difference between the high price and the top of the body. [Ref: Math-Logic]
            return highPrice - maxBodyPoint;
        }

        // Calculates the size of the lower shadow (distance from the lowest body point to low). [Ref: Method-Def]
        public static decimal CalculateLowerShadow(DomainCandle candle)
        {
            // Extracts the double value from Open Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Extracts the double value from Close Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the double value from Low Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal lowPrice = (decimal)candle.Low.Value;

            // Finds the minimum value between the open and close prices (the bottom of the body). [Ref: Math-Logic]
            decimal minBodyPoint = Math.Min(openPrice, closePrice);

            // Returns the difference between the bottom of the body and the low price. [Ref: Math-Logic]
            return minBodyPoint - lowPrice;
        }
    }
}