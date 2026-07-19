// Imports the System namespace. [Ref: Core-Sys]
// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace for momentum computations. [Ref: SRP]
namespace Nexus.PriceAction.Candle.Calculators
{
    // Declares a static class to calculate directional momentum. [Ref: Stateless-Engine]
    public static class MomentumCalculator
    {
        // Calculates the directional power of the candle normalized between -1 and +1. [Ref: Method-Def]
        public static decimal CalculateDirectionalMomentum(DomainCandle candle, decimal totalRange)
        {
            // Checks if the total range is zero to prevent DivideByZero exceptions. [Ref: Guard-Clause]
            if (totalRange == 0)
            {
                // Returns zero momentum if the market is frozen (High == Low). [Ref: Edge-Case]
                return 0m;
            }

            // Extracts the double value from Close Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the double value from Open Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Calculates the directional difference (positive if bullish, negative if bearish). [Ref: Math-Logic]
            decimal priceDifference = closePrice - openPrice;

            // Divides the difference by total range to normalize the value between -1.0 and 1.0. [Ref: Math-Logic]
            return priceDifference / totalRange;
        }
    }
}