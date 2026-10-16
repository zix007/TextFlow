using System;
using System.Collections.Generic;

namespace TextFlow.Models;

public class SessionDocumentState
{
    public string? FilePath { get; set; }
    public string Title { get; set; } = "Untitled";
    public string? Content { get; set; } = string.Empty;
    public string EncodingName { get; set; } = "UTF-8";
    public int CursorPosition { get; set; }
    public int LineNumber { get; set; } = 1;
    public int ColumnNumber { get; set; } = 1;
    public double ZoomLevel { get; set; } = 1.0;
    public bool IsModified { get; set; }
    public string? TempAutoSavePath { get; set; }
}

public class SessionWindowState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<SessionDocumentState> Documents { get; set; } = new();
    public int ActiveTabIndex { get; set; } = 0;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsMaximized { get; set; }
}

public class SessionModel
{
    public List<SessionWindowState> Windows { get; set; } = new();
    public List<SessionDocumentState> OpenedFiles { get; set; } = new();
    public string? ActiveFile { get; set; }
    public int ActiveTabIndex { get; set; } = 0;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsMaximized { get; set; }
    public string Theme { get; set; } = "System";
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 14;
    public string FontStyle { get; set; } = "Regular";
    public bool WordWrap { get; set; } = true;
    public bool LineNumbers { get; set; } = true;
    public bool IsGoogleDriveConnected { get; set; }
}

