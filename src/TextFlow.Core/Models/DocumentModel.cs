using System;
using System.IO;
using System.Text;
using TextFlow.Infrastructure;

namespace TextFlow.Models;

public class DocumentModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? FilePath { get; set; }
    public string Title { get; set; } = "Untitled";
    public string Content { get; set; } = string.Empty;
    public bool IsModified { get; set; }
    public Encoding FileEncoding { get; set; } = Encoding.UTF8;
    public bool HasBom { get; set; }
    public string FileExtension => string.IsNullOrEmpty(FilePath) ? ".txt" : Path.GetExtension(FilePath).ToLowerInvariant();
    public DateTime LastModifiedTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastSavedTime { get; set; }
    public int CursorPosition { get; set; }
    public int LineNumber { get; set; } = 1;
    public int ColumnNumber { get; set; } = 1;
    public double ZoomLevel { get; set; } = 1.0;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Ready;
    public string? CloudFileId { get; set; }
    public DateTime? LastSyncedTime { get; set; }

    public static DocumentModel CreateNew(string title = "Untitled", string content = "")
    {
        return new DocumentModel
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Content = content,
            IsModified = false,
            FileEncoding = Encoding.UTF8,
            LastModifiedTime = DateTime.UtcNow
        };
    }
}
