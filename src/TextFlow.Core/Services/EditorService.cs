using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TextFlow.Services.Interfaces;

namespace TextFlow.Services;

public class EditorService : IEditorService
{
    public List<FindMatch> FindMatches(string text, FindOptions options)
    {
        var matches = new List<FindMatch>();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(options.SearchText))
        {
            return matches;
        }

        try
        {
            Regex regex = BuildRegex(options);
            var regexMatches = regex.Matches(text);

            foreach (Match match in regexMatches)
            {
                var (line, col) = GetLineAndColumn(text, match.Index);
                matches.Add(new FindMatch(match.Index, match.Length, match.Value, line, col));
            }
        }
        catch (ArgumentException)
        {
            // Invalid regex handled safely
        }

        return matches;
    }

    public (string NewText, int ReplacementCount) ReplaceAll(string text, FindOptions options, string replacement)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(options.SearchText))
        {
            return (text, 0);
        }

        try
        {
            Regex regex = BuildRegex(options);
            int count = regex.Matches(text).Count;
            string newText = regex.Replace(text, replacement);
            return (newText, count);
        }
        catch (ArgumentException)
        {
            return (text, 0);
        }
    }

    public (string NewText, bool Replaced, int NextIndex) ReplaceNext(string text, FindOptions options, string replacement, int startIndex)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(options.SearchText))
        {
            return (text, false, startIndex);
        }

        try
        {
            Regex regex = BuildRegex(options);
            int searchStart = Math.Max(0, Math.Min(startIndex, text.Length));
            var match = regex.Match(text, searchStart);

            if (!match.Success && searchStart > 0)
            {
                // Wrap around to beginning
                match = regex.Match(text, 0);
            }

            if (match.Success)
            {
                string newText = text.Remove(match.Index, match.Length).Insert(match.Index, replacement);
                return (newText, true, match.Index + replacement.Length);
            }
        }
        catch (ArgumentException)
        {
        }

        return (text, false, startIndex);
    }

    public (int Line, int Column) GetLineAndColumn(string text, int position)
    {
        if (string.IsNullOrEmpty(text) || position <= 0)
        {
            return (1, 1);
        }

        int clampedPos = Math.Min(position, text.Length);
        int line = 1;
        int lastLineBreak = -1;

        for (int i = 0; i < clampedPos; i++)
        {
            if (text[i] == '\r')
            {
                line++;
                if (i + 1 < clampedPos && text[i + 1] == '\n')
                {
                    i++;
                }
                lastLineBreak = i;
            }
            else if (text[i] == '\n')
            {
                line++;
                lastLineBreak = i;
            }
        }

        int column = clampedPos - lastLineBreak;
        return (line, column);
    }

    private static Regex BuildRegex(FindOptions options)
    {
        string pattern = options.UseRegex
            ? options.SearchText
            : Regex.Escape(options.SearchText);

        if (options.WholeWord)
        {
            pattern = $@"\b{pattern}\b";
        }

        var regexOptions = RegexOptions.Multiline;
        if (!options.MatchCase)
        {
            regexOptions |= RegexOptions.IgnoreCase;
        }

        return new Regex(pattern, regexOptions);
    }
}
