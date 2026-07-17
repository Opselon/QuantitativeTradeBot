using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Core.Interfaces;

namespace Nexus.Training
{
    /// <summary>
    /// Manages the physical exporting of completed SQL ExperienceRecords to optimized, 
    /// local flat-file datasets (/datasets/) for high-speed offline ML optimization.
    /// Separates operational relational databases from large AI historical features storage.
    /// </summary>
    public sealed class TrainingDataStore
    {
        #region Private Fields
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _datasetsDirectory;
        #endregion

        #region Constructor
        public TrainingDataStore(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            // Standard directory setup in execution base directory
            _datasetsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "datasets");
            if (!Directory.Exists(_datasetsDirectory))
            {
                Directory.CreateDirectory(_datasetsDirectory);
            }
        }
        #endregion

        #region Dataset Exporter (Database to CSV Flat-File)
        /// <summary>
        /// Reads completed SQL experiences for the symbol, formats them, and appends them
        /// to a per-symbol local CSV file inside /datasets/ EURUSD.csv, establishing 
        /// a dedicated high-speed training file structure.
        /// </summary>
        public async Task ExportCompletedExperiencesToDatasetAsync(string symbol, CancellationToken ct = default)
        {
            string sanitizedSymbol = symbol.Trim().ToUpperInvariant();
            string filePath = Path.Combine(_datasetsDirectory, $"{sanitizedSymbol}_training_data.csv");

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IExperienceRepository>();
            var records = await repo.GetRecentExperiencesAsync(1000, ct); // Retrieve recent 1000

            var csvBuilder = new StringBuilder();

            // Append CSV Header if file is newly created
            if (!File.Exists(filePath))
            {
                csvBuilder.AppendLine("TimestampUtc,ExecutedAction,RealizedPips,BuyConfidence,SellConfidence,RiskScore,MarketRegime,FeaturesVectorCsv");
            }

            foreach (var r in records)
            {
                if (r.IsCompleted && string.Equals(r.Symbol, sanitizedSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    string vectorCsv = string.Join(";", r.MarketVectorFeatures); // Semi-colon separated feature array

                    csvBuilder.AppendLine($"{r.TimestampUtc:yyyy-MM-dd HH:mm:ss.fffZ},{r.ExecutedAction},{r.RealizedPips:F2},{r.BuyConfidence:F4},{r.SellConfidence:F4},{r.RiskScore:F4},{r.MarketRegime},{vectorCsv}");
                }
            }

            if (csvBuilder.Length > 0)
            {
                // Thread-safe fast append to disk
                await File.AppendAllTextAsync(filePath, csvBuilder.ToString(), Encoding.UTF8, ct);
                Console.WriteLine($"[TRAINING STORE] Successfully appended training features to local file dataset: {filePath}");
            }
        }
        #endregion
    }
}