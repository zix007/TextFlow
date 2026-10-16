using TextFlow.Services;
using TextFlow.Services.Interfaces;
using Xunit;

namespace TextFlow.Tests;

public class EditorServiceTests
{
    private readonly EditorService _service = new();

    [Fact]
    public void FindMatches_StandardSearch_FindsAllOccurrences()
    {
        string text = "TextFlow is great. TextFlow is fast. TEXTFLOW rocks.";
        var options = new FindOptions("TextFlow", MatchCase: true, WholeWord: false, UseRegex: false);

        var matches = _service.FindMatches(text, options);

        Assert.Equal(2, matches.Count);
        Assert.Equal(0, matches[0].Index);
        Assert.Equal(19, matches[1].Index);
    }

    [Fact]
    public void FindMatches_CaseInsensitive_FindsAllCases()
    {
        string text = "TextFlow is great. textflow is fast. TEXTFLOW rocks.";
        var options = new FindOptions("textflow", MatchCase: false, WholeWord: false, UseRegex: false);

        var matches = _service.FindMatches(text, options);

        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void FindMatches_RegexSearch_FindsMatches()
    {
        string text = "Order #123, Order #456 and Order #789";
        var options = new FindOptions(@"Order #\d+", MatchCase: false, WholeWord: false, UseRegex: true);

        var matches = _service.FindMatches(text, options);

        Assert.Equal(3, matches.Count);
        Assert.Equal("Order #123", matches[0].MatchedText);
        Assert.Equal("Order #456", matches[1].MatchedText);
    }

    [Fact]
    public void ReplaceAll_ReplacesCorrectly()
    {
        string text = "Apple, Banana, Apple, Cherry";
        var options = new FindOptions("Apple", MatchCase: true, WholeWord: true, UseRegex: false);

        var (newText, count) = _service.ReplaceAll(text, options, "Orange");

        Assert.Equal(2, count);
        Assert.Equal("Orange, Banana, Orange, Cherry", newText);
    }

    [Fact]
    public void GetLineAndColumn_CalculatesCorrectPosition()
    {
        string text = "Line 1\nLine 2\nLine 3 is long";
        int index = text.IndexOf("Line 2");

        var (line, col) = _service.GetLineAndColumn(text, index);

        Assert.Equal(2, line);
        Assert.Equal(1, col);
    }

    [Theory]
    [InlineData("Line 1\nLine 2\nLine 3", "Line 2", 2, 1)]
    [InlineData("Line 1\rLine 2\rLine 3", "Line 2", 2, 1)]
    [InlineData("Line 1\r\nLine 2\r\nLine 3", "Line 2", 2, 1)]
    [InlineData("\r\r\rText", "Text", 4, 1)]
    public void GetLineAndColumn_HandlesVariousNewlineFormats(string text, string target, int expectedLine, int expectedCol)
    {
        int index = text.IndexOf(target);
        var (line, col) = _service.GetLineAndColumn(text, index);
        Assert.Equal(expectedLine, line);
        Assert.Equal(expectedCol, col);
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData("Single line", "1")]
    [InlineData("Line 1\nLine 2\nLine 3", "1\r\n2\r\n3")]
    [InlineData("Line 1\rLine 2\rLine 3", "1\r\n2\r\n3")]
    [InlineData("Line 1\r\nLine 2\r\nLine 3", "1\r\n2\r\n3")]
    [InlineData("\r\r\r", "1\r\n2\r\n3\r\n4")]
    public void DocumentViewModel_LineNumbersText_HandlesAllNewlineFormats(string content, string expected)
    {
        var model = new TextFlow.Models.DocumentModel { Content = content };
        var vm = new TextFlow.ViewModels.DocumentViewModel(model);
        // Normalize newlines for cross-platform comparison
        string normalizedActual = vm.LineNumbersText.Replace("\r\n", "\n").Replace("\r", "\n");
        string normalizedExpected = expected.Replace("\r\n", "\n").Replace("\r", "\n");
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   \r\n   ", null)]
    [InlineData("Hello World", "Hello World")]
    [InlineData("# My Project Notes\nBody text here", "My Project Notes")]
    [InlineData("### Deep Heading", "Deep Heading")]
    [InlineData("- First Task in List\n- Second Task", "First Task in List")]
    [InlineData("* Bullet Point", "Bullet Point")]
    [InlineData("1. Numbered Item", "Numbered Item")]
    [InlineData("\r\n\r\nActual First Line\r\nSecond Line", "Actual First Line")]
    public void DocumentViewModel_GetInferredTitle_ExtractsFirstLineCorrectly(string? content, string? expected)
    {
        string? result = TextFlow.ViewModels.DocumentViewModel.GetInferredTitle(content);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DocumentViewModel_GetInferredTitle_TruncatesLongLines()
    {
        string longLine = "This is an extremely long title that exceeds the maximum allowed character limit for a tab";
        string? result = TextFlow.ViewModels.DocumentViewModel.GetInferredTitle(longLine);
        Assert.NotNull(result);
        Assert.True(result.Length <= 32);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void DocumentViewModel_DisplayTitle_UsesInferredTitleWhenUntitled()
    {
        var model = new TextFlow.Models.DocumentModel
        {
            Title = "Untitled 1",
            FilePath = null,
            Content = "Project Roadmap\n- Item 1"
        };
        var vm = new TextFlow.ViewModels.DocumentViewModel(model);

        Assert.Equal("Project Roadmap", vm.DisplayTitle);
        Assert.Equal("Project Roadmap", vm.HeaderTitle);

        vm.IsModified = true;
        Assert.Equal("Project Roadmap *", vm.HeaderTitle);
    }

    [Fact]
    public void DocumentViewModel_DisplayTitle_UsesFileNameWhenSaved()
    {
        var model = new TextFlow.Models.DocumentModel
        {
            Title = "notes.txt",
            FilePath = "C:\\docs\\notes.txt",
            Content = "Meeting Minutes\nToday we discussed..."
        };
        var vm = new TextFlow.ViewModels.DocumentViewModel(model);

        Assert.Equal("notes.txt", vm.DisplayTitle);
    }
}
