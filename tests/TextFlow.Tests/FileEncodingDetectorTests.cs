using System.IO;
using System.Text;
using TextFlow.Infrastructure;
using Xunit;

namespace TextFlow.Tests;

public class FileEncodingDetectorTests
{
    [Fact]
    public void DetectEncoding_Utf8WithBom_ReturnsUtf8WithBom()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Hello World with UTF-8 BOM", new UTF8Encoding(true));
            var encoding = FileEncodingDetector.DetectEncoding(tempFile, out bool hasBom);

            Assert.True(hasBom);
            Assert.Equal("utf-8", encoding.WebName.ToLowerInvariant());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DetectEncoding_Utf16Le_ReturnsUnicode()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Hello UTF-16 LE", Encoding.Unicode);
            var encoding = FileEncodingDetector.DetectEncoding(tempFile, out bool hasBom);

            Assert.True(hasBom);
            Assert.Equal(Encoding.Unicode.WebName, encoding.WebName);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void DetectEncoding_PureAscii_ReturnsAscii()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "Pure ASCII text 123456", Encoding.ASCII);
            var encoding = FileEncodingDetector.DetectEncoding(tempFile, out bool hasBom);

            Assert.False(hasBom);
            Assert.Equal(Encoding.ASCII.WebName, encoding.WebName);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
