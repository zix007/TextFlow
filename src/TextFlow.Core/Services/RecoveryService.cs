using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Persistence;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class RecoveryService : IRecoveryService
{
    private readonly DatabaseService _databaseService;

    public RecoveryService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<List<AutoSaveRecord>> GetRecoverableFilesAsync()
    {
        var records = await _databaseService.GetUnrestoredAutoSavesAsync();
        var validRecords = new List<AutoSaveRecord>();

        foreach (var rec in records)
        {
            if (File.Exists(rec.BackupFilePath))
            {
                validRecords.Add(rec);
            }
            else
            {
                // Mark clean if physical file was deleted
                await _databaseService.MarkAutoSaveResolvedAsync(rec.Id);
            }
        }

        return validRecords;
    }

    public async Task<DocumentModel> RestoreFileAsync(AutoSaveRecord record)
    {
        string content = "";
        var encoding = System.Text.Encoding.UTF8;
        bool hasBom = false;

        if (File.Exists(record.BackupFilePath))
        {
            encoding = FileEncodingDetector.DetectEncoding(record.BackupFilePath, out hasBom);
            content = await File.ReadAllTextAsync(record.BackupFilePath, encoding);
        }

        var doc = new DocumentModel
        {
            Id = record.DocumentId,
            FilePath = record.OriginalFilePath,
            Title = record.Title,
            Content = content,
            FileEncoding = encoding,
            HasBom = hasBom,
            IsModified = true, // mark modified since restored from crash
            LastModifiedTime = record.SavedAt
        };

        await _databaseService.MarkAutoSaveResolvedAsync(record.Id);
        return doc;
    }

    public async Task DiscardFileAsync(AutoSaveRecord record)
    {
        try
        {
            if (File.Exists(record.BackupFilePath))
            {
                File.Delete(record.BackupFilePath);
            }
        }
        catch
        {
        }

        await _databaseService.MarkAutoSaveResolvedAsync(record.Id);
    }

    public async Task DiscardAllAsync()
    {
        var records = await GetRecoverableFilesAsync();
        foreach (var rec in records)
        {
            await DiscardFileAsync(rec);
        }
    }
}
