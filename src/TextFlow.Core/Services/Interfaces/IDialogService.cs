using System.Collections.Generic;
using System.Threading.Tasks;

namespace TextFlow.Services.Interfaces;

public interface IDialogService
{
    void Initialize(nint windowHandle);
    Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*");
    Task<List<string>> ShowOpenMultipleFilesDialogAsync();
    Task<string?> ShowSaveFileDialogAsync(string defaultFileName, string defaultExtension = ".txt");
    Task<bool> ShowConfirmationAsync(string title, string message, string primaryButton = "Yes", string closeButton = "No");
    Task<int> ShowSavePromptAsync(string fileName); // 0: Save, 1: Don't Save, 2: Cancel
    Task ShowMessageAsync(string title, string message);
}
