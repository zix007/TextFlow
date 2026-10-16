using System;
using System.Threading.Tasks;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface ISettingsService
{
    SettingsModel Settings { get; }
    event EventHandler<SettingsModel>? SettingsChanged;
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    void AddRecentFile(string filePath);
    void ClearRecentFiles();
}
