using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using TextFlow.Infrastructure;
using TextFlow.Models;

namespace TextFlow.Persistence;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDir = Path.Combine(localAppData, "TextFlowPro");
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        string dbPath = Path.Combine(appDir, "textflow.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    public DatabaseService(string customDbPath)
    {
        string dir = Path.GetDirectoryName(customDbPath)!;
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _connectionString = $"Data Source={customDbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS SyncJobs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId TEXT NOT NULL,
                LocalFilePath TEXT NOT NULL,
                CloudFileId TEXT,
                Action TEXT NOT NULL,
                Status INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                ExecutedAt TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT
            );

            CREATE TABLE IF NOT EXISTS AutoSaveRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentId TEXT NOT NULL,
                OriginalFilePath TEXT,
                Title TEXT NOT NULL,
                BackupFilePath TEXT NOT NULL,
                SavedAt TEXT NOT NULL,
                IsRestoredOrDiscarded INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS FileMetadata (
                FilePath TEXT PRIMARY KEY,
                CloudFileId TEXT,
                LastSyncedTime TEXT,
                Checksum TEXT
            );
        ";
        command.ExecuteNonQuery();
    }

    // --- SyncJobs Operations ---

    public async Task<long> AddSyncJobAsync(SyncJobModel job)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO SyncJobs (DocumentId, LocalFilePath, CloudFileId, Action, Status, CreatedAt, ExecutedAt, RetryCount, ErrorMessage)
            VALUES (@DocumentId, @LocalFilePath, @CloudFileId, @Action, @Status, @CreatedAt, @ExecutedAt, @RetryCount, @ErrorMessage);
            SELECT last_insert_rowid();
        ";
        command.Parameters.AddWithValue("@DocumentId", job.DocumentId);
        command.Parameters.AddWithValue("@LocalFilePath", job.LocalFilePath);
        command.Parameters.AddWithValue("@CloudFileId", (object?)job.CloudFileId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Action", job.Action);
        command.Parameters.AddWithValue("@Status", (int)job.Status);
        command.Parameters.AddWithValue("@CreatedAt", job.CreatedAt.ToString("o"));
        command.Parameters.AddWithValue("@ExecutedAt", job.ExecutedAt.HasValue ? job.ExecutedAt.Value.ToString("o") : DBNull.Value);
        command.Parameters.AddWithValue("@RetryCount", job.RetryCount);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)job.ErrorMessage ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public async Task<List<SyncJobModel>> GetPendingSyncJobsAsync()
    {
        var list = new List<SyncJobModel>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, DocumentId, LocalFilePath, CloudFileId, Action, Status, CreatedAt, ExecutedAt, RetryCount, ErrorMessage FROM SyncJobs WHERE Status = 0 ORDER BY CreatedAt ASC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SyncJobModel
            {
                Id = reader.GetInt64(0),
                DocumentId = reader.GetString(1),
                LocalFilePath = reader.GetString(2),
                CloudFileId = reader.IsDBNull(3) ? null : reader.GetString(3),
                Action = reader.GetString(4),
                Status = (SyncJobStatus)reader.GetInt32(5),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                ExecutedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                RetryCount = reader.GetInt32(8),
                ErrorMessage = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }
        return list;
    }

    public async Task UpdateSyncJobStatusAsync(long id, SyncJobStatus status, string? errorMessage = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE SyncJobs 
            SET Status = @Status, 
                ExecutedAt = @ExecutedAt, 
                ErrorMessage = @ErrorMessage,
                RetryCount = RetryCount + CASE WHEN @Status = 3 THEN 1 ELSE 0 END
            WHERE Id = @Id
        ";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Status", (int)status);
        command.Parameters.AddWithValue("@ExecutedAt", DateTime.UtcNow.ToString("o"));
        command.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    // --- AutoSave Operations ---

    public async Task AddAutoSaveRecordAsync(AutoSaveRecord record)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO AutoSaveRecords (DocumentId, OriginalFilePath, Title, BackupFilePath, SavedAt, IsRestoredOrDiscarded)
            VALUES (@DocumentId, @OriginalFilePath, @Title, @BackupFilePath, @SavedAt, 0);
        ";
        command.Parameters.AddWithValue("@DocumentId", record.DocumentId);
        command.Parameters.AddWithValue("@OriginalFilePath", (object?)record.OriginalFilePath ?? DBNull.Value);
        command.Parameters.AddWithValue("@Title", record.Title);
        command.Parameters.AddWithValue("@BackupFilePath", record.BackupFilePath);
        command.Parameters.AddWithValue("@SavedAt", record.SavedAt.ToString("o"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<AutoSaveRecord>> GetUnrestoredAutoSavesAsync()
    {
        var list = new List<AutoSaveRecord>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, DocumentId, OriginalFilePath, Title, BackupFilePath, SavedAt FROM AutoSaveRecords WHERE IsRestoredOrDiscarded = 0 ORDER BY SavedAt DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new AutoSaveRecord
            {
                Id = reader.GetInt64(0),
                DocumentId = reader.GetString(1),
                OriginalFilePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                Title = reader.GetString(3),
                BackupFilePath = reader.GetString(4),
                SavedAt = DateTime.Parse(reader.GetString(5)),
                IsRestoredOrDiscarded = false
            });
        }
        return list;
    }

    public async Task MarkAutoSaveResolvedAsync(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE AutoSaveRecords SET IsRestoredOrDiscarded = 1 WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ClearAutoSavesForDocumentAsync(string documentId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE AutoSaveRecords SET IsRestoredOrDiscarded = 1 WHERE DocumentId = @DocumentId";
        command.Parameters.AddWithValue("@DocumentId", documentId);

        await command.ExecuteNonQueryAsync();
    }
}
