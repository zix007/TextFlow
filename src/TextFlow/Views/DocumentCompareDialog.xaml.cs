using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TextFlow.ViewModels;
using Windows.UI;

namespace TextFlow.Views;

public sealed partial class DocumentCompareDialog : ContentDialog
{
    public DocumentCompareViewModel ViewModel { get; }

    public DocumentCompareDialog(DocumentCompareViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }

    public static string FormatStats(int added, int removed, int modified)
    {
        return $"Changes: +{added} added, -{removed} removed, ~{modified} modified";
    }

    public static Brush DiffBackground(string diffType)
    {
        return diffType switch
        {
            "Added" => new SolidColorBrush(Color.FromArgb(40, 0, 200, 0)),
            "Removed" => new SolidColorBrush(Color.FromArgb(40, 220, 0, 0)),
            "Modified" => new SolidColorBrush(Color.FromArgb(40, 220, 180, 0)),
            _ => new SolidColorBrush(Colors.Transparent)
        };
    }
}
