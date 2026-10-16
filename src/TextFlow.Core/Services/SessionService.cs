using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class SessionService : ISessionService
{
    private readonly string _sessionFilePath;

    public SessionService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDir = Path.Combine(localAppData, "TextFlowPro");
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }
        _sessionFilePath = Path.Combine(appDir, "session.json");
    }

    public SessionService(string customSessionPath)
    {
        _sessionFilePath = customSessionPath;
    }

    public async Task SaveSessionAsync(SessionModel session)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_sessionFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(session, options);
            await File.WriteAllTextAsync(_sessionFilePath, json);
        }
        catch
        {
            // Silently fail or log
        }
    }

    public async Task<SessionModel?> LoadSessionAsync()
    {
        try
        {
            if (!File.Exists(_sessionFilePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(_sessionFilePath);
            return JsonSerializer.Deserialize<SessionModel>(json);
        }
        catch
        {
            return null;
        }
    }

    public Task ClearSessionAsync()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }
        catch
        {
        }
        return Task.CompletedTask;
    }
}
