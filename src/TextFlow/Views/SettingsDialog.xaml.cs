using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextFlow.Infrastructure;
using TextFlow.ViewModels;

namespace TextFlow.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    public SettingsViewModel ViewModel { get; }

    public SettingsDialog(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    private async void OnSaveClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        await ViewModel.SaveAsync();
    }

    public static int ThemeToIndex(AppThemeMode theme) => (int)theme;

    public static string ConnectButtonText(bool isConnected) => isConnected ? "Disconnect" : "Connect Google Drive";
}
