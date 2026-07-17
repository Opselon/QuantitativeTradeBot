using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nexus.Core.AI.Entities;
using Nexus.Core.AI.Enums;
using Nexus.Core.AI.Interfaces;
using Nexus.Infrastructure.Storage.Models;

namespace Nexus.Infrastructure.Storage.Repositories
{
    public class ModelRegistry : IModelRegistry
    {
        private readonly TrainingDbContext _context;

        public ModelRegistry(TrainingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task RegisterModelAsync(ModelMetadata model, CancellationToken ct = default)
        {
            var dbModel = new ModelMetadataDbModel
            {
                ModelId = model.ModelId,
                ArchitectureType = model.ArchitectureType,
                Backend = model.Backend.ToString(),
                DatasetId = model.DatasetId,
                ExperimentId = model.ExperimentId,
                FeatureVersion = model.FeatureVersion,
                LabelVersion = model.LabelVersion,
                Status = model.Status.ToString(),
                CheckpointPath = model.CheckpointPath,
                CreatedAtUtc = model.CreatedAtUtc,
                GitCommit = model.GitCommit
            };

            await _context.Models.AddAsync(dbModel, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateModelStatusAsync(string modelId, ModelStatus status, CancellationToken ct = default)
        {
            var model = await _context.Models.FindAsync(new object[] { modelId }, ct);
            if (model != null)
            {
                model.Status = status.ToString();
                _context.Models.Update(model);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<ModelMetadata?> GetChampionAsync(CancellationToken ct = default)
        {
            var dbModel = await _context.Models
                .Where(m => m.Status == ModelStatus.Champion.ToString())
                .OrderByDescending(m => m.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (dbModel == null) return null;

            return MapToDomain(dbModel);
        }

        public async Task<IReadOnlyList<ModelMetadata>> GetCandidatesAsync(CancellationToken ct = default)
        {
            var dbModels = await _context.Models
                .Where(m => m.Status == ModelStatus.Candidate.ToString())
                .OrderByDescending(m => m.CreatedAtUtc)
                .ToListAsync(ct);

            return dbModels.Select(MapToDomain).ToList();
        }

        private static ModelMetadata MapToDomain(ModelMetadataDbModel dbModel)
        {
            return new ModelMetadata(
                dbModel.ModelId,
                dbModel.ArchitectureType,
                Enum.Parse<ExecutionBackend>(dbModel.Backend),
                dbModel.DatasetId,
                dbModel.ExperimentId,
                dbModel.FeatureVersion,
                dbModel.LabelVersion,
                Enum.Parse<ModelStatus>(dbModel.Status),
                dbModel.CheckpointPath,
                dbModel.CreatedAtUtc,
                dbModel.GitCommit
            );
        }
    }
}