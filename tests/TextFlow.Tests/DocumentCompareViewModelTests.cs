using TextFlow.ViewModels;
using Xunit;

namespace TextFlow.Tests;

public class DocumentCompareViewModelTests
{
    [Fact]
    public void Compare_DetectsAddedRemovedAndModifiedLines()
    {
        var vm = new DocumentCompareViewModel();

        string docA = "Line 1\nLine 2\nLine 3\nLine 4";
        string docB = "Line 1\nLine 2 (Changed)\nLine 3\nLine 4\nLine 5";

        vm.Compare("Doc A", docA, "Doc B", docB);

        Assert.Equal(5, vm.DiffLines.Count);
        Assert.Equal(1, vm.ModifiedLinesCount); // Line 2 modified
        Assert.Equal(1, vm.AddedLinesCount);    // Line 5 added
        Assert.Equal(0, vm.RemovedLinesCount);
        Assert.Contains("+1 added", vm.StatsText);
    }
}
