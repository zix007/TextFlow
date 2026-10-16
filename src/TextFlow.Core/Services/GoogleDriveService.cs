using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
    private DriveService? _driveService;
    private UserCredential? _credential;
    private readonly ISyncQueueService _syncQueueService;
    private readonly ISettingsService _settingsService;
    private SyncStatus _currentSyncStatus = SyncStatus.NotConfigured;
    private string? _userEmail;

    public bool IsAuthenticated => _driveService != null && _credential != null;
    public string? UserEmail => _userEmail;
    public SyncStatus CurrentSyncStatus => _currentSyncStatus;
    public event EventHandler<SyncStatus>? SyncStatusChanged;

    public GoogleDriveService(ISyncQueueService syncQueueService, ISettingsService settingsService)
    {
        _syncQueueService = syncQueueService;
        _settingsService = settingsService;
    }

    private void SetStatus(SyncStatus status)
    {
        _currentSyncStatus = status;
        SyncStatusChanged?.Invoke(this, status);
    }

    public async Task<bool> AuthenticateAsync(string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            SetStatus(SyncStatus.NotConfigured);
            return false;
        }

        try
        {
            SetStatus(SyncStatus.Syncing);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tokenPath = Path.Combine(appData, "TextFlowPro", "token.store");

            var secrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokenPath, true)
            );

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = "TextFlow Pro"
            });

            _userEmail = _credential.UserId ?? "google_user";
            _settingsService.Settings.IsAuthenticated = true;
            _settingsService.Settings.UserEmail = _userEmail;
            await _settingsService.SaveSettingsAsync();

            SetStatus(SyncStatus.Ready);
            return true;
        }
        catch (Exception)
        {
            SetStatus(SyncStatus.Error);
            return false;
        }
    }

    public async Task SignOutAsync()
    {
        _driveService?.Dispose();
        _driveService = null;
        _credential = null;
        _userEmail = null;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string tokenPath = Path.Combine(appData, "TextFlowPro", "token.store");
        if (Directory.Exists(tokenPath))
        {
            try { Directory.Delete(tokenPath, true); } catch { }
        }

        _settingsService.Settings.IsAuthenticated = false;
        _settingsService.Settings.UserEmail = "";
        await _settingsService.SaveSettingsAsync();

        SetStatus(SyncStatus.NotConfigured);
    }

    public async Task<string> UploadFileAsync(DocumentModel document, string folderName)
    {
        if (!IsAuthenticated || _driveService == null)
        {
            // Queue for offline
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                await _syncQueueService.EnqueueUploadJobAsync(document.Id, document.FilePath, document.CloudFileId);
            }
            SetStatus(SyncStatus.Offline);
            return string.Empty;
        }

        try
        {
            SetStatus(SyncStatus.Syncing);

            string folderId = await GetOrCreateFolderAsync(folderName);
            string fileName = Path.GetFileName(document.FilePath ?? (document.Title + ".txt"));

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = string.IsNullOrEmpty(document.CloudFileId) ? new List<string> { folderId } : null
            };

            byte[] byteArray = document.FileEncoding.GetBytes(document.Content);
            using var stream = new MemoryStream(byteArray);

            string uploadedId;
            if (string.IsNullOrEmpty(document.CloudFileId))
            {
                var request = _driveService.Files.Create(fileMetadata, stream, "text/plain");
                request.Fields = "id, name, modifiedTime";
                var progress = await request.UploadAsync();
                if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    throw progress.Exception;
                }
                uploadedId = request.ResponseBody.Id;
            }
            else
            {
                var updateRequest = _driveService.Files.Update(fileMetadata, document.CloudFileId, stream, "text/plain");
                updateRequest.Fields = "id, name, modifiedTime";
                var progress = await updateRequest.UploadAsync();
                if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    throw progress.Exception;
                }
                uploadedId = updateRequest.ResponseBody.Id;
            }

            document.CloudFileId = uploadedId;
            document.LastSyncedTime = DateTime.UtcNow;
            document.SyncStatus = SyncStatus.Synced;
            SetStatus(SyncStatus.Synced);
            return uploadedId;
        }
        catch (Exception)
        {
            SetStatus(SyncStatus.Offline);
            if (!string.IsNullOrEmpty(document.FilePath))
            {
                await _syncQueueService.EnqueueUploadJobAsync(document.Id, document.FilePath, document.CloudFileId);
            }
            return string.Empty;
        }
    }

    public async Task<string> DownloadFileContentAsync(string cloudFileId)
    {
        if (!IsAuthenticated || _driveService == null)
        {
            throw new InvalidOperationException("Not authenticated with Google Drive.");
        }

        using var memoryStream = new MemoryStream();
        var request = _driveService.Files.Get(cloudFileId);
        await request.DownloadAsync(memoryStream);
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        return await reader.ReadToEndAsync();
    }

    public async Task ProcessSyncQueueAsync()
    {
        if (!IsAuthenticated || _driveService == null) return;

        var pending = await _syncQueueService.GetPendingJobsAsync();
        foreach (var job in pending)
        {
            try
            {
                if (job.Action == "Upload" && File.Exists(job.LocalFilePath))
                {
                    string content = await File.ReadAllTextAsync(job.LocalFilePath);
                    var doc = new DocumentModel
                    {
                        Id = job.DocumentId,
                        FilePath = job.LocalFilePath,
                        Title = Path.GetFileName(job.LocalFilePath),
                        Content = content,
                        CloudFileId = job.CloudFileId
                    };
                    await UploadFileAsync(doc, _settingsService.Settings.GoogleSyncFolder);
                    await _syncQueueService.MarkJobCompletedAsync(job.Id);
                }
            }
            catch (Exception ex)
            {
                await _syncQueueService.MarkJobFailedAsync(job.Id, ex.Message);
            }
        }
    }

    public async Task SyncAllOpenDocumentsAsync(IEnumerable<DocumentModel> documents, string folderName)
    {
        SetStatus(SyncStatus.Syncing);
        foreach (var doc in documents)
        {
            await UploadFileAsync(doc, folderName);
        }
        await ProcessSyncQueueAsync();
        SetStatus(SyncStatus.Synced);
    }

    private async Task<string> GetOrCreateFolderAsync(string folderName)
    {
        if (_driveService == null) return "root";

        var listRequest = _driveService.Files.List();
        listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
        listRequest.Fields = "files(id, name)";
        var result = await listRequest.ExecuteAsync();

        if (result.Files != null && result.Files.Count > 0)
        {
            return result.Files[0].Id;
        }

        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder"
        };
        var createRequest = _driveService.Files.Create(folderMetadata);
        createRequest.Fields = "id";
        var folder = await createRequest.ExecuteAsync();
        return folder.Id;
    }
}
