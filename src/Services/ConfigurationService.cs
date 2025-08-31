using Microsoft.Extensions.Logging;
using MovieMonitor.Models;
using System.Text.Json;

namespace MovieMonitor.Services;

/// <summary>
/// 設定サービスの実装
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly DirectoryPaths _paths;
    private readonly ILogger<ConfigurationService> _logger;
    private AppSettings? _cachedSettings;

    /// <summary>
    /// 設定変更時のイベント
    /// </summary>
    public event EventHandler<AppSettings>? SettingsChanged;

    public ConfigurationService(DirectoryPaths paths, ILogger<ConfigurationService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            if (!File.Exists(_paths.ConfigFilePath))
            {
                _logger.LogInformation("Configuration file not found, creating default settings");
                _cachedSettings = new AppSettings();
                await SaveSettingsAsync(_cachedSettings);
                return _cachedSettings;
            }

            var json = await File.ReadAllTextAsync(_paths.ConfigFilePath);
            _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            
            _logger.LogDebug("Settings loaded from {ConfigPath}", _paths.ConfigFilePath);
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            Console.WriteLine($"[DEBUG] ConfigurationService.SaveSettingsAsync - Received Theme: {settings.Theme}, ThumbnailSize: {settings.ThumbnailSize}");
            
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Console.WriteLine($"[DEBUG] ConfigurationService.SaveSettingsAsync - Serialized JSON contains: {json.Substring(0, Math.Min(200, json.Length))}...");
            
            await File.WriteAllTextAsync(_paths.ConfigFilePath, json);
            _cachedSettings = settings;
            
            Console.WriteLine($"[DEBUG] ConfigurationService.SaveSettingsAsync - _cachedSettings updated, Theme: {_cachedSettings.Theme}");
            
            _logger.LogInformation("Settings saved to {ConfigPath}. ThumbnailSize: {Size}", _paths.ConfigFilePath, settings.ThumbnailSize);

            // 設定変更イベントを発生
            _logger.LogInformation("Invoking SettingsChanged event. Subscribers: {Count}", SettingsChanged?.GetInvocationList()?.Length ?? 0);
            Console.WriteLine($"[DEBUG] ConfigurationService - About to invoke SettingsChanged event with Theme: {settings.Theme}");
            SettingsChanged?.Invoke(this, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task<T?> GetValueAsync<T>(string key)
    {
        try
        {
            var settings = await GetSettingsAsync();
            var property = typeof(AppSettings).GetProperty(key);
            
            if (property == null)
            {
                _logger.LogWarning("Property {Key} not found in AppSettings", key);
                return default;
            }

            var value = property.GetValue(settings);
            
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get value for key {Key}", key);
            return default;
        }
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        try
        {
            var settings = await GetSettingsAsync();
            var property = typeof(AppSettings).GetProperty(key);
            
            if (property == null)
            {
                _logger.LogWarning("Property {Key} not found in AppSettings", key);
                return;
            }

            if (property.CanWrite)
            {
                property.SetValue(settings, value);
                await SaveSettingsAsync(settings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value for key {Key}", key);
            throw;
        }
    }

    public bool ConfigurationExists()
    {
        return File.Exists(_paths.ConfigFilePath);
    }
}