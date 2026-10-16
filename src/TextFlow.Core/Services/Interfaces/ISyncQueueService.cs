using System.Collections.Generic;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface ISyncQueueService
{
    Task EnqueueUploadJobAsync(string documentId, string localFilePath, string? cloudFileId);
    Task EnqueueDownloadJobAsync(string documentId, string localFilePath, string cloudFileId);
    Task<List<SyncJobModel>> GetPendingJobsAsync();
    Task MarkJobCompletedAsync(long jobId);
    Task MarkJobFailedAsync(long jobId, string errorMessage);
}
