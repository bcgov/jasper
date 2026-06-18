namespace Scv.Core.Helpers;

public static class DocumentHelper
{
    private static readonly byte[] PdfSignature = [0x25, 0x50, 0x44, 0x46];
    private static readonly byte[] DocSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly byte[] DocxSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly byte[][] PdfOrWordSignatures = [PdfSignature, DocSignature, DocxSignature];

    public static bool IsWordDocument(Stream stream) =>
        HasSignature(stream, DocxSignature);

    public static bool IsPdfOrWordDocument(Stream stream) =>
        PdfOrWordSignatures.Any(signature => HasSignature(stream, signature));

    public static bool IsPdfOrWordDocumentBase64(string base64Data)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Data);
            return PdfOrWordSignatures.Any(signature =>
                bytes.Length >= signature.Length &&
                bytes.AsSpan(0, signature.Length).SequenceEqual(signature));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool HasSignature(Stream stream, ReadOnlySpan<byte> signature)
    {
        if (stream is null || !stream.CanRead || !stream.CanSeek)
        {
            return false;
        }

        if (stream.Length - stream.Position < signature.Length)
        {
            return false;
        }

        var originalPosition = stream.Position;
        Span<byte> buffer = stackalloc byte[signature.Length];
        try
        {
            stream.ReadExactly(buffer);
            return buffer.SequenceEqual(signature);
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
