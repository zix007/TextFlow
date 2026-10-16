using System;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface IGoogleDriveService
{
    bool IsAuthenticated { get; }
    string? UserEmail { get; }
    SyncStatus CurrentSyncStatus { get; }
    event EventHandler<SyncStatus>? SyncStatusChanged;

    Task<bool> AuthenticateAsync(string clientId, string clientSecret);
    Task SignOutAsync();
    Task<string> UploadFileAsync(DocumentModel document, string folderName);
    Task<string> DownloadFileContentAsync(string cloudFileId);
    Task ProcessSyncQueueAsync();
    Task SyncAllOpenDocumentsAsync(System.Collections.Generic.IEnumerable<DocumentModel> documents, string folderName);
}
