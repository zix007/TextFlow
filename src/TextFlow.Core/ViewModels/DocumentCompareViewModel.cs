using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TextFlow.ViewModels;

public class DiffLine
{
    public int OldLineNumber { get; set; }
    public int NewLineNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public string DiffType { get; set; } = "Unchanged";

    public DiffLine() { }

    public DiffLine(int oldLine, int newLine, string text, string diffType)
    {
        OldLineNumber = oldLine;
        NewLineNumber = newLine;
        Text = text;
        DiffType = diffType;
    }
}

public partial class DocumentCompareViewModel : ObservableObject
{
    [ObservableProperty]
    private string _leftTitle = "Document A";

    [ObservableProperty]
    private string _rightTitle = "Document B";

    [ObservableProperty]
    private string _leftContent = string.Empty;

    [ObservableProperty]
    private string _rightContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DiffLine> _diffLines = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsText))]
    private int _addedLinesCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsText))]
    private int _removedLinesCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatsText))]
    private int _modifiedLinesCount;

    public string StatsText => $"Changes: +{AddedLinesCount} added, -{RemovedLinesCount} removed, ~{ModifiedLinesCount} modified";

    public void Compare(string leftTitle, string leftText, string rightTitle, string rightText)
    {
        LeftTitle = leftTitle;
        RightTitle = rightTitle;
        LeftContent = leftText;
        RightContent = rightText;

        DiffLines.Clear();
        var leftLines = leftText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var rightLines = rightText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int max = Math.Max(leftLines.Length, rightLines.Length);
        int added = 0;
        int removed = 0;
        int modified = 0;

        for (int i = 0; i < max; i++)
        {
            string left = i < leftLines.Length ? leftLines[i] : null!;
            string right = i < rightLines.Length ? rightLines[i] : null!;

            if (left != null && right != null)
            {
                if (left == right)
                {
                    DiffLines.Add(new DiffLine(i + 1, i + 1, left, "Unchanged"));
                }
                else
                {
                    DiffLines.Add(new DiffLine(i + 1, i + 1, $"- {left} | + {right}", "Modified"));
                    modified++;
                }
            }
            else if (left != null && right == null)
            {
                DiffLines.Add(new DiffLine(i + 1, 0, $"- {left}", "Removed"));
                removed++;
            }
            else if (left == null && right != null)
            {
                DiffLines.Add(new DiffLine(0, i + 1, $"+ {right}", "Added"));
                added++;
            }
        }

        AddedLinesCount = added;
        RemovedLinesCount = removed;
        ModifiedLinesCount = modified;
    }
}
