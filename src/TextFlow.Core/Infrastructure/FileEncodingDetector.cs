using System;
using System.IO;
using System.Text;

namespace TextFlow.Infrastructure;

public static class FileEncodingDetector
{
    public static Encoding DetectEncoding(string filePath, out bool hasBom)
    {
        hasBom = false;
        if (!File.Exists(filePath))
        {
            return Encoding.UTF8;
        }

        byte[] buffer = new byte[4];
        int bytesRead;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            bytesRead = fs.Read(buffer, 0, buffer.Length);
        }

        if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            hasBom = true;
            return new UTF8Encoding(true);
        }

        if (bytesRead >= 2)
        {
            if (buffer[0] == 0xFF && buffer[1] == 0xFE)
            {
                hasBom = true;
                return Encoding.Unicode; // UTF-16 LE
            }
            if (buffer[0] == 0xFE && buffer[1] == 0xFF)
            {
                hasBom = true;
                return Encoding.BigEndianUnicode; // UTF-16 BE
            }
        }

        // Read sample of file (up to 8KB) to check if valid UTF-8 or ASCII
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            byte[] sample = new byte[Math.Min(8192, (int)fs.Length)];
            int sampleBytes = fs.Read(sample, 0, sample.Length);

            if (IsPureAscii(sample, sampleBytes))
            {
                return Encoding.ASCII;
            }

            if (IsValidUtf8(sample, sampleBytes))
            {
                return new UTF8Encoding(false);
            }
        }

        return Encoding.Default;
    }

    public static Encoding GetEncodingByName(string name, bool withBom = false)
    {
        return name.ToUpperInvariant() switch
        {
            "UTF-8" => new UTF8Encoding(withBom),
            "UTF-8 WITH BOM" => new UTF8Encoding(true),
            "UTF-16 LE" or "UTF-16" or "UNICODE" => Encoding.Unicode,
            "UTF-16 BE" => Encoding.BigEndianUnicode,
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8
        };
    }

    public static string GetEncodingDisplayName(Encoding encoding, bool hasBom = false)
    {
        if (encoding is UTF8Encoding utf8)
        {
            return hasBom ? "UTF-8 with BOM" : "UTF-8";
        }
        if (encoding == Encoding.Unicode)
        {
            return "UTF-16 LE";
        }
        if (encoding == Encoding.BigEndianUnicode)
        {
            return "UTF-16 BE";
        }
        if (encoding == Encoding.ASCII)
        {
            return "ASCII";
        }
        return encoding.WebName.ToUpperInvariant();
    }

    private static bool IsPureAscii(byte[] bytes, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (bytes[i] > 127)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidUtf8(byte[] bytes, int length)
    {
        int i = 0;
        while (i < length)
        {
            if (bytes[i] <= 0x7F)
            {
                i++;
                continue;
            }

            int n;
            if ((bytes[i] & 0xE0) == 0xC0)
                n = 1;
            else if ((bytes[i] & 0xF0) == 0xE0)
                n = 2;
            else if ((bytes[i] & 0xF8) == 0xF0)
                n = 3;
            else
                return false;

            i++;
            for (int j = 0; j < n; j++)
            {
                if (i >= length || (bytes[i] & 0xC0) != 0x80)
                    return false;
                i++;
            }
        }
        return true;
    }
}
