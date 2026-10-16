using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface IAutoSaveService
{
    void Start(int intervalSeconds);
    void Stop();
    Task AutoSaveDocumentAsync(DocumentModel document);
    Task AutoSaveAllAsync(IEnumerable<DocumentModel> documents);
    Task ClearAutoSaveAsync(string documentId);
    string GetAutoSaveDirectory();
}
