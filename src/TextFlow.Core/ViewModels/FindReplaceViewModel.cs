using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextFlow.Services.Interfaces;

namespace TextFlow.ViewModels;

public partial class FindReplaceViewModel : ObservableObject
{
    private readonly IEditorService _editorService;
    private DocumentViewModel? _currentDocument;
    private List<FindMatch> _currentMatches = new();
    private int _currentMatchIndex = -1;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private bool _matchCase;

    [ObservableProperty]
    private bool _wholeWord;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private bool _isReplaceMode;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event EventHandler<(int Index, int Length)>? SelectTextRequested;

    public FindReplaceViewModel(IEditorService editorService)
    {
        _editorService = editorService;
    }

    public void AttachDocument(DocumentViewModel? doc)
    {
        _currentDocument = doc;
        UpdateMatches();
    }

    partial void OnSearchTextChanged(string value) => UpdateMatches();
    partial void OnMatchCaseChanged(bool value) => UpdateMatches();
    partial void OnWholeWordChanged(bool value) => UpdateMatches();
    partial void OnUseRegexChanged(bool value) => UpdateMatches();

    public void UpdateMatches()
    {
        if (_currentDocument == null || string.IsNullOrEmpty(SearchText))
        {
            _currentMatches.Clear();
            _currentMatchIndex = -1;
            StatusMessage = string.Empty;
            return;
        }

        var options = new FindOptions(SearchText, MatchCase, WholeWord, UseRegex);
        _currentMatches = _editorService.FindMatches(_currentDocument.Content, options);

        if (_currentMatches.Count > 0)
        {
            _currentMatchIndex = 0;
            StatusMessage = $"1 of {_currentMatches.Count} matches";
            HighlightCurrentMatch();
        }
        else
        {
            _currentMatchIndex = -1;
            StatusMessage = "No matches found";
        }
    }

    [RelayCommand]
    public void FindNext()
    {
        if (_currentMatches.Count == 0)
        {
            UpdateMatches();
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _currentMatches.Count;
        StatusMessage = $"{_currentMatchIndex + 1} of {_currentMatches.Count} matches";
        HighlightCurrentMatch();
    }

    [RelayCommand]
    public void FindPrevious()
    {
        if (_currentMatches.Count == 0)
        {
            UpdateMatches();
            return;
        }

        _currentMatchIndex = (_currentMatchIndex - 1 + _currentMatches.Count) % _currentMatches.Count;
        StatusMessage = $"{_currentMatchIndex + 1} of {_currentMatches.Count} matches";
        HighlightCurrentMatch();
    }

    [RelayCommand]
    public void ReplaceCurrent()
    {
        if (_currentDocument == null || string.IsNullOrEmpty(SearchText)) return;

        var options = new FindOptions(SearchText, MatchCase, WholeWord, UseRegex);
        int startIndex = _currentMatchIndex >= 0 && _currentMatchIndex < _currentMatches.Count
            ? _currentMatches[_currentMatchIndex].Index
            : 0;

        var (newText, replaced, nextIndex) = _editorService.ReplaceNext(_currentDocument.Content, options, ReplaceText, startIndex);
        if (replaced)
        {
            _currentDocument.Content = newText;
            UpdateMatches();
        }
    }

    [RelayCommand]
    public void ReplaceAll()
    {
        if (_currentDocument == null || string.IsNullOrEmpty(SearchText)) return;

        var options = new FindOptions(SearchText, MatchCase, WholeWord, UseRegex);
        var (newText, count) = _editorService.ReplaceAll(_currentDocument.Content, options, ReplaceText);

        _currentDocument.Content = newText;
        UpdateMatches();
        StatusMessage = $"Replaced {count} occurrences";
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
    }

    private void HighlightCurrentMatch()
    {
        if (_currentMatchIndex >= 0 && _currentMatchIndex < _currentMatches.Count)
        {
            var match = _currentMatches[_currentMatchIndex];
            SelectTextRequested?.Invoke(this, (match.Index, match.Length));
        }
    }
}
