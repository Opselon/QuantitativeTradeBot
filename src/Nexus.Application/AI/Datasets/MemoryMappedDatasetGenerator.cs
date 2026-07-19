// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   APPLICATION LAYER (Dataset Use Cases)
// FILE:    MemoryMappedDatasetGenerator.cs
// REFERENCED BY:
//   - src/Nexus.Training/TrainingPipeline.cs
// ============================================================================

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Application.AI.Datasets
{
    /// <summary>
    /// Generates highly optimized binary ML datasets leveraging operating system Memory-Mapped Files.
    /// Supports parallel zero-copy writing and columnar layouts aligned for high-speed GPU loading.
    /// </summary>
    public sealed class MemoryMappedDatasetGenerator : IDisposable
    {
        private MemoryMappedFile? _mmf;
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>
        /// Fixed-size C# struct representing the exact unmanaged layout of a synchronized multi-timeframe row.
        /// Directly mappable to GPU and Tensor memory buffers without marshaling overhead (Zero-Copy).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ColumnarDataRow
        {
            public double TimestampTicks;
            public double OpenPrice;
            public double HighPrice;
            public double LowPrice;
            public double ClosePrice;
            public double VolumeValue;
            public double VolatilityIndicator;
            public double MomentumIndicator;

            /// <summary>
            /// Fixed 64-element feature array representing native C++ quant outputs.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public float[] QuantFeatures;
        }

        /// <summary>
        /// Compiles and serializes an array of columnar rows directly into a high-performance binary file.
        /// </summary>
        /// <param name="datasetPath">The target physical file output path.</param>
        /// <param name="dataRows">The compiled synchronized market data rows.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The verified path of the mapped file.</returns>
        public async Task<string> BuildMemoryMappedDatasetAsync(
            string datasetPath,
            ColumnarDataRow[] dataRows,
            CancellationToken cancellationToken)
        {
            if (dataRows == null || dataRows.Length == 0)
            {
                throw new ArgumentException("Data rows collection cannot be empty.", nameof(dataRows));
            }

            int rowSize = Marshal.SizeOf<ColumnarDataRow>();
            long totalByteSize = (long)rowSize * dataRows.Length;

            // Maintain file safety by cleaning pre-existing files on target path
            if (File.Exists(datasetPath))
            {
                File.Delete(datasetPath);
            }

            lock (_lock)
            {
                // Instantiate the mapped binary file stream
                _mmf = MemoryMappedFile.CreateFromFile(
                    datasetPath,
                    FileMode.CreateNew,
                    "NexusTftDataset",
                    totalByteSize,
                    MemoryMappedFileAccess.ReadWrite);
            }

            // Execute parallel zero-copy serialization on multiple worker threads
            await Task.Run(() =>
            {
                using (var accessor = _mmf.CreateViewAccessor(0, totalByteSize, MemoryMappedFileAccess.Write))
                {
                    Parallel.For(0, dataRows.Length, new ParallelOptions { CancellationToken = cancellationToken }, i =>
                    {
                        long offset = (long)i * rowSize;
                        ColumnarDataRow row = dataRows[i];

                        // Zero-copy direct memory write
                        accessor.Write(offset, ref row);
                    });
                }
            }, cancellationToken);

            return datasetPath;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _mmf?.Dispose();
                    _mmf = null;
                    _disposed = true;
                }
            }
        }
    }
}