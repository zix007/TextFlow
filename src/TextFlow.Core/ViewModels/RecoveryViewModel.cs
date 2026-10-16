using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TextFlow.Models;
using TextFlow.Services.Interfaces;

namespace TextFlow.ViewModels;

public partial class RecoveryViewModel : ObservableObject
{
    private readonly IRecoveryService _recoveryService;

    [ObservableProperty]
    private ObservableCollection<AutoSaveRecord> _recoverableFiles = new();

    [ObservableProperty]
    private AutoSaveRecord? _selectedRecord;

    public event System.EventHandler<DocumentModel>? DocumentRestored;

    public RecoveryViewModel(IRecoveryService recoveryService)
    {
        _recoveryService = recoveryService;
    }

    public async Task LoadRecoverableFilesAsync()
    {
        var records = await _recoveryService.GetRecoverableFilesAsync();
        RecoverableFiles.Clear();
        foreach (var r in records)
        {
            RecoverableFiles.Add(r);
        }
        if (RecoverableFiles.Count > 0)
        {
            SelectedRecord = RecoverableFiles[0];
        }
    }

    [RelayCommand]
    public async Task RestoreSelectedAsync()
    {
        if (SelectedRecord == null) return;

        var doc = await _recoveryService.RestoreFileAsync(SelectedRecord);
        RecoverableFiles.Remove(SelectedRecord);
        DocumentRestored?.Invoke(this, doc);
    }

    [RelayCommand]
    public async Task DiscardSelectedAsync()
    {
        if (SelectedRecord == null) return;

        await _recoveryService.DiscardFileAsync(SelectedRecord);
        RecoverableFiles.Remove(SelectedRecord);
    }

    [RelayCommand]
    public async Task RestoreAllAsync()
    {
        var list = new System.Collections.Generic.List<AutoSaveRecord>(RecoverableFiles);
        foreach (var record in list)
        {
            var doc = await _recoveryService.RestoreFileAsync(record);
            DocumentRestored?.Invoke(this, doc);
        }
        RecoverableFiles.Clear();
    }

    [RelayCommand]
    public async Task DiscardAllAsync()
    {
        await _recoveryService.DiscardAllAsync();
        RecoverableFiles.Clear();
    }
}
