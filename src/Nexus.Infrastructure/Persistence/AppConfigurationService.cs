using System;
using System.IO;
using System.Text.Json;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Persistence
{
    public class AppConfigurationService : IAppConfigurationService
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private AppSettings? _cachedSettings;

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

        public AppSettings GetSettings()
        {
            lock (_lock)
            {
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

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
                catch
                {
                    _cachedSettings = new AppSettings();
                }

                return _cachedSettings;
            }
        }

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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving app settings: {ex.Message}");
                }
            }
        }
    }
}
