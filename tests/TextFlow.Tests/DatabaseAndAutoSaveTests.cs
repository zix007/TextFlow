using System;
using System.IO;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Persistence;
using TextFlow.Services;
using Xunit;

namespace TextFlow.Tests;

public class DatabaseAndAutoSaveTests
{
    [Fact]
    public async Task AutoSaveAndRecovery_SavesAndRestoresCrashFile()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "TextFlow_DbTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        string dbPath = Path.Combine(testDir, "test.db");
        string autoSaveDir = Path.Combine(testDir, "autosaves");

        try
        {
            var db = new DatabaseService(dbPath);
            using var autoSaveService = new AutoSaveService(db, autoSaveDir);
            var recoveryService = new RecoveryService(db);

            var doc = DocumentModel.CreateNew("ImportantDraft", "Vital document text that must not be lost!");
            doc.IsModified = true;

            // Trigger autosave
            await autoSaveService.AutoSaveDocumentAsync(doc);

            // Verify recoverable files
            var recoverable = await recoveryService.GetRecoverableFilesAsync();
            Assert.NotEmpty(recoverable);
            Assert.Equal("ImportantDraft", recoverable[0].Title);

            // Restore
            var restored = await recoveryService.RestoreFileAsync(recoverable[0]);
            Assert.Equal("Vital document text that must not be lost!", restored.Content);
            Assert.True(restored.IsModified);

            // Check that restored is now resolved
            var remaining = await recoveryService.GetRecoverableFilesAsync();
            Assert.Empty(remaining);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task SyncQueue_EnqueuesAndCompletesJobs()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "TextFlow_SyncTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        string dbPath = Path.Combine(testDir, "test_sync.db");

        try
        {
            var db = new DatabaseService(dbPath);
            var queueService = new SyncQueueService(db);

            await queueService.EnqueueUploadJobAsync("doc-1", "C:\\temp\\file.txt", "cloud-1");
            var pending = await queueService.GetPendingJobsAsync();

            Assert.Single(pending);
            Assert.Equal("doc-1", pending[0].DocumentId);
            Assert.Equal(SyncJobStatus.Pending, pending[0].Status);

            await queueService.MarkJobCompletedAsync(pending[0].Id);
            var pendingAfter = await queueService.GetPendingJobsAsync();
            Assert.Empty(pendingAfter);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }
    }
}
