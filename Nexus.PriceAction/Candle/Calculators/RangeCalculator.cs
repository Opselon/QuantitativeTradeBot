// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace for single-responsibility calculator classes. [Ref: SRP]
namespace Nexus.PriceAction.Candle.Calculators
{
    // Declares a static, stateless class dedicated to calculating a candle's full range. [Ref: Stateless-Engine]
    public static class RangeCalculator
    {
        // Calculates the total distance between the high and low prices. [Ref: Method-Def]
        public static decimal Calculate(DomainCandle candle)
        {
            // Extracts the double value from High Price value object and explicitly casts to decimal. [Ref: CS0266-Fix]
            decimal highPrice = (decimal)candle.High.Value;

            // Extracts the double value from Low Price value object and explicitly casts to decimal. [Ref: CS0266-Fix]
            decimal lowPrice = (decimal)candle.Low.Value;

            // Subtracts the low price from the high price and returns the total range. [Ref: Math-Logic]
            return highPrice - lowPrice;
        }
    }
}