// Maps the Domain Candle entity to avoid namespace collisions. [Ref: CS0118-Fix]
// Imports the CandleType enum for classification. [Ref: Enum-Import]
using Nexus.PriceAction.Candle.Enums;
using DomainCandle = Nexus.Core.Entities.Candle;

// Defines the namespace for the models used specifically in the Candle Engine. [Ref: Arch-Layer]
namespace Nexus.PriceAction.Candle.Models
{
    // Declares an immutable record to store the mathematical output of a single candle analysis. [Ref: Immutability]
    public record CandleAnalysisResult(
        // Stores the original raw domain candle reference. [Ref: Data-Traceability]
        DomainCandle SourceCandle,

        // Stores the absolute decimal size of the candle body. [Ref: Math-Prop]
        decimal BodySize,

        // Stores the absolute decimal size of the upper wick (shadow). [Ref: Math-Prop]
        decimal UpperShadowSize,

        // Stores the absolute decimal size of the lower wick (shadow). [Ref: Math-Prop]
        decimal LowerShadowSize,

        // Stores the total decimal distance from high to low. [Ref: Math-Prop]
        decimal TotalRange,

        // Stores the ratio of the body size relative to the total range (0 to 1). [Ref: Math-Prop]
        decimal BodyToRangeRatio,

        // Stores the directional momentum of the candle (-1 to +1). [Ref: Math-Prop]
        decimal MomentumDirectional,

        // Stores the classified geometrical shape of the candle. [Ref: Classification]
        CandleType Type,

        // Stores a boolean indicating if the closing price is higher than the opening price. [Ref: Boolean-Flag]
        bool IsBullish,

        // Stores a boolean indicating if the closing price is lower than the opening price. [Ref: Boolean-Flag]
        bool IsBearish
    );
}