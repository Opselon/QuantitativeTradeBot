// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   INFRASTRUCTURE LAYER (Storage)
// FILE:    KnowledgeDataReader.cs
// DESCRIPTION: Zero-copy high-performance reader for .dat knowledge datasets.
// ============================================================================

using Nexus.Infrastructure.TorchSharp.Training;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Nexus.Infrastructure.Storage
{
    /// <summary>
    /// Reads binary .dat files efficiently using Memory Mapped Files.
    /// Provides random access to market knowledge rows for training iteration.
    /// </summary>
    public sealed class KnowledgeDataReader : IDisposable
    {
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly int _rowSize;
        private readonly long _totalRows;

        /// <summary>
        /// Gets the total number of knowledge samples available in the file.
        /// </summary>
        public long TotalRows => _totalRows;

        /// <summary>
        /// Opens a .dat knowledge file for read-only access.
        /// </summary>
        public KnowledgeDataReader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Knowledge dataset file not found.", filePath);

            var fileInfo = new FileInfo(filePath);
            _rowSize = Marshal.SizeOf<MarketKnowledgeRow>();
            _totalRows = fileInfo.Length / _rowSize;

            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor = _mmf.CreateViewAccessor(0, fileInfo.Length, MemoryMappedFileAccess.Read);
        }

        /// <summary>
        /// Retrieves a specific knowledge row by index with zero memory allocation.
        /// </summary>
        public MarketKnowledgeRow ReadRow(long index)
        {
            if (index < 0 || index >= _totalRows)
                throw new ArgumentOutOfRangeException(nameof(index));

            MarketKnowledgeRow row;
            _accessor.Read(index * _rowSize, out row);
            return row;
        }

        /// <summary>
        /// Retrieves a batch of rows for neural network training iteration.
        /// </summary>
        public MarketKnowledgeRow[] ReadBatch(long startIndex, int count)
        {
            int actualCount = (int)Math.Min(count, _totalRows - startIndex);
            MarketKnowledgeRow[] batch = new MarketKnowledgeRow[actualCount];

            for (int i = 0; i < actualCount; i++)
            {
                _accessor.Read((startIndex + i) * _rowSize, out batch[i]);
            }

            return batch;
        }

        public void Dispose()
        {
            _accessor.Dispose();
            _mmf.Dispose();
        }
    }
}