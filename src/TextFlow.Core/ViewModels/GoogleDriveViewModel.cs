using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextFlow.Infrastructure;
using TextFlow.Services.Interfaces;

namespace TextFlow.ViewModels;

public partial class GoogleDriveViewModel : ObservableObject
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private string _syncFolder = "TextFlowDocs";

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _statusText = "Not Connected";

    [ObservableProperty]
    private bool _isBusy;

    public GoogleDriveViewModel(IGoogleDriveService googleDriveService, ISettingsService settingsService)
    {
        _googleDriveService = googleDriveService;
        _settingsService = settingsService;

        _clientId = _settingsService.Settings.GoogleClientId;
        _clientSecret = _settingsService.Settings.GoogleClientSecret;
        _syncFolder = _settingsService.Settings.GoogleSyncFolder;
        _isAuthenticated = _googleDriveService.IsAuthenticated || _settingsService.Settings.IsAuthenticated;
        _userEmail = _settingsService.Settings.UserEmail;
        UpdateStatusText(_googleDriveService.CurrentSyncStatus);

        _googleDriveService.SyncStatusChanged += (s, e) => UpdateStatusText(e);
    }

    private void UpdateStatusText(SyncStatus status)
    {
        StatusText = status switch
        {
            SyncStatus.NotConfigured => "Google Drive not configured",
            SyncStatus.Ready => "Connected & Ready",
            SyncStatus.Syncing => "Syncing in progress...",
            SyncStatus.Synced => "All files synchronized",
            SyncStatus.Error => "Sync error occurred",
            SyncStatus.Offline => "Offline - sync queued",
            _ => "Idle"
        };
    }

    [RelayCommand]
    public async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
        {
            StatusText = "Please enter both Client ID and Client Secret.";
            return;
        }

        IsBusy = true;
        StatusText = "Authorizing with Google...";

        bool success = await _googleDriveService.AuthenticateAsync(ClientId, ClientSecret);
        IsBusy = false;

        if (success)
        {
            IsAuthenticated = true;
            UserEmail = _googleDriveService.UserEmail ?? "";
            _settingsService.Settings.GoogleClientId = ClientId;
            _settingsService.Settings.GoogleClientSecret = ClientSecret;
            _settingsService.Settings.GoogleSyncFolder = SyncFolder;
            await _settingsService.SaveSettingsAsync();
            StatusText = $"Signed in as {UserEmail}";
        }
        else
        {
            StatusText = "Authentication failed. Check credentials or connection.";
        }
    }

    [RelayCommand]
    public async Task SignOutAsync()
    {
        IsBusy = true;
        await _googleDriveService.SignOutAsync();
        IsBusy = false;
        IsAuthenticated = false;
        UserEmail = string.Empty;
        StatusText = "Signed out";
    }

    [RelayCommand]
    public async Task SyncNowAsync()
    {
        IsBusy = true;
        await _googleDriveService.ProcessSyncQueueAsync();
        IsBusy = false;
    }
}
