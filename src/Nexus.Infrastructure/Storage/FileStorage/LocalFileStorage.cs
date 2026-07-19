using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Storage.FileStorage
{
    /// <summary>
    /// File storage adapter executing read and write IO operations against the local host file system.
    /// Safely isolates paths under a configured base directory and checks for path traversal.
    /// </summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _baseDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileStorage"/> class.
        /// </summary>
        /// <param name="baseDirectory">The root directory for file storage. Defaults to AppContext.BaseDirectory/Storage.</param>
        public LocalFileStorage(string? baseDirectory = null)
        {
            _baseDirectory = Path.GetFullPath(baseDirectory ?? Path.Combine(AppContext.BaseDirectory, "Storage"));
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
            }
        }

        private string GetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be empty.", nameof(path));
            }

            var baseDirResolved = Path.GetFullPath(_baseDirectory);
            var combinedPath = Path.GetFullPath(Path.Combine(baseDirResolved, path));

            // Ensure the resolved full path starts with our safe base directory path.
            // Also append a directory separator char to avoid partial name match bypasses (e.g., /app/StorageSecret matching /app/Storage prefix).
            var separator = baseDirResolved.EndsWith(Path.DirectorySeparatorChar)
                ? string.Empty
                : Path.DirectorySeparatorChar.ToString();

            var safePrefix = baseDirResolved + separator;

            if (!combinedPath.StartsWith(safePrefix, StringComparison.OrdinalIgnoreCase) && combinedPath != baseDirResolved)
            {
                throw new ArgumentException("Directory traversal or rooted path is prohibited.", nameof(path));
            }

            return combinedPath;
        }

        /// <inheritdoc />
        public async Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken = default)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var fullPath = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        /// <inheritdoc />
        public async Task SaveFileBytesAsync(string path, byte[] content, CancellationToken cancellationToken = default)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var fullPath = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Stream> LoadFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The file was not found inside local storage: {path}", path);
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return Task.FromResult<Stream>(fileStream);
        }

        /// <inheritdoc />
        public async Task<byte[]> LoadFileBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The file was not found inside local storage: {path}", path);
            }

            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(path);
            return Task.FromResult(File.Exists(fullPath));
        }

        /// <inheritdoc />
        public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFullPath(path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }
    }
}
