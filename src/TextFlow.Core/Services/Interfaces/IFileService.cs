using System.Text;
using System.Threading.Tasks;
using TextFlow.Models;

namespace TextFlow.Services.Interfaces;

public interface IFileService
{
    Task<DocumentModel> OpenFileAsync(string filePath);
    Task SaveFileAsync(DocumentModel document, string? customPath = null, Encoding? customEncoding = null);
    bool FileExists(string filePath);
    string GetFileExtension(string filePath);
    string GetFileName(string filePath);
}
