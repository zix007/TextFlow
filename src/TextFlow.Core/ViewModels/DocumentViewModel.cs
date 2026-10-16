using System;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using TextFlow.Infrastructure;
using TextFlow.Models;

namespace TextFlow.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    private readonly DocumentModel _model;

    public DocumentModel Model => _model;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _title = "Untitled";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    [NotifyPropertyChangedFor(nameof(LineNumbersText))]
    private string _content = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    private bool _isModified;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EncodingDisplayName))]
    private Encoding _fileEncoding = Encoding.UTF8;

    [ObservableProperty]
    private bool _hasBom;

    [ObservableProperty]
    private int _cursorPosition;

    [ObservableProperty]
    private int _lineNumber = 1;

    [ObservableProperty]
    private int _columnNumber = 1;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncStatusDisplayName))]
    private SyncStatus _syncStatus = SyncStatus.Ready;

    [ObservableProperty]
    private string? _cloudFileId;

    [ObservableProperty]
    private DateTime? _lastSyncedTime;

    public string Id => _model.Id;

    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                return Title;
            }

            string? inferred = GetInferredTitle(Content);
            return !string.IsNullOrEmpty(inferred) ? inferred : Title;
        }
    }

    public string HeaderTitle => IsModified ? $"{DisplayTitle} *" : DisplayTitle;

    public static string? GetInferredTitle(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        string? firstLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (string.IsNullOrWhiteSpace(firstLine)) return null;

        // Clean markdown headings, quotes, and whitespace
        firstLine = firstLine.TrimStart('#', '>', ' ', '\t');

        // Clean bullet list markers (- or * or 1.)
        if (firstLine.StartsWith("- ") || firstLine.StartsWith("* "))
        {
            firstLine = firstLine.Substring(2).Trim();
        }
        else if (char.IsDigit(firstLine[0]))
        {
            int dotIndex = firstLine.IndexOf('.');
            if (dotIndex > 0 && dotIndex < 4 && dotIndex + 1 < firstLine.Length && firstLine[dotIndex + 1] == ' ')
            {
                firstLine = firstLine.Substring(dotIndex + 2).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(firstLine)) return null;

        const int maxLen = 30;
        if (firstLine.Length > maxLen)
        {
            firstLine = firstLine.Substring(0, maxLen).TrimEnd() + "…";
        }

        return firstLine;
    }

    public string EncodingDisplayName => FileEncodingDetector.GetEncodingDisplayName(FileEncoding, HasBom);

    public string SyncStatusDisplayName => SyncStatus switch
    {
        SyncStatus.NotConfigured => "Cloud: Off",
        SyncStatus.Ready => "Ready",
        SyncStatus.Syncing => "Syncing...",
        SyncStatus.Synced => "Synced ✓",
        SyncStatus.Error => "Sync Error ⚠",
        SyncStatus.Offline => "Offline (Queued)",
        _ => "Ready"
    };

    public string FileExtension => string.IsNullOrEmpty(FilePath) ? ".txt" : System.IO.Path.GetExtension(FilePath).ToLowerInvariant();

    public string IconGlyph => FileExtension switch
    {
        ".json" => "\uE943",
        ".csv" => "\uE81E",
        ".sql" => "\uE9D9",
        _ => "\uE8A5"
    };

    public string LineNumbersText
    {
        get
        {
            if (string.IsNullOrEmpty(Content)) return "1";
            int count = 1;
            for (int i = 0; i < Content.Length; i++)
            {
                if (Content[i] == '\r')
                {
                    count++;
                    if (i + 1 < Content.Length && Content[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else if (Content[i] == '\n')
                {
                    count++;
                }
            }
            var sb = new StringBuilder();
            for (int i = 1; i <= count; i++)
            {
                sb.AppendLine(i.ToString());
            }
            return sb.ToString().TrimEnd();
        }
    }

    public DocumentViewModel(DocumentModel model)
    {
        _model = model;
        _title = model.Title;
        _filePath = model.FilePath;
        _content = model.Content;
        _isModified = model.IsModified;
        _fileEncoding = model.FileEncoding;
        _hasBom = model.HasBom;
        _cursorPosition = model.CursorPosition;
        _lineNumber = model.LineNumber;
        _columnNumber = model.ColumnNumber;
        _zoomLevel = model.ZoomLevel;
        _syncStatus = model.SyncStatus;
        _cloudFileId = model.CloudFileId;
        _lastSyncedTime = model.LastSyncedTime;
    }

    public void SyncToModel()
    {
        _model.Title = Title;
        _model.FilePath = FilePath;
        _model.Content = Content;
        _model.IsModified = IsModified;
        _model.FileEncoding = FileEncoding;
        _model.HasBom = HasBom;
        _model.CursorPosition = CursorPosition;
        _model.LineNumber = LineNumber;
        _model.ColumnNumber = ColumnNumber;
        _model.ZoomLevel = ZoomLevel;
        _model.SyncStatus = SyncStatus;
        _model.CloudFileId = CloudFileId;
        _model.LastSyncedTime = LastSyncedTime;
    }

    partial void OnContentChanged(string value)
    {
        _model.Content = value;
        _model.LastModifiedTime = DateTime.UtcNow;
        if (!_model.IsModified)
        {
            IsModified = true;
            _model.IsModified = true;
        }
    }
}
