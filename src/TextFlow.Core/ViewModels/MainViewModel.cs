using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Services;
using TextFlow.Services.Interfaces;

namespace TextFlow.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly ISessionService _sessionService;
    private readonly IAutoSaveService _autoSaveService;
    private readonly IRecoveryService _recoveryService;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IDialogService _dialogService;
    private readonly IAIService _aiService;

    [ObservableProperty]
    private ObservableCollection<DocumentViewModel> _documents = new();

    [ObservableProperty]
    private DocumentViewModel? _selectedDocument;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private FindReplaceViewModel _findReplace;

    [ObservableProperty]
    private SettingsViewModel _settingsVm;

    [ObservableProperty]
    private RecoveryViewModel _recoveryVm;

    [ObservableProperty]
    private GoogleDriveViewModel _googleDriveVm;

    [ObservableProperty]
    private DocumentCompareViewModel _compareVm;

    // View Options bound to Settings
    [ObservableProperty]
    private bool _wordWrap;

    [ObservableProperty]
    private bool _lineNumbers;

    [ObservableProperty]
    private string _fontFamily = "Consolas";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private string _fontStyle = "Regular";

    [ObservableProperty]
    private AppThemeMode _currentTheme = AppThemeMode.System;

    [ObservableProperty]
    private string _globalStatusText = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public MainViewModel(
        IFileService fileService,
        IEditorService editorService,
        ISettingsService settingsService,
        ISessionService sessionService,
        IAutoSaveService autoSaveService,
        IRecoveryService recoveryService,
        IGoogleDriveService googleDriveService,
        IDialogService dialogService,
        IAIService aiService)
    {
        _fileService = fileService;
        _settingsService = settingsService;
        _sessionService = sessionService;
        _autoSaveService = autoSaveService;
        _recoveryService = recoveryService;
        _googleDriveService = googleDriveService;
        _dialogService = dialogService;
        _aiService = aiService;

        _findReplace = new FindReplaceViewModel(editorService);
        _settingsVm = new SettingsViewModel(settingsService, googleDriveService);
        _recoveryVm = new RecoveryViewModel(recoveryService);
        _googleDriveVm = new GoogleDriveViewModel(googleDriveService, settingsService);
        _compareVm = new DocumentCompareViewModel();

        ApplySettingsToView(_settingsService.Settings);
        _settingsService.SettingsChanged += (s, settings) => ApplySettingsToView(settings);

        _googleDriveService.SyncStatusChanged += OnSyncStatusChanged;
        _recoveryVm.DocumentRestored += OnDocumentRestored;

        if (_autoSaveService is AutoSaveService autoSaveImpl)
        {
            autoSaveImpl.RegisterDocumentProvider(() => Documents.Select(d =>
            {
                d.SyncToModel();
                return d.Model;
            }));
        }
    }

    private void ApplySettingsToView(SettingsModel settings)
    {
        WordWrap = settings.WordWrap;
        LineNumbers = settings.LineNumbers;
        FontFamily = string.Equals(settings.FontFamily, "Cascadia Code", StringComparison.OrdinalIgnoreCase) ? "Consolas" : settings.FontFamily;
        FontSize = settings.FontSize;
        FontStyle = settings.FontStyle;
        CurrentTheme = settings.ThemeMode;

        _autoSaveService.Start(settings.AutoSaveIntervalSeconds);
    }

    private void OnSyncStatusChanged(object? sender, SyncStatus status)
    {
        if (SelectedDocument != null)
        {
            SelectedDocument.SyncStatus = status;
        }
    }

    private void OnDocumentRestored(object? sender, DocumentModel model)
    {
        var vm = new DocumentViewModel(model);
        Documents.Add(vm);
        SelectedDocument = vm;
    }

    public async Task InitializeAsync()
    {
        await InitializeWithStateAsync(null);
    }

    public async Task InitializeWithStateAsync(SessionWindowState? initialState)
    {
        await _settingsService.LoadSettingsAsync();

        // Check crash recovery
        var recoverable = await _recoveryService.GetRecoverableFilesAsync();
        if (recoverable.Count > 0)
        {
            await RecoveryVm.LoadRecoverableFilesAsync();
        }

        // 1. If explicit window state passed (from multi-window restore)
        if (initialState != null && initialState.Documents.Count > 0)
        {
            await RestoreDocumentsAsync(initialState.Documents, initialState.ActiveTabIndex);
            return;
        }

        // 2. Restore Session from ISessionService if enabled
        if (_settingsService.Settings.StartupBehavior == "RestoreSession")
        {
            var session = await _sessionService.LoadSessionAsync();
            if (session != null)
            {
                if (session.Windows.Count > 0 && session.Windows[0].Documents.Count > 0)
                {
                    await RestoreDocumentsAsync(session.Windows[0].Documents, session.Windows[0].ActiveTabIndex);
                    return;
                }
                else if (session.OpenedFiles.Count > 0)
                {
                    await RestoreDocumentsAsync(session.OpenedFiles, session.ActiveTabIndex);
                    return;
                }
            }
        }

        // Default: Open one empty document
        CreateNewDocument();
    }

    public async Task RestoreDocumentsAsync(IEnumerable<SessionDocumentState> docStates, int activeTabIndex = 0)
    {
        Documents.Clear();

        foreach (var docState in docStates)
        {
            DocumentModel doc;
            if (!string.IsNullOrEmpty(docState.FilePath) && _fileService.FileExists(docState.FilePath))
            {
                try
                {
                    doc = await _fileService.OpenFileAsync(docState.FilePath);
                    // If document had unsaved modifications before close, restore them
                    if (docState.IsModified && docState.Content != null)
                    {
                        doc.Content = docState.Content;
                        doc.IsModified = true;
                    }
                }
                catch
                {
                    doc = new DocumentModel
                    {
                        FilePath = docState.FilePath,
                        Title = docState.Title ?? "Untitled",
                        Content = docState.Content ?? string.Empty,
                        IsModified = docState.IsModified
                    };
                }
            }
            else
            {
                // Document without file path or with no data/empty
                doc = new DocumentModel
                {
                    FilePath = docState.FilePath,
                    Title = string.IsNullOrEmpty(docState.Title) ? "Untitled" : docState.Title,
                    Content = docState.Content ?? string.Empty,
                    IsModified = docState.IsModified
                };

                if (!string.IsNullOrEmpty(docState.EncodingName))
                {
                    try
                    {
                        doc.FileEncoding = System.Text.Encoding.GetEncoding(docState.EncodingName);
                    }
                    catch { }
                }
            }

            doc.CursorPosition = docState.CursorPosition;
            doc.LineNumber = Math.Max(1, docState.LineNumber);
            doc.ColumnNumber = Math.Max(1, docState.ColumnNumber);
            doc.ZoomLevel = docState.ZoomLevel > 0 ? docState.ZoomLevel : 1.0;

            var docVm = new DocumentViewModel(doc);
            Documents.Add(docVm);
        }

        if (Documents.Count == 0)
        {
            CreateNewDocument();
        }
        else
        {
            int index = Math.Clamp(activeTabIndex, 0, Documents.Count - 1);
            SelectedDocument = Documents[index];
        }
    }

    public SessionWindowState CaptureWindowState(string windowId = "", double width = 1200, double height = 800, double? left = null, double? top = null, bool isMaximized = false)
    {
        var windowState = new SessionWindowState
        {
            Id = string.IsNullOrEmpty(windowId) ? Guid.NewGuid().ToString() : windowId,
            ActiveTabIndex = SelectedTabIndex,
            WindowWidth = width,
            WindowHeight = height,
            WindowLeft = left,
            WindowTop = top,
            IsMaximized = isMaximized
        };

        foreach (var doc in Documents)
        {
            doc.SyncToModel();
            windowState.Documents.Add(new SessionDocumentState
            {
                FilePath = doc.FilePath,
                Title = doc.Title,
                Content = doc.Content ?? string.Empty,
                EncodingName = doc.FileEncoding?.WebName ?? "UTF-8",
                CursorPosition = doc.CursorPosition,
                LineNumber = doc.LineNumber,
                ColumnNumber = doc.ColumnNumber,
                ZoomLevel = doc.ZoomLevel,
                IsModified = doc.IsModified
            });
        }

        return windowState;
    }

    public async Task ShutdownAsync()
    {
        _autoSaveService.Stop();

        // Save session
        var session = new SessionModel
        {
            ActiveTabIndex = SelectedTabIndex,
            ActiveFile = SelectedDocument?.FilePath,
            Theme = CurrentTheme.ToString(),
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontStyle = FontStyle,
            WordWrap = WordWrap,
            LineNumbers = LineNumbers,
            IsGoogleDriveConnected = _googleDriveService.IsAuthenticated
        };

        var currentWindowState = CaptureWindowState();
        session.Windows.Add(currentWindowState);
        session.OpenedFiles = currentWindowState.Documents.ToList();

        await _sessionService.SaveSessionAsync(session);

        // Auto-save modified documents for crash safety
        await _autoSaveService.AutoSaveAllAsync(Documents.Select(d => d.Model));

        // Sync to cloud if enabled
        if (_settingsService.Settings.AutoSyncEnabled && _googleDriveService.IsAuthenticated)
        {
            await _googleDriveService.SyncAllOpenDocumentsAsync(Documents.Select(d => d.Model), _settingsService.Settings.GoogleSyncFolder);
        }
    }

    partial void OnSelectedDocumentChanged(DocumentViewModel? value)
    {
        FindReplace.AttachDocument(value);
    }

    // --- Commands ---

    [RelayCommand]
    public void CreateNewDocument()
    {
        int untitledIndex = Documents.Count(d => d.Title.StartsWith("Untitled")) + 1;
        var model = DocumentModel.CreateNew($"Untitled {untitledIndex}");
        var vm = new DocumentViewModel(model);
        Documents.Add(vm);
        SelectedDocument = vm;
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        var filePath = await _dialogService.ShowOpenFileDialogAsync();
        if (!string.IsNullOrEmpty(filePath))
        {
            await OpenSpecificFileAsync(filePath);
        }
    }

    public async Task OpenSpecificFileAsync(string filePath)
    {
        // Check if already open
        var existing = Documents.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SelectedDocument = existing;
            return;
        }

        try
        {
            IsBusy = true;
            GlobalStatusText = $"Opening {Path.GetFileName(filePath)}...";
            var doc = await _fileService.OpenFileAsync(filePath);
            var vm = new DocumentViewModel(doc);

            // Replace single clean untitled tab if pristine
            if (Documents.Count == 1 && string.IsNullOrEmpty(Documents[0].FilePath) && !Documents[0].IsModified && string.IsNullOrEmpty(Documents[0].Content))
            {
                Documents[0] = vm;
            }
            else
            {
                Documents.Add(vm);
            }

            SelectedDocument = vm;
            _settingsService.AddRecentFile(filePath);
            GlobalStatusText = "Ready";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error Opening File", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveFileAsync()
    {
        if (SelectedDocument == null) return;

        if (string.IsNullOrEmpty(SelectedDocument.FilePath))
        {
            await SaveAsFileAsync();
            return;
        }

        try
        {
            SelectedDocument.SyncToModel();
            await _fileService.SaveFileAsync(SelectedDocument.Model);
            SelectedDocument.IsModified = false;
            await _autoSaveService.ClearAutoSaveAsync(SelectedDocument.Id);
            _settingsService.AddRecentFile(SelectedDocument.FilePath);

            if (_settingsService.Settings.AutoSyncEnabled && _googleDriveService.IsAuthenticated)
            {
                _ = _googleDriveService.UploadFileAsync(SelectedDocument.Model, _settingsService.Settings.GoogleSyncFolder);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error Saving File", ex.Message);
        }
    }

    [RelayCommand]
    public async Task SaveAsFileAsync()
    {
        if (SelectedDocument == null) return;

        string defaultName = !string.IsNullOrEmpty(SelectedDocument.FilePath)
            ? Path.GetFileName(SelectedDocument.FilePath)
            : GetSafeFileName(SelectedDocument.DisplayTitle);
        string defaultExt = SelectedDocument.FileExtension;

        var savePath = await _dialogService.ShowSaveFileDialogAsync(defaultName, defaultExt);
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                SelectedDocument.SyncToModel();
                await _fileService.SaveFileAsync(SelectedDocument.Model, savePath);
                SelectedDocument.FilePath = savePath;
                SelectedDocument.Title = Path.GetFileName(savePath);
                SelectedDocument.IsModified = false;
                await _autoSaveService.ClearAutoSaveAsync(SelectedDocument.Id);
                _settingsService.AddRecentFile(savePath);

                if (_settingsService.Settings.AutoSyncEnabled && _googleDriveService.IsAuthenticated)
                {
                    _ = _googleDriveService.UploadFileAsync(SelectedDocument.Model, _settingsService.Settings.GoogleSyncFolder);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Error Saving File", ex.Message);
            }
        }
    }

    [RelayCommand]
    public async Task SaveAllFilesAsync()
    {
        foreach (var doc in Documents.Where(d => d.IsModified))
        {
            if (string.IsNullOrEmpty(doc.FilePath))
            {
                SelectedDocument = doc;
                await SaveAsFileAsync();
            }
            else
            {
                doc.SyncToModel();
                await _fileService.SaveFileAsync(doc.Model);
                doc.IsModified = false;
                await _autoSaveService.ClearAutoSaveAsync(doc.Id);
            }
        }
    }

    [RelayCommand]
    public async Task<bool> CloseTabAsync(DocumentViewModel? doc = null)
    {
        var targetDoc = doc ?? SelectedDocument;
        if (targetDoc == null) return true;

        if (targetDoc.IsModified)
        {
            int choice = await _dialogService.ShowSavePromptAsync(targetDoc.DisplayTitle);
            if (choice == 0) // Save
            {
                SelectedDocument = targetDoc;
                if (string.IsNullOrEmpty(targetDoc.FilePath))
                {
                    await SaveAsFileAsync();
                    if (targetDoc.IsModified) return false; // user cancelled save dialog
                }
                else
                {
                    await SaveFileAsync();
                }
            }
            else if (choice == 2) // Cancel
            {
                return false;
            }
        }

        await _autoSaveService.ClearAutoSaveAsync(targetDoc.Id);
        int index = Documents.IndexOf(targetDoc);
        Documents.Remove(targetDoc);

        if (Documents.Count == 0)
        {
            CreateNewDocument();
        }
        else
        {
            SelectedDocument = Documents[Math.Clamp(index, 0, Documents.Count - 1)];
        }

        return true;
    }

    [RelayCommand]
    public void NextTab()
    {
        if (Documents.Count <= 1) return;
        int nextIndex = (SelectedTabIndex + 1) % Documents.Count;
        SelectedDocument = Documents[nextIndex];
    }

    [RelayCommand]
    public void PreviousTab()
    {
        if (Documents.Count <= 1) return;
        int prevIndex = (SelectedTabIndex - 1 + Documents.Count) % Documents.Count;
        SelectedDocument = Documents[prevIndex];
    }

    [RelayCommand]
    public void OpenFind()
    {
        FindReplace.IsOpen = true;
        FindReplace.IsReplaceMode = false;
        FindReplace.AttachDocument(SelectedDocument);
    }

    [RelayCommand]
    public void OpenReplace()
    {
        FindReplace.IsOpen = true;
        FindReplace.IsReplaceMode = true;
        FindReplace.AttachDocument(SelectedDocument);
    }

    [RelayCommand]
    public async Task SyncCloudAsync()
    {
        if (!_googleDriveService.IsAuthenticated)
        {
            await _dialogService.ShowMessageAsync("Google Drive", "Google Drive is not configured. Please sign in via Settings or Tools > Google Drive.");
            return;
        }

        IsBusy = true;
        GlobalStatusText = "Synchronizing with Google Drive...";
        await _googleDriveService.SyncAllOpenDocumentsAsync(Documents.Select(d => d.Model), _settingsService.Settings.GoogleSyncFolder);
        IsBusy = false;
        GlobalStatusText = "Sync complete";
    }

    [RelayCommand]
    public void ToggleWordWrap()
    {
        WordWrap = !WordWrap;
        _settingsService.Settings.WordWrap = WordWrap;
        _ = _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    public void ToggleLineNumbers()
    {
        LineNumbers = !LineNumbers;
        _settingsService.Settings.LineNumbers = LineNumbers;
        _ = _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    public void ZoomIn()
    {
        if (FontSize < 72)
        {
            FontSize = Math.Min(72, FontSize + 2);
            _settingsService.Settings.FontSize = FontSize;
            _ = _settingsService.SaveSettingsAsync();
        }
    }

    [RelayCommand]
    public void ZoomOut()
    {
        if (FontSize > 8)
        {
            FontSize = Math.Max(8, FontSize - 2);
            _settingsService.Settings.FontSize = FontSize;
            _ = _settingsService.SaveSettingsAsync();
        }
    }

    [RelayCommand]
    public void ResetZoom()
    {
        FontSize = 14;
        _settingsService.Settings.FontSize = FontSize;
        _ = _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    public void CompareDocuments()
    {
        if (Documents.Count >= 2)
        {
            CompareVm.Compare(Documents[0].Title, Documents[0].Content, Documents[1].Title, Documents[1].Content);
        }
        else if (SelectedDocument != null)
        {
            CompareVm.Compare(SelectedDocument.Title, SelectedDocument.Content, "Empty Document", "");
        }
    }

    // --- AI Extension Features ---

    [RelayCommand]
    public async Task SummarizeDocumentWithAiAsync()
    {
        if (SelectedDocument == null || string.IsNullOrWhiteSpace(SelectedDocument.Content)) return;

        IsBusy = true;
        GlobalStatusText = "AI is summarizing document...";
        var summary = await _aiService.SummarizeTextAsync(SelectedDocument.Content);
        IsBusy = false;
        GlobalStatusText = "Ready";

        await _dialogService.ShowMessageAsync("TextFlow AI Summary", summary);
    }

    [RelayCommand]
    public async Task ExplainSqlWithAiAsync()
    {
        if (SelectedDocument == null || string.IsNullOrWhiteSpace(SelectedDocument.Content)) return;

        IsBusy = true;
        GlobalStatusText = "AI is analyzing SQL...";
        var explanation = await _aiService.ExplainSqlAsync(SelectedDocument.Content);
        IsBusy = false;
        GlobalStatusText = "Ready";

        await _dialogService.ShowMessageAsync("TextFlow AI SQL Explanation", explanation);
    }

    private static string GetSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Untitled";
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Where(c => !invalid.Contains(c) && c != '…').ToArray()).Trim();
        return string.IsNullOrEmpty(clean) ? "Untitled" : clean;
    }
}
