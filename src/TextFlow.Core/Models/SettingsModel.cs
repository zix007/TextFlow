using System.Collections.Generic;
using TextFlow.Infrastructure;

namespace TextFlow.Models;

public class SettingsModel
{
    // General Settings
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public string StartupBehavior { get; set; } = "RestoreSession"; // "RestoreSession" or "BlankDocument"
    public string DefaultEncoding { get; set; } = "UTF-8";
    public int RecentFileCount { get; set; } = 10;
    public List<string> RecentFiles { get; set; } = new();

    // Editor Settings
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 14;
    public string FontStyle { get; set; } = "Regular"; // Regular, Bold, Italic, Bold Italic
    public bool WordWrap { get; set; } = true;
    public bool LineNumbers { get; set; } = true;

    // Theme Settings
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;

    // Google Drive Settings
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";
    public string GoogleSyncFolder { get; set; } = "TextFlowDocs";
    public int SyncIntervalMinutes { get; set; } = 5;
    public bool AutoSyncEnabled { get; set; } = true;
    public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.LocalWins;
    public string UserEmail { get; set; } = "";
    public bool IsAuthenticated { get; set; } = false;

    // AI Extension Settings (Future AI extensible architecture)
    public string AiProvider { get; set; } = "OpenAI";
    public string AiApiKey { get; set; } = "";
    public string AiModel { get; set; } = "gpt-4o";
}
