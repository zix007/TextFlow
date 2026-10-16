using Microsoft.UI.Xaml.Controls;
using TextFlow.ViewModels;

namespace TextFlow.Views;

public sealed partial class RecoveryDialog : ContentDialog
{
    public RecoveryViewModel ViewModel { get; }

    public RecoveryDialog(RecoveryViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    private async void OnRestoreAllClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        await ViewModel.RestoreAllAsync();
    }

    private async void OnDiscardAllClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        await ViewModel.DiscardAllAsync();
    }
}
