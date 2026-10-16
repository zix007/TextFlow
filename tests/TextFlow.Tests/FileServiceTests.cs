using System.IO;
using System.Text;
using System.Threading.Tasks;
using TextFlow.Models;
using TextFlow.Services;
using Xunit;

namespace TextFlow.Tests;

public class FileServiceTests
{
    private readonly FileService _service = new();

    [Fact]
    public async Task SaveFileAndOpenFile_PreservesContentAndEncoding()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "TextFlowTests");
        Directory.CreateDirectory(tempDir);
        string testFile = Path.Combine(tempDir, "test_doc.md");

        try
        {
            var doc = DocumentModel.CreateNew("test_doc.md", "# Title\nThis is markdown.");
            doc.FilePath = testFile;

            await _service.SaveFileAsync(doc, testFile, Encoding.UTF8);

            Assert.True(File.Exists(testFile));

            var openedDoc = await _service.OpenFileAsync(testFile);
            Assert.Equal("# Title\nThis is markdown.", openedDoc.Content);
            Assert.Equal(".md", openedDoc.FileExtension);
            Assert.False(openedDoc.IsModified);
        }
        finally
        {
            if (File.Exists(testFile)) File.Delete(testFile);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
