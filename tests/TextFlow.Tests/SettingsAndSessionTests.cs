using System;
using System.IO;
using System.Threading.Tasks;
using TextFlow.Models;
using TextFlow.Services;
using Xunit;

namespace TextFlow.Tests;

public class SettingsAndSessionTests
{
    [Fact]
    public async Task SettingsService_SavesAndLoadsCorrectly()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            service.Settings.FontFamily = "Consolas";
            service.Settings.FontSize = 18;
            service.Settings.WordWrap = false;
            service.AddRecentFile("C:\\test\\file1.txt");

            await service.SaveSettingsAsync();

            var reloadedService = new SettingsService(tempFile);
            await reloadedService.LoadSettingsAsync();

            Assert.Equal("Consolas", reloadedService.Settings.FontFamily);
            Assert.Equal(18, reloadedService.Settings.FontSize);
            Assert.False(reloadedService.Settings.WordWrap);
            Assert.Single(reloadedService.Settings.RecentFiles);
            Assert.Equal("C:\\test\\file1.txt", reloadedService.Settings.RecentFiles[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SessionService_SavesAndRestoresWorkspaceState()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"session_{Guid.NewGuid():N}.json");
        try
        {
            var sessionService = new SessionService(tempFile);
            var session = new SessionModel
            {
                ActiveTabIndex = 1,
                ActiveFile = "C:\\docs\\notes.md",
                Theme = "Dark",
                FontFamily = "Cascadia Code",
                FontSize = 16
            };
            session.OpenedFiles.Add(new SessionDocumentState { FilePath = "C:\\docs\\todo.txt", Title = "todo.txt", LineNumber = 10 });
            session.OpenedFiles.Add(new SessionDocumentState { FilePath = "C:\\docs\\notes.md", Title = "notes.md", LineNumber = 25 });

            await sessionService.SaveSessionAsync(session);

            var loaded = await sessionService.LoadSessionAsync();
            Assert.NotNull(loaded);
            Assert.Equal(1, loaded.ActiveTabIndex);
            Assert.Equal(2, loaded.OpenedFiles.Count);
            Assert.Equal("C:\\docs\\notes.md", loaded.ActiveFile);
            Assert.Equal("Dark", loaded.Theme);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SessionService_SavesAndRestoresUnsavedAndEmptyWindows()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"session_multi_{Guid.NewGuid():N}.json");
        try
        {
            var sessionService = new SessionService(tempFile);
            var session = new SessionModel();

            // Window 1: Has unsaved note + empty tab
            var win1 = new SessionWindowState
            {
                Id = "win-1",
                ActiveTabIndex = 0
            };
            win1.Documents.Add(new SessionDocumentState
            {
                FilePath = null,
                Title = "Untitled 1",
                Content = "Unsaved draft content here",
                IsModified = true,
                LineNumber = 2,
                ColumnNumber = 5
            });
            win1.Documents.Add(new SessionDocumentState
            {
                FilePath = null,
                Title = "Untitled 2",
                Content = string.Empty, // empty window/tab with no data!
                IsModified = false,
                LineNumber = 1,
                ColumnNumber = 1
            });

            // Window 2: Completely empty window with no data
            var win2 = new SessionWindowState
            {
                Id = "win-2",
                ActiveTabIndex = 0
            };
            win2.Documents.Add(new SessionDocumentState
            {
                FilePath = null,
                Title = "Untitled",
                Content = string.Empty,
                IsModified = false
            });

            session.Windows.Add(win1);
            session.Windows.Add(win2);

            await sessionService.SaveSessionAsync(session);

            var reloaded = await sessionService.LoadSessionAsync();
            Assert.NotNull(reloaded);
            Assert.Equal(2, reloaded.Windows.Count);

            // Verify Window 1
            var reloadedWin1 = reloaded.Windows[0];
            Assert.Equal(2, reloadedWin1.Documents.Count);
            Assert.Null(reloadedWin1.Documents[0].FilePath);
            Assert.Equal("Unsaved draft content here", reloadedWin1.Documents[0].Content);
            Assert.True(reloadedWin1.Documents[0].IsModified);
            Assert.Null(reloadedWin1.Documents[1].FilePath);
            Assert.Equal(string.Empty, reloadedWin1.Documents[1].Content);
            Assert.False(reloadedWin1.Documents[1].IsModified);

            // Verify Window 2
            var reloadedWin2 = reloaded.Windows[1];
            Assert.Single(reloadedWin2.Documents);
            Assert.Null(reloadedWin2.Documents[0].FilePath);
            Assert.Equal(string.Empty, reloadedWin2.Documents[0].Content);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SessionService_SavesAndRestoresWindowPositionAndSize()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"session_pos_{Guid.NewGuid():N}.json");
        try
        {
            var sessionService = new SessionService(tempFile);
            var session = new SessionModel
            {
                WindowLeft = 150,
                WindowTop = 200,
                WindowWidth = 1440,
                WindowHeight = 900,
                IsMaximized = false
            };

            var winState = new SessionWindowState
            {
                Id = "win-pos-test",
                WindowLeft = 150,
                WindowTop = 200,
                WindowWidth = 1440,
                WindowHeight = 900,
                IsMaximized = false
            };
            session.Windows.Add(winState);

            await sessionService.SaveSessionAsync(session);

            var loaded = await sessionService.LoadSessionAsync();
            Assert.NotNull(loaded);
            Assert.Equal(150, loaded.WindowLeft);
            Assert.Equal(200, loaded.WindowTop);
            Assert.Equal(1440, loaded.WindowWidth);
            Assert.Equal(900, loaded.WindowHeight);
            Assert.False(loaded.IsMaximized);

            Assert.Single(loaded.Windows);
            Assert.Equal(150, loaded.Windows[0].WindowLeft);
            Assert.Equal(200, loaded.Windows[0].WindowTop);
            Assert.Equal(1440, loaded.Windows[0].WindowWidth);
            Assert.Equal(900, loaded.Windows[0].WindowHeight);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SettingsModel_Defaults_Consolas()
    {
        var settings = new SettingsModel();
        Assert.Equal("Consolas", settings.FontFamily);
    }
}

