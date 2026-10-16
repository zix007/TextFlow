using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IGoogleDriveService _googleDriveService;

    // General
    [ObservableProperty]
    private int _autoSaveIntervalSeconds;

    [ObservableProperty]
    private string _startupBehavior = "RestoreSession";

    [ObservableProperty]
    private string _defaultEncoding = "UTF-8";

    // Editor
    [ObservableProperty]
    private string _fontFamily = "Consolas";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private string _fontStyle = "Regular";

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private bool _lineNumbers = true;

    // Theme
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeIndex))]
    private AppThemeMode _themeMode = AppThemeMode.System;

    public int ThemeIndex
    {
        get => (int)ThemeMode;
        set
        {
            if (value >= 0 && value <= 2)
            {
                ThemeMode = (AppThemeMode)value;
                OnPropertyChanged();
            }
        }
    }

    // Google Drive
    [ObservableProperty]
    private string _googleClientId = string.Empty;

    [ObservableProperty]
    private string _googleClientSecret = string.Empty;

    [ObservableProperty]
    private string _googleSyncFolder = "TextFlowDocs";

    [ObservableProperty]
    private int _syncIntervalMinutes = 5;

    [ObservableProperty]
    private bool _autoSyncEnabled = true;

    [ObservableProperty]
    private ConflictResolutionStrategy _conflictStrategy = ConflictResolutionStrategy.LocalWins;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectButtonText))]
    private bool _isGoogleConnected;

    public string ConnectButtonText => IsGoogleConnected ? "Disconnect" : "Connect Google Drive";

    [ObservableProperty]
    private string _googleUserEmail = string.Empty;

    // AI Extension
    [ObservableProperty]
    private string _aiApiKey = string.Empty;

    [ObservableProperty]
    private string _aiModel = "gpt-4o";

    public List<string> AvailableFontFamilies { get; } = new()
    {
        "Consolas",
        "Cascadia Code",
        "Segoe UI",
        "Courier New",
        "Lucida Console"
    };

    public List<string> AvailableFontStyles { get; } = new()
    {
        "Regular",
        "Bold",
        "Italic",
        "Bold Italic"
    };

    public List<string> AvailableEncodings { get; } = new()
    {
        "UTF-8",
        "UTF-8 with BOM",
        "UTF-16 LE",
        "UTF-16 BE",
        "ASCII"
    };

    public SettingsViewModel(ISettingsService settingsService, IGoogleDriveService googleDriveService)
    {
        _settingsService = settingsService;
        _googleDriveService = googleDriveService;
        LoadFromSettings();
    }

    public void LoadFromSettings()
    {
        var s = _settingsService.Settings;
        AutoSaveIntervalSeconds = s.AutoSaveIntervalSeconds;
        StartupBehavior = s.StartupBehavior;
        DefaultEncoding = s.DefaultEncoding;

        FontFamily = s.FontFamily;
        FontSize = s.FontSize;
        FontStyle = s.FontStyle;
        WordWrap = s.WordWrap;
        LineNumbers = s.LineNumbers;

        ThemeMode = s.ThemeMode;

        GoogleClientId = s.GoogleClientId;
        GoogleClientSecret = s.GoogleClientSecret;
        GoogleSyncFolder = s.GoogleSyncFolder;
        SyncIntervalMinutes = s.SyncIntervalMinutes;
        AutoSyncEnabled = s.AutoSyncEnabled;
        ConflictStrategy = s.ConflictStrategy;
        IsGoogleConnected = _googleDriveService.IsAuthenticated || s.IsAuthenticated;
        GoogleUserEmail = s.UserEmail;

        AiApiKey = s.AiApiKey;
        AiModel = s.AiModel;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var s = _settingsService.Settings;
        s.AutoSaveIntervalSeconds = AutoSaveIntervalSeconds;
        s.StartupBehavior = StartupBehavior;
        s.DefaultEncoding = DefaultEncoding;

        s.FontFamily = FontFamily;
        s.FontSize = FontSize;
        s.FontStyle = FontStyle;
        s.WordWrap = WordWrap;
        s.LineNumbers = LineNumbers;

        s.ThemeMode = ThemeMode;

        s.GoogleClientId = GoogleClientId;
        s.GoogleClientSecret = GoogleClientSecret;
        s.GoogleSyncFolder = GoogleSyncFolder;
        s.SyncIntervalMinutes = SyncIntervalMinutes;
        s.AutoSyncEnabled = AutoSyncEnabled;
        s.ConflictStrategy = ConflictStrategy;

        s.AiApiKey = AiApiKey;
        s.AiModel = AiModel;

        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    public async Task ConnectGoogleDriveAsync()
    {
        if (string.IsNullOrWhiteSpace(GoogleClientId) || string.IsNullOrWhiteSpace(GoogleClientSecret))
        {
            return;
        }

        bool success = await _googleDriveService.AuthenticateAsync(GoogleClientId, GoogleClientSecret);
        IsGoogleConnected = success;
        GoogleUserEmail = _googleDriveService.UserEmail ?? "";
    }

    [RelayCommand]
    public async Task DisconnectGoogleDriveAsync()
    {
        await _googleDriveService.SignOutAsync();
        IsGoogleConnected = false;
        GoogleUserEmail = string.Empty;
    }
}
