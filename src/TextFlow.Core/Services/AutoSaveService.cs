using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TextFlow.Models;
using TextFlow.Persistence;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class AutoSaveService : IAutoSaveService, IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly string _autoSaveDirectory;
    private System.Threading.Timer? _timer;
    private Func<IEnumerable<DocumentModel>>? _getDocumentsFunc;

    public AutoSaveService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        string tempPath = Path.GetTempPath();
        _autoSaveDirectory = Path.Combine(tempPath, "TextFlowPro");
        if (!Directory.Exists(_autoSaveDirectory))
        {
            Directory.CreateDirectory(_autoSaveDirectory);
        }
    }

    public AutoSaveService(DatabaseService databaseService, string customAutoSaveDir)
    {
        _databaseService = databaseService;
        _autoSaveDirectory = customAutoSaveDir;
        if (!Directory.Exists(_autoSaveDirectory))
        {
            Directory.CreateDirectory(_autoSaveDirectory);
        }
    }

    public void RegisterDocumentProvider(Func<IEnumerable<DocumentModel>> getDocumentsFunc)
    {
        _getDocumentsFunc = getDocumentsFunc;
    }

    public void Start(int intervalSeconds)
    {
        Stop();
        if (intervalSeconds <= 0) intervalSeconds = 30;
        _timer = new System.Threading.Timer(OnTimerCallback, null, TimeSpan.FromSeconds(intervalSeconds), TimeSpan.FromSeconds(intervalSeconds));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTimerCallback(object? state)
    {
        if (_getDocumentsFunc == null) return;
        var docs = _getDocumentsFunc();
        _ = AutoSaveAllAsync(docs);
    }

    public async Task AutoSaveDocumentAsync(DocumentModel document)
    {
        if (!document.IsModified) return;

        string safeName = string.IsNullOrEmpty(document.FilePath)
            ? $"Untitled_{document.Id}.bak"
            : $"{Path.GetFileNameWithoutExtension(document.FilePath)}_{document.Id}.bak";

        string backupPath = Path.Combine(_autoSaveDirectory, safeName);
        await File.WriteAllTextAsync(backupPath, document.Content, document.FileEncoding);

        var record = new AutoSaveRecord
        {
            DocumentId = document.Id,
            OriginalFilePath = document.FilePath,
            Title = document.Title,
            BackupFilePath = backupPath,
            SavedAt = DateTime.UtcNow
        };

        await _databaseService.AddAutoSaveRecordAsync(record);
    }

    public async Task AutoSaveAllAsync(IEnumerable<DocumentModel> documents)
    {
        foreach (var doc in documents)
        {
            if (doc.IsModified)
            {
                await AutoSaveDocumentAsync(doc);
            }
        }
    }

    public async Task ClearAutoSaveAsync(string documentId)
    {
        await _databaseService.ClearAutoSavesForDocumentAsync(documentId);
    }

    public string GetAutoSaveDirectory() => _autoSaveDirectory;

    public void Dispose()
    {
        Stop();
    }
}
