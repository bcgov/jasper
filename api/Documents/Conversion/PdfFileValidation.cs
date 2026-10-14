using System;
using System.IO;

namespace Scv.Api.Documents.Conversion;

internal static class PdfFileValidation
{
    public static bool HasPdfSignature(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var position = stream.Position;
        try
        {
            stream.Position = 0;
            Span<byte> signature = stackalloc byte[5];
            return stream.Read(signature) == signature.Length &&
                signature.SequenceEqual("%PDF-"u8);
        }
        finally
        {
            stream.Position = position;
        }
    }
}
