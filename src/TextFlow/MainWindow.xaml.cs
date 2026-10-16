using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TextFlow.Infrastructure;
using TextFlow.Models;
using TextFlow.Services;
using TextFlow.Services.Interfaces;
using TextFlow.ViewModels;
using TextFlow.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace TextFlow;

public sealed partial class MainWindow : Window
{
    public string WindowId { get; } = Guid.NewGuid().ToString();
    public MainViewModel ViewModel { get; }
    private readonly IServiceProvider _serviceProvider;
    private readonly IDialogService _dialogService;
    private readonly IEditorService _editorService;
    private int? _lastNormalLeft;
    private int? _lastNormalTop;
    private int? _lastNormalWidth;
    private int? _lastNormalHeight;

    public MainWindow() : this(null)
    {
    }

    public MainWindow(SessionWindowState? initialWindowState)
    {
        _serviceProvider = ((App)Application.Current).Services;
        ViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        _dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        _editorService = _serviceProvider.GetRequiredService<IEditorService>();

        if (initialWindowState != null && !string.IsNullOrEmpty(initialWindowState.Id))
        {
            WindowId = initialWindowState.Id;
        }

        this.InitializeComponent();

        // Enable Mica backdrop
        SystemBackdrop = new MicaBackdrop();

        // Configure Modern Titlebar (no separate OS titlebar, content extends into title bar)
        ConfigureTitleBar();

        // Set application & taskbar icon
        SetWindowIcon();

        // Restore window position and size before showing
        ApplyInitialWindowState(initialWindowState);

        if (AppWindow != null)
        {
            AppWindow.Changed += OnAppWindowChanged;
        }

        // Initialize Window Handle for Dialogs / Pickers
        nint hwnd = WindowNative.GetWindowHandle(this);
        _dialogService.Initialize(hwnd);

        // Hook XamlRoot once loaded
        if (Content is FrameworkElement rootElement)
        {
            rootElement.Loaded += (s, e) =>
            {
                if (_dialogService is DialogService ds)
                {
                    ds.SetXamlRoot(rootElement.XamlRoot);
                }
            };
        }

        // Wire FindReplace selection highlight
        ViewModel.FindReplace.SelectTextRequested += (s, range) =>
        {
            MainEditorTextBox.Focus(FocusState.Programmatic);
            MainEditorTextBox.Select(range.Index, range.Length);
        };

        // Synchronize editor scrolling with line numbers
        MainEditorTextBox.Loaded += (s, e) => SynchronizeEditorScrolling();

        // Window closing event to save session
        this.Closed += OnWindowClosed;

        // Start initialization with initial window state if provided
        _ = ViewModel.InitializeWithStateAsync(initialWindowState);
    }

    public SessionWindowState CaptureWindowState()
    {
        var presenter = AppWindow?.Presenter as OverlappedPresenter;
        bool isMaximized = presenter?.State == OverlappedPresenterState.Maximized;

        double width = 1200;
        double height = 800;
        double? left = null;
        double? top = null;

        if (AppWindow != null)
        {
            if (isMaximized)
            {
                width = _lastNormalWidth ?? (AppWindow.Size.Width > 0 ? AppWindow.Size.Width : 1200);
                height = _lastNormalHeight ?? (AppWindow.Size.Height > 0 ? AppWindow.Size.Height : 800);
                left = _lastNormalLeft ?? AppWindow.Position.X;
                top = _lastNormalTop ?? AppWindow.Position.Y;
            }
            else
            {
                width = AppWindow.Size.Width > 0 ? AppWindow.Size.Width : 1200;
                height = AppWindow.Size.Height > 0 ? AppWindow.Size.Height : 800;
                left = AppWindow.Position.X;
                top = AppWindow.Position.Y;
            }
        }
        else if (Bounds.Width > 0)
        {
            width = Bounds.Width;
            height = Bounds.Height;
        }

        return ViewModel.CaptureWindowState(WindowId, width, height, left, top, isMaximized);
    }

    private void OnNewWindowClick(object sender, RoutedEventArgs e)
    {
        App.CreateNewWindow();
    }

    private async void OnWindowClosed(object sender, WindowEventArgs args)
    {
        App.OnMainWindowClosed(this);
        await ViewModel.ShutdownAsync();
    }

    // --- Tab Events ---

    private void OnAddTabButtonClick(TabView sender, object args)
    {
        ViewModel.CreateNewDocument();
    }

