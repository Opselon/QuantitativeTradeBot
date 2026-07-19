namespace Nexus.Training
{
    /// <summary>
    /// File-based local implementation of model artifact persistence.
    /// Stores files in a configurable directory with security boundaries.
    /// </summary>
    public sealed class FileModelStorage : IModelStorage
    {
        private readonly string _baseDirectory;

        public FileModelStorage(string? baseDirectory = null)
        {
            _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models")
                : Path.GetFullPath(baseDirectory);

            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
            }
        }

        private string GetSafePath(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version cannot be empty.", nameof(version));

            var baseDirResolved = Path.GetFullPath(_baseDirectory);
            var combinedPath = Path.GetFullPath(Path.Combine(baseDirResolved, version));

            var separator = baseDirResolved.EndsWith(Path.DirectorySeparatorChar)
                ? string.Empty
                : Path.DirectorySeparatorChar.ToString();

            var safePrefix = baseDirResolved + separator;

            if (!combinedPath.StartsWith(safePrefix, StringComparison.OrdinalIgnoreCase) && combinedPath != baseDirResolved)
            {
                throw new InvalidOperationException("Path traversal attempt detected.");
            }

            return combinedPath;
        }

        public async Task SaveModelAsync(string version, byte[] modelBytes, string format)
        {
            if (modelBytes == null) throw new ArgumentNullException(nameof(modelBytes));

            string fullPath = GetSafePath(version);

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(fullPath, modelBytes);
        }

        public async Task<byte[]> LoadModelAsync(string version)
        {
            string fullPath = GetSafePath(version);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Model file for version '{version}' not found.");
            }

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task DeleteModelAsync(string version)
        {
            string fullPath = GetSafePath(version);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public bool ModelExists(string version)
        {
            try
            {
                string fullPath = GetSafePath(version);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
