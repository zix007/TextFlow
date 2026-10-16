using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private SettingsModel _settings = new();

    public SettingsModel Settings => _settings;
    public event EventHandler<SettingsModel>? SettingsChanged;

    public SettingsService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDir = Path.Combine(localAppData, "TextFlowPro");
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }
        _settingsFilePath = Path.Combine(appDir, "settings.json");
    }

    public SettingsService(string customSettingsPath)
    {
        _settingsFilePath = customSettingsPath;
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<SettingsModel>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }
        catch
        {
            // Fallback to defaults on error
            _settings = new SettingsModel();
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_settings, options);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            SettingsChanged?.Invoke(this, _settings);
        }
        catch
        {
            // Handle logging or non-critical failure
        }
    }

    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        _settings.RecentFiles.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
        _settings.RecentFiles.Insert(0, filePath);

        while (_settings.RecentFiles.Count > _settings.RecentFileCount)
        {
            _settings.RecentFiles.RemoveAt(_settings.RecentFiles.Count - 1);
        }

        _ = SaveSettingsAsync();
    }

    public void ClearRecentFiles()
    {
        _settings.RecentFiles.Clear();
        _ = SaveSettingsAsync();
    }
}
