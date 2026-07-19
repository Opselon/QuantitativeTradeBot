using System.Collections.Concurrent;

namespace Nexus.Training
{
    /// <summary>
    /// Thread-safe registry for model tracking, managing life cycle state transitions and approvals.
    /// </summary>
    public sealed class ModelRegistry
    {
        private readonly ConcurrentDictionary<string, ModelVersionInfo> _registry = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a new model version.
        /// </summary>
        public void RegisterModel(ModelVersionInfo model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(model.Version)) throw new ArgumentException("Model version cannot be empty.", nameof(model.Version));

            if (!_registry.TryAdd(model.Version, model))
            {
                throw new InvalidOperationException($"Model version '{model.Version}' is already registered.");
            }
        }

        /// <summary>
        /// Retrieves a registered model version by name/number.
        /// </summary>
        public ModelVersionInfo? GetModel(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return null;
            return _registry.TryGetValue(version, out var model) ? model : null;
        }

        /// <summary>
        /// Returns all registered model versions.
        /// </summary>
        public IReadOnlyList<ModelVersionInfo> GetAllModels()
        {
            return _registry.Values.ToList();
        }

        /// <summary>
        /// Updates the lifecycle status of a model version.
        /// Enforces status rules (e.g., only one model can be Active at a time).
        /// </summary>
        public void UpdateStatus(string version, TrainingModelStatus status)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version cannot be empty.", nameof(version));

            if (!_registry.TryGetValue(version, out var model))
            {
                throw new KeyNotFoundException($"Model version '{version}' is not registered.");
            }

            lock (model)
            {
                if (status == TrainingModelStatus.Active)
                {
                    // If a new model becomes active, deprecate or deactivate all other active models
                    foreach (var otherModel in _registry.Values)
                    {
                        if (otherModel.Version != version && otherModel.Status == TrainingModelStatus.Active)
                        {
                            otherModel.Status = TrainingModelStatus.Approved; // Deactivate to Approved status
                        }
                    }
                }

                model.Status = status;
            }
        }

        /// <summary>
        /// Retrieves the currently active model version.
        /// </summary>
        public ModelVersionInfo? GetActiveModel()
        {
            return _registry.Values.FirstOrDefault(m => m.Status == TrainingModelStatus.Active);
        }

        /// <summary>
        /// Clears all models in the registry (useful for testing).
        /// </summary>
        public void Clear()
        {
            _registry.Clear();
        }
    }
}
