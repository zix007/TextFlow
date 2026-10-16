using System;
using TextFlow.Infrastructure;

namespace TextFlow.Models;

public class SyncJobModel
{
    public long Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string LocalFilePath { get; set; } = string.Empty;
    public string? CloudFileId { get; set; }
    public string Action { get; set; } = "Upload"; // Upload, Download, Delete
    public SyncJobStatus Status { get; set; } = SyncJobStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AutoSaveRecord
{
    public long Id { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string? OriginalFilePath { get; set; }
    public string Title { get; set; } = "Untitled";
    public string BackupFilePath { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public string FormattedSavedAt => SavedAt.ToLocalTime().ToString("g");
    public bool IsRestoredOrDiscarded { get; set; }
}
