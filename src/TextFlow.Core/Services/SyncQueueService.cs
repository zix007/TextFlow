using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Persistence;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class SyncQueueService : ISyncQueueService
{
    private readonly DatabaseService _databaseService;

    public SyncQueueService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task EnqueueUploadJobAsync(string documentId, string localFilePath, string? cloudFileId)
    {
        var job = new SyncJobModel
        {
            DocumentId = documentId,
            LocalFilePath = localFilePath,
            CloudFileId = cloudFileId,
            Action = "Upload",
            Status = SyncJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _databaseService.AddSyncJobAsync(job);
    }

    public async Task EnqueueDownloadJobAsync(string documentId, string localFilePath, string cloudFileId)
    {
        var job = new SyncJobModel
        {
            DocumentId = documentId,
            LocalFilePath = localFilePath,
            CloudFileId = cloudFileId,
            Action = "Download",
            Status = SyncJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _databaseService.AddSyncJobAsync(job);
    }

    public Task<List<SyncJobModel>> GetPendingJobsAsync()
    {
        return _databaseService.GetPendingSyncJobsAsync();
    }

    public Task MarkJobCompletedAsync(long jobId)
    {
        return _databaseService.UpdateSyncJobStatusAsync(jobId, SyncJobStatus.Completed);
    }

    public Task MarkJobFailedAsync(long jobId, string errorMessage)
    {
        return _databaseService.UpdateSyncJobStatusAsync(jobId, SyncJobStatus.Failed, errorMessage);
    }
}
