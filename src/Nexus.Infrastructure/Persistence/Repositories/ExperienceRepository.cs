using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Database implementation of the IExperienceRepository port.
    /// Handles mapping and parsing database schemas back to domain entities safely under Clean Architecture.
    /// </summary>
    public sealed class ExperienceRepository : IExperienceRepository
    {
        private readonly NexusDbContext _dbContext;

        public ExperienceRepository(NexusDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IReadOnlyList<ExperienceRecord>> GetRecentExperiencesAsync(int limit, CancellationToken ct = default)
        {
            var dbModels = await _dbContext.ExperienceRecords
                .OrderByDescending(e => e.TimestampUtc)
                .Take(limit)
                .ToListAsync(ct);

            var list = new List<ExperienceRecord>();
            foreach (var m in dbModels)
            {
                // Parse flat CSV vector back
                float[] features = Array.Empty<float>();
                if (!string.IsNullOrWhiteSpace(m.MarketVectorCsv))
                {
                    try
                    {
                        features = m.MarketVectorCsv.Split(',')
                            .Select(float.Parse)
                            .ToArray();
                    }
                    catch {}
                }

                var record = new ExperienceRecord(
                    m.Id,
                    m.Symbol,
                    features,
                    m.ModelVersion,
                    m.BuyConfidence,
                    m.SellConfidence,
                    m.RiskScore,
                    m.MarketRegime,
                    m.ExecutedAction
                )
                {
                    RealizedPips = m.RealizedPips,
                    IsCompleted = m.IsCompleted
                };

                list.Add(record);
            }

            return list;
        }
    }
}
