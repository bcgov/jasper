using System;
using System.Linq;

namespace Scv.Api.Helpers;

public static class DocumentHelper
{
    private static readonly byte[][] AllowedSignatures =
    [
        // PDF
        [0x25, 0x50, 0x44, 0x46],
        // DOC
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1],
        // DOCX
        [0x50, 0x4B, 0x03, 0x04]
    ];

    public static bool IsPdfOrWordDocumentBase64(string base64Data)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Data);
            return AllowedSignatures.Any(signature =>
                bytes.Length >= signature.Length &&
                bytes.AsSpan(0, signature.Length).SequenceEqual(signature));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}