using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class DialogService : IDialogService
{
    private nint _hwnd;
    private XamlRoot? _xamlRoot;

    public void Initialize(nint windowHandle)
    {
        _hwnd = windowHandle;
    }

    public void SetXamlRoot(XamlRoot root)
    {
        _xamlRoot = root;
    }

    public async Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*")
    {
        var picker = new FileOpenPicker();
        if (_hwnd != nint.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
        }

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".sql");
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<List<string>> ShowOpenMultipleFilesDialogAsync()
    {
        var picker = new FileOpenPicker();
        if (_hwnd != nint.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
        }

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".sql");
        picker.FileTypeFilter.Add("*");

        var files = await picker.PickMultipleFilesAsync();
        return files?.Select(f => f.Path).ToList() ?? new List<string>();
    }

    public async Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string defaultExtension = ".txt")
    {
        var picker = new FileSavePicker();
        if (_hwnd != nint.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
        }

        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = defaultFileName;

        if (defaultExtension == ".json")
            picker.FileTypeChoices.Add("JSON File", new List<string> { ".json" });
        else if (defaultExtension == ".csv")
            picker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });
        else if (defaultExtension == ".sql")
            picker.FileTypeChoices.Add("SQL File", new List<string> { ".sql" });
        else
            picker.FileTypeChoices.Add("Text Document", new List<string> { ".txt" });

        picker.FileTypeChoices.Add("All Files", new List<string> { "." });

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string primaryButton = "Yes", string closeButton = "No")
    {
        if (_xamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButton,
            CloseButtonText = closeButton,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = _xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task<int> ShowSavePromptAsync(string fileName)
    {
        if (_xamlRoot == null) return 1; // Don't save fallback

        var dialog = new ContentDialog
        {
            Title = "Save changes?",
            Content = $"Do you want to save changes to \"{fileName}\"?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = _xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => 0,
            ContentDialogResult.Secondary => 1,
            _ => 2
        };
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        if (_xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = _xamlRoot
        };

        await dialog.ShowAsync();
    }
}
