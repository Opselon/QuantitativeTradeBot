// Imports the System namespace for mathematical absolute functions. [Ref: Math-Lib]
using System;
// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace for mathematical calculation of candle bodies. [Ref: SRP]
namespace Nexus.PriceAction.Candle.Calculators
{
    // Declares a static, stateless class to isolate body-related calculations. [Ref: Stateless-Engine]
    public static class BodyCalculator
    {
        // Calculates the absolute size of the candle body regardless of direction. [Ref: Method-Def]
        public static decimal Calculate(DomainCandle candle)
        {
            // Extracts the double value from Close Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the double value from Open Price value object and explicitly casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Returns the absolute mathematical difference between close and open prices. [Ref: Math-Logic]
            return Math.Abs(closePrice - openPrice);
        }

        // Evaluates if the candle is bullish (closed higher than it opened). [Ref: Method-Def]
        public static bool IsBullish(DomainCandle candle)
        {
            // Extracts the Close Price double value and casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the Open Price double value and casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Returns true if the close price is strictly greater than the open price. [Ref: Logic-Op]
            return closePrice > openPrice;
        }

        // Evaluates if the candle is bearish (closed lower than it opened). [Ref: Method-Def]
        public static bool IsBearish(DomainCandle candle)
        {
            // Extracts the Close Price double value and casts to decimal. [Ref: CS0030-Fix]
            decimal closePrice = (decimal)candle.Close.Value;

            // Extracts the Open Price double value and casts to decimal. [Ref: CS0030-Fix]
            decimal openPrice = (decimal)candle.Open.Value;

            // Returns true if the close price is strictly less than the open price. [Ref: Logic-Op]
            return closePrice < openPrice;
        }
    }
}