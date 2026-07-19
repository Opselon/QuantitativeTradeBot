namespace Nexus.Application.Strategies
{
    public record StrategyDescriptor(
        string StrategyId,
        string Name,
        List<string> SubscribedSymbols,
        Dictionary<string, string> Parameters
    );
}
