using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class FileService : IFileService
{
    public async Task<DocumentModel> OpenFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }

        // Detect encoding
        Encoding encoding = FileEncodingDetector.DetectEncoding(filePath, out bool hasBom);

        string content;
        using (var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true))
        {
            content = await reader.ReadToEndAsync();
            // In case StreamReader determined actual encoding from preamble
            encoding = reader.CurrentEncoding;
        }

        var doc = new DocumentModel
        {
            FilePath = filePath,
            Title = Path.GetFileName(filePath),
            Content = content,
            FileEncoding = encoding,
            HasBom = hasBom,
            IsModified = false,
            LastModifiedTime = File.GetLastWriteTimeUtc(filePath),
            LastSavedTime = File.GetLastWriteTimeUtc(filePath)
        };

        return doc;
    }

    public async Task SaveFileAsync(DocumentModel document, string? customPath = null, Encoding? customEncoding = null)
    {
        string targetPath = customPath ?? document.FilePath ?? throw new InvalidOperationException("No file path specified for saving.");
        Encoding targetEncoding = customEncoding ?? document.FileEncoding;

        // Ensure directory exists
        string? dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Write file atomically via temp file or direct stream
        using (var writer = new StreamWriter(targetPath, false, targetEncoding))
        {
            await writer.WriteAsync(document.Content);
        }

        document.FilePath = targetPath;
        document.Title = Path.GetFileName(targetPath);
        document.FileEncoding = targetEncoding;
        document.IsModified = false;
        document.LastSavedTime = DateTime.UtcNow;
        document.LastModifiedTime = DateTime.UtcNow;
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public string GetFileExtension(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant();
    }

    public string GetFileName(string filePath)
    {
        return Path.GetFileName(filePath);
    }
}
