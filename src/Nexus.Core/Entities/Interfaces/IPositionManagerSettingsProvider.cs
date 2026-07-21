using Nexus.Core.Entities;
using System;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Contract for the Thread-Safe Singleton managing the active configuration state of the quant engine.
    /// Acts as the communication bridge between presentation UI sliders and the underlying execution managers.
    /// </summary>
    public interface IPositionManagerSettingsProvider
    {
        /// <summary>
        /// Retrieves the current snapshot of runtime configuration settings.
        /// </summary>
        PositionManagerSettings GetSettings();

        /// <summary>
        /// Updates the current active configuration settings atomically.
        /// </summary>
        void UpdateSettings(PositionManagerSettings settings);

        /// <summary>
        /// Event fired whenever any setting value changes, allowing dependent background engines to recalculate logic instantly.
        /// </summary>
        event Action<PositionManagerSettings>? OnSettingsChanged;
    }
}