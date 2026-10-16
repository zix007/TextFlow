using System.Collections.Generic;
using System.Threading.Tasks;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface IRecoveryService
{
    Task<List<AutoSaveRecord>> GetRecoverableFilesAsync();
    Task<DocumentModel> RestoreFileAsync(AutoSaveRecord record);
    Task DiscardFileAsync(AutoSaveRecord record);
    Task DiscardAllAsync();
}
