namespace Nexus.Core.AI.Enums
{
    public enum ModelStatus
    {
        Experimental,
        Candidate,
        Champion,
        Rejected,
        Archived,
        Rollback
    }

    public enum ExecutionBackend
    {
        TorchSharp,
        OnnxRuntime,
        TensorRT,
        MLDotNet,
        Custom
    }

    public enum FeatureType
    {
        Momentum,
        Volatility,
        Volume,
        Liquidity,
        Macro,
        Categorical
    }
}