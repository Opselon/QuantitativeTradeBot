using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface representing the file system/object storage abstraction.
    /// Supports managing AI model binaries, reports, log files, and other flat-file datasets.
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Saves a stream content to the specified path.
        /// </summary>
        /// <param name="path">The target file path relative to the storage root.</param>
        /// <param name="content">The readable stream containing the data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves raw byte content to the specified path.
        /// </summary>
        /// <param name="path">The target file path relative to the storage root.</param>
        /// <param name="content">The byte array containing the data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveFileBytesAsync(string path, byte[] content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads and returns a read-only stream from the specified file path.
        /// </summary>
        /// <param name="path">The target file path relative to the storage root.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A readable <see cref="Stream"/> of the file contents.</returns>
        Task<Stream> LoadFileAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads and returns raw byte content from the specified file path.
        /// </summary>
        /// <param name="path">The target file path relative to the storage root.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The byte array of the file contents.</returns>
        Task<byte[]> LoadFileBytesAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to check relative to the storage root.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the file at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to delete relative to the storage root.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    }
}
