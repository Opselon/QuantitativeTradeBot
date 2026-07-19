using Nexus.Application.Ports;
using System.Text.Json;

namespace Nexus.Infrastructure.Persistence
{
    /// <summary>
    /// Service responsible for managing local application settings.
    /// Provides thread-safe reads and writes to the local configuration JSON store.
    /// </summary>
    public class AppConfigurationService : IAppConfigurationService
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private AppSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigurationService"/> class.
        /// </summary>
        /// <param name="customPath">Optional custom file path directory target.</param>
        public AppConfigurationService(string? customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                _filePath = customPath;
            }
            else
            {
                _filePath = Path.Combine(AppContext.BaseDirectory, "nexus_config.json");
            }
        }

        #region NEW VERSION - Thread-Safe Real-Time Settings Loading
        /// <summary>
        /// Loads the active configuration state.
        /// Bypasses stale in-memory cached checks to ensure the routing engines
        /// read the absolute latest settings modified by the UI toggles.
        /// </summary>
        /// <returns>The deserialized <see cref="AppSettings"/> context.</returns>
        public AppSettings GetSettings()
        {
            lock (_lock)
            {
                // Cache-Bypassing Fix:
                // We always read the file from disk on demand. This prevents service containers
                // from operating on stale, in-memory configurations when the operator switches 
                // between Simulated and Real modes in the WPF Client.
                if (!File.Exists(_filePath))
                {
                    _cachedSettings = new AppSettings();
                    return _cachedSettings;
                }

                try
                {
                    var json = File.ReadAllText(_filePath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AppConfigurationService] Read failure. Using default settings: {ex.Message}");
                    _cachedSettings = new AppSettings();
                }

                return _cachedSettings;
            }
        }

        /// <summary>
        /// Thread-safely persists configuration changes to the local JSON configuration file.
        /// </summary>
        /// <param name="settings">The updated <see cref="AppSettings"/> properties.</param>
        public void SaveSettings(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            lock (_lock)
            {
                _cachedSettings = settings;
                try
                {
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_filePath, json);
                    Console.WriteLine("[AppConfigurationService] App settings written successfully to disk.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AppConfigurationService] Error saving app settings to disk: {ex.Message}");
                }
            }
        }
        #endregion
    }
}