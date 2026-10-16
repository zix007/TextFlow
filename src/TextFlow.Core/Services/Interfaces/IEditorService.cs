using System.Collections.Generic;

namespace TextFlow.Services.Interfaces;

public record FindMatch(int Index, int Length, string MatchedText, int Line, int Column);

public record FindOptions(string SearchText, bool MatchCase, bool WholeWord, bool UseRegex);

public interface IEditorService
{
    List<FindMatch> FindMatches(string text, FindOptions options);
    (string NewText, int ReplacementCount) ReplaceAll(string text, FindOptions options, string replacement);
    (string NewText, bool Replaced, int NextIndex) ReplaceNext(string text, FindOptions options, string replacement, int startIndex);
    (int Line, int Column) GetLineAndColumn(string text, int position);
}
