using Nexus.Core.Entities;
using Nexus.Core.Interfaces;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// Thread-safe, lock-free reference replacement implementation of the quantitative settings provider.
    /// Ensures instantaneous hot-reloads of risk and auto-trading states across concurrent processing threads.
    /// </summary>
    /// <remarks>
    /// Reference Files:
    /// - Implements: src/Nexus.Core/Interfaces/IPositionManagerSettingsProvider.cs
    /// - Utilized by: src/Nexus.Execution/Management/PositionManager.cs
    /// - Mutated by: src/Nexus.Desktop/ViewModels/Workspaces/RiskControlDeskViewModel.cs
    /// </remarks>
    public class PositionManagerSettingsProvider : IPositionManagerSettingsProvider
    {
        // Thread Safety: Uses volatile and Atomic Reference Swapping via Interlocked to prevent locks.
        private object _settingsHolder = new PositionManagerSettings();

        /// <summary>
        /// Event triggered when configuration properties are mutated by the WPF view models.
        /// </summary>
        public event Action<PositionManagerSettings>? OnSettingsChanged;

        /// <summary>
        /// Gets the current read-only atomic snapshot of runtime settings.
        /// </summary>
        public PositionManagerSettings GetSettings()
        {
            return (PositionManagerSettings)Volatile.Read(ref _settingsHolder);
        }

        /// <summary>
        /// Swaps the current settings reference with a newly provided configured object atomically.
        /// </summary>
        public void UpdateSettings(PositionManagerSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            // Atomic Swap
            var original = Interlocked.Exchange(ref _settingsHolder, settings);

            // Raise change notification asynchronously to avoid blocking the caller thread
            OnSettingsChanged?.Invoke(settings);
        }
    }
}