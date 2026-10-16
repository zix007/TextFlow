using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
using TextFlow.Persistence;
using TextFlow.Services;
using TextFlow.Services.Interfaces;
using TextFlow.ViewModels;

namespace TextFlow;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeLogging();
        this.UnhandledException += (s, e) =>
        {
            string crashPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextFlow_Crash.txt");
            File.WriteAllText(crashPath, e.Exception?.ToString() ?? e.Message);
        };
        Services = ConfigureServices();
        InitializeComponent();
    }

    private static void InitializeLogging()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logDir = Path.Combine(localAppData, "TextFlowPro", "logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        string logFile = Path.Combine(logDir, "textflow-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("TextFlow Pro application initialized.");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Persistence
        services.AddSingleton<DatabaseService>();

        // Services
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAutoSaveService, AutoSaveService>();
        services.AddSingleton<IRecoveryService, RecoveryService>();
        services.AddSingleton<ISyncQueueService, SyncQueueService>();
        services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
        services.AddSingleton<IEditorService, EditorService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IAIService, AIService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<RecoveryViewModel>();
        services.AddTransient<GoogleDriveViewModel>();
        services.AddTransient<FindReplaceViewModel>();
        services.AddTransient<DocumentCompareViewModel>();

        return services.BuildServiceProvider();
    }

    public static List<MainWindow> ActiveWindows { get; } = new();
    private static readonly System.Collections.Generic.Dictionary<string, TextFlow.Models.SessionWindowState> _persistentWindowStates = new();

    public static MainWindow CreateNewWindow(TextFlow.Models.SessionWindowState? initialWindowState = null)
    {
        var window = new MainWindow(initialWindowState);
        ActiveWindows.Add(window);
        window.Activate();
        return window;
    }

    public static void OnMainWindowClosed(MainWindow window)
    {
        try
        {
            var state = window.CaptureWindowState();
            _persistentWindowStates[window.WindowId] = state;
            ActiveWindows.Remove(window);

            // Persist all windows
            _ = SaveAllSessionsAsync();
        }
        catch
        {
        }
    }

    public static async Task SaveAllSessionsAsync()
    {
        if (Current is not App app) return;
        var sessionService = app.Services.GetRequiredService<ISessionService>();
        var settingsService = app.Services.GetRequiredService<ISettingsService>();

        bool restoreSession = settingsService.Settings.StartupBehavior == "RestoreSession";

        // Snapshot all currently active windows
        foreach (var window in ActiveWindows.ToList())
        {
            try
            {
                var state = window.CaptureWindowState();
                _persistentWindowStates[window.WindowId] = state;
            }
            catch { }
        }

        if (_persistentWindowStates.Count == 0) return;

        var session = new TextFlow.Models.SessionModel
        {
            Windows = _persistentWindowStates.Values.ToList()
        };

        if (session.Windows.Count > 0)
        {
            session.OpenedFiles = restoreSession ? session.Windows[0].Documents : new();
            session.ActiveTabIndex = session.Windows[0].ActiveTabIndex;
            session.WindowWidth = session.Windows[0].WindowWidth;
            session.WindowHeight = session.Windows[0].WindowHeight;
            session.WindowLeft = session.Windows[0].WindowLeft;
            session.WindowTop = session.Windows[0].WindowTop;
            session.IsMaximized = session.Windows[0].IsMaximized;
        }

        if (!restoreSession)
        {
            foreach (var win in session.Windows)
            {
                win.Documents = new();
            }
        }

        await sessionService.SaveSessionAsync(session);
    }

    public static void CloseAll()
    {
        var windows = ActiveWindows.ToList();
        foreach (var win in windows)
        {
            win.Close();
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var sessionService = Services.GetRequiredService<ISessionService>();

            await settingsService.LoadSettingsAsync();

            TextFlow.Models.SessionModel? session = await sessionService.LoadSessionAsync();
            if (settingsService.Settings.StartupBehavior != "RestoreSession" && session != null)
            {
                session.OpenedFiles.Clear();
                foreach (var w in session.Windows)
                {
                    w.Documents.Clear();
                }
            }

            if (session != null && session.Windows.Count > 0)
            {
                foreach (var winState in session.Windows)
                {
                    _persistentWindowStates[winState.Id] = winState;
                    CreateNewWindow(winState);
                }
            }
            else if (session != null && (session.WindowWidth > 0 || session.OpenedFiles.Count > 0))
            {
                var legacyState = new TextFlow.Models.SessionWindowState
                {
                    Documents = session.OpenedFiles,
                    ActiveTabIndex = session.ActiveTabIndex,
                    WindowWidth = session.WindowWidth,
                    WindowHeight = session.WindowHeight,
                    WindowLeft = session.WindowLeft,
                    WindowTop = session.WindowTop,
                    IsMaximized = session.IsMaximized
                };
                _persistentWindowStates[legacyState.Id] = legacyState;
                CreateNewWindow(legacyState);
            }
            else
            {
                CreateNewWindow(null);
            }
        }
        catch (Exception ex)
        {
            string crashPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TextFlow_Crash.txt");
            File.WriteAllText(crashPath, ex.ToString());
            throw;
        }
    }
}