    private async void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is DocumentViewModel doc)
        {
            await ViewModel.CloseTabAsync(doc);
        }
    }

    private async void OnTabPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Middle click to close tab
        var pointerPoint = e.GetCurrentPoint(sender as UIElement);
        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            if (sender is FrameworkElement elem && elem.DataContext is DocumentViewModel doc)
            {
                e.Handled = true;
                await ViewModel.CloseTabAsync(doc);
            }
        }
    }

    private async void OnCloseCurrentTabClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.CloseTabAsync();
    }

    // --- Editor Events ---

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedDocument == null) return;

        int pos = MainEditorTextBox.SelectionStart;
        var (line, col) = _editorService.GetLineAndColumn(MainEditorTextBox.Text, pos);

        ViewModel.SelectedDocument.CursorPosition = pos;
        ViewModel.SelectedDocument.LineNumber = line;
        ViewModel.SelectedDocument.ColumnNumber = col;
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ViewModel.SelectedDocument != null)
        {
            int pos = MainEditorTextBox.SelectionStart;
            var (line, col) = _editorService.GetLineAndColumn(MainEditorTextBox.Text, pos);
            ViewModel.SelectedDocument.CursorPosition = pos;
            ViewModel.SelectedDocument.LineNumber = line;
            ViewModel.SelectedDocument.ColumnNumber = col;
            UpdateWindowTitle();
        }
    }

    private void UpdateWindowTitle()
    {
        string docTitle = ViewModel.SelectedDocument?.DisplayTitle ?? "TextFlow Pro";
        this.Title = $"{docTitle} - TextFlow Pro";
    }

    // --- Drag and Drop File Opening ---

    private void OnEditorDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }

    private async void OnEditorDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (item is Windows.Storage.StorageFile file)
                {
                    await ViewModel.OpenSpecificFileAsync(file.Path);
                }
            }
        }
    }

    // --- Menu Bar / Toolbar Actions ---

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        if (MainEditorTextBox.CanUndo) MainEditorTextBox.Undo();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        if (MainEditorTextBox.CanRedo) MainEditorTextBox.Redo();
    }

    private void OnCutClick(object sender, RoutedEventArgs e)
    {
        MainEditorTextBox.CutSelectionToClipboard();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        MainEditorTextBox.CopySelectionToClipboard();
    }

    private void OnPasteClick(object sender, RoutedEventArgs e)
    {
        MainEditorTextBox.PasteFromClipboard();
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        MainEditorTextBox.SelectAll();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        App.CloseAll();
    }

    // --- Theme Switchers ---

    private void OnSetSystemTheme(object sender, RoutedEventArgs e)
    {
        SetAppTheme(AppThemeMode.System);
    }

    private void OnSetLightTheme(object sender, RoutedEventArgs e)
    {
        SetAppTheme(AppThemeMode.Light);
    }

    private void OnSetDarkTheme(object sender, RoutedEventArgs e)
    {
        SetAppTheme(AppThemeMode.Dark);
    }

    private void SetAppTheme(AppThemeMode theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppThemeMode.Light => ElementTheme.Light,
                AppThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
        ViewModel.CurrentTheme = theme;
        UpdateTitleBarColors(theme);
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        settingsService.Settings.ThemeMode = theme;
        _ = settingsService.SaveSettingsAsync();
    }

    private void SetWindowIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow?.SetIcon(iconPath);
            }
        }
        catch
        {
        }
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow?.TitleBar != null)
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);

            UpdateTitleBarColors(ViewModel.CurrentTheme);

            AppTitleBar.SizeChanged += (s, e) => UpdateTitleBarPadding();
            if (Content is FrameworkElement fe)
            {
                fe.ActualThemeChanged += (s, e) => UpdateTitleBarColors(ViewModel.CurrentTheme);
                fe.Loaded += (s, e) =>
                {
                    UpdateTitleBarPadding();
                    UpdateTitleBarColors(ViewModel.CurrentTheme);
                };
            }
        }
    }

    private void UpdateTitleBarPadding()
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow?.TitleBar != null)
        {
            double scale = Content?.XamlRoot?.RasterizationScale ?? 1.0;
            if (scale <= 0) scale = 1.0;
            double rightInset = AppWindow.TitleBar.RightInset / scale;
            RightPaddingColumn.Width = new GridLength(Math.Max(rightInset, 140));
        }
    }

    private void UpdateTitleBarColors(AppThemeMode theme)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported() || AppWindow?.TitleBar == null) return;

        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);

        bool isDark = theme switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark
        };

        var fgColor = isDark
            ? Color.FromArgb(255, 240, 240, 240)
            : Color.FromArgb(255, 30, 30, 30);
        var hoverBg = isDark
            ? Color.FromArgb(30, 255, 255, 255)
            : Color.FromArgb(30, 0, 0, 0);
        var pressedBg = isDark
            ? Color.FromArgb(50, 255, 255, 255)
            : Color.FromArgb(50, 0, 0, 0);

        titleBar.ButtonForegroundColor = fgColor;
        titleBar.ButtonHoverForegroundColor = fgColor;
        titleBar.ButtonHoverBackgroundColor = hoverBg;
        titleBar.ButtonPressedForegroundColor = fgColor;
        titleBar.ButtonPressedBackgroundColor = pressedBg;
    }

    private ScrollViewer? _editorScrollViewer;

    private void SynchronizeEditorScrolling()
    {
        if (_editorScrollViewer != null) return;

        _editorScrollViewer = FindVisualChild<ScrollViewer>(MainEditorTextBox);
        if (_editorScrollViewer != null)
        {
            _editorScrollViewer.ViewChanged += (s, e) =>
            {
                LineNumbersScrollViewer.ChangeView(null, _editorScrollViewer.VerticalOffset, null, true);
            };
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }
            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // --- Dialogs ---

    private async void OnOpenSettingsDialog(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            var dialog = new SettingsDialog(ViewModel.SettingsVm)
            {
                XamlRoot = root.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void OnOpenRecoveryDialog(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            await ViewModel.RecoveryVm.LoadRecoverableFilesAsync();
            var dialog = new RecoveryDialog(ViewModel.RecoveryVm)
            {
                XamlRoot = root.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void OnOpenGoogleDriveDialog(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            var dialog = new GoogleDriveLoginDialog(ViewModel.GoogleDriveVm)
            {
                XamlRoot = root.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void OnOpenCompareDialog(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            ViewModel.CompareDocuments();
            var dialog = new DocumentCompareDialog(ViewModel.CompareVm)
            {
                XamlRoot = root.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        await _dialogService.ShowMessageAsync(
            "About TextFlow Pro",
            "TextFlow Pro v1.0.0\n" +
            "A modern, high-performance text editor for Windows 11.\n\n" +
            "Built with WinUI 3, .NET 8, MVVM Architecture, SQLite, and Google Drive Cloud Sync.\n" +
            "Created following Windows 11 Fluent Design principles."
        );
    }

    // --- Window Position, Size & Current Line Highlight Helpers ---

    private void ApplyInitialWindowState(SessionWindowState? state)
    {
        if (state == null || AppWindow == null) return;

        try
        {
            int w = (int)(state.WindowWidth >= 300 ? state.WindowWidth : 1200);
            int h = (int)(state.WindowHeight >= 200 ? state.WindowHeight : 800);

            _lastNormalWidth = w;
            _lastNormalHeight = h;

            if (state.WindowLeft.HasValue && state.WindowTop.HasValue)
            {
                int x = (int)state.WindowLeft.Value;
                int y = (int)state.WindowTop.Value;
                _lastNormalLeft = x;
                _lastNormalTop = y;

                // Move and resize window within reasonable screen coordinate bounds
                if (x >= -200 && y >= -200)
                {
                    AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
                }
                else
                {
                    AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
                }
            }
            else
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(w, h));
            }

            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (state.IsMaximized && presenter != null)
            {
                presenter.Maximize();
            }
        }
        catch
        {
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            var presenter = sender.Presenter as OverlappedPresenter;
            if (presenter != null && presenter.State == OverlappedPresenterState.Restored)
            {
                _lastNormalLeft = sender.Position.X;
                _lastNormalTop = sender.Position.Y;
                _lastNormalWidth = sender.Size.Width;
                _lastNormalHeight = sender.Size.Height;
            }
        }
    }

    // --- Static Formatters for XAML x:Bind ---

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static TextWrapping BoolToWrap(bool wordWrap) => wordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public static FontWeight GetFontWeight(string style) => style.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal;

    public static Windows.UI.Text.FontStyle GetFontStyle(string style) => style.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal;

    public static string FormatCursor(int line, int col) => $"Ln {line}, Col {col}";

    public static string FormatZoom(double size) => $"{Math.Round(size / 14.0 * 100)}%";
}
