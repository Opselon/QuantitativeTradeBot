namespace Nexus.Core.AI.Entities
{
    /// <summary>
    /// Backend-agnostic inference output.
    /// </summary>
    public sealed record Prediction(
        string ModelId,
        string TargetSymbol,
        IReadOnlyDictionary<string, double> Probabilities,
        double ExpectedValue,
        double Confidence,
        IReadOnlyDictionary<string, double> FeatureContributions,
        DateTime TimestampUtc
    );
}