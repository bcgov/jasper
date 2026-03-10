using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Scv.Api.SignalR;

public static class PostgresSignalRPayloadCodec
{
    public static int GetUtf8Size(string value)
    {
        return value == null ? 0 : Encoding.UTF8.GetByteCount(value);
    }

    public static string CompressToBase64(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var inputBytes = Encoding.UTF8.GetBytes(value);
        using var outputStream = new MemoryStream();
        using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(inputBytes, 0, inputBytes.Length);
        }

        return Convert.ToBase64String(outputStream.ToArray());
    }

    public static string DecompressFromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return string.Empty;
        }

        var compressedBytes = Convert.FromBase64String(base64);
        using var inputStream = new MemoryStream(compressedBytes);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzip.CopyTo(outputStream);

        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
