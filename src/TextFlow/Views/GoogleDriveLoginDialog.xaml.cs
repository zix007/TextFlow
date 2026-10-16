using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TextFlow.ViewModels;

namespace TextFlow.Views;

public sealed partial class GoogleDriveLoginDialog : ContentDialog
{
    public GoogleDriveViewModel ViewModel { get; }

    public GoogleDriveLoginDialog(GoogleDriveViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    public Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BoolToNegatedVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
}
