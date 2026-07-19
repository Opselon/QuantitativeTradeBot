namespace Nexus.Training
{
    /// <summary>
    /// Contract for loading and saving raw neural network model artifacts.
    /// Supports ONNX models, binary weights, or native model configuration streams.
    /// </summary>
    public interface IModelStorage
    {
        Task SaveModelAsync(string version, byte[] modelBytes, string format);
        Task<byte[]> LoadModelAsync(string version);
        Task DeleteModelAsync(string version);
        bool ModelExists(string version);
    }
}
