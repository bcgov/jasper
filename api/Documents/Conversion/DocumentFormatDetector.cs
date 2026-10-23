using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using FileSignatures;

namespace Scv.Api.Documents.Conversion;

public enum DocumentFormat
{
    Unknown,
    EncryptedOffice,
    Pdf,
    Doc,
    Docx,
    Docm,
    Dotx,
    Dotm,
    Xlsx,
    Xlsm,
    Xltx,
    Xltm,
    Xls,
    Xlsb,
    Pptx,
    Pptm,
    Rtf,
    Odt,
    Png,
    Jpeg,
    Tiff,
    Webp
}

internal static class DocumentFormatDetector
{
    private static readonly IFileFormatInspector Inspector = new FileFormatInspector();

    private static readonly IReadOnlyDictionary<string, DocumentFormat> Extensions =
        new Dictionary<string, DocumentFormat>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = DocumentFormat.Pdf,
            [".doc"] = DocumentFormat.Doc,
            [".docx"] = DocumentFormat.Docx,
            [".docm"] = DocumentFormat.Docm,
            [".dotx"] = DocumentFormat.Dotx,
            [".dotm"] = DocumentFormat.Dotm,
            [".xlsx"] = DocumentFormat.Xlsx,
            [".xlsm"] = DocumentFormat.Xlsm,
            [".xltx"] = DocumentFormat.Xltx,
            [".xltm"] = DocumentFormat.Xltm,
            [".xls"] = DocumentFormat.Xls,
            [".xlsb"] = DocumentFormat.Xlsb,
            [".pptx"] = DocumentFormat.Pptx,
            [".pptm"] = DocumentFormat.Pptm,
            [".rtf"] = DocumentFormat.Rtf,
            [".odt"] = DocumentFormat.Odt,
            [".png"] = DocumentFormat.Png,
            [".jpg"] = DocumentFormat.Jpeg,
            [".jpeg"] = DocumentFormat.Jpeg,
            [".tiff"] = DocumentFormat.Tiff,
            [".webp"] = DocumentFormat.Webp
        };

    internal static bool TryGetDeclaredFormat(string fileName, out DocumentFormat format)
    {
        return Extensions.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out format);
    }

    internal static bool TryGetFormatForExtension(string extension, out DocumentFormat format)
    {
        return Extensions.TryGetValue(extension, out format);
    }

    internal static DocumentFormat Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
        {
            throw new ArgumentException("A readable, seekable stream is required for format detection.", nameof(stream));
        }

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var detected = Inspector.DetermineFileFormat(stream);
            return MapDetectedFormat(detected?.Extension);
        }
        catch (Exception exception) when (exception is
            IOException or InvalidDataException or XmlException or OpenMcdf.FileFormatException)
        {
            return DocumentFormat.Unknown;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    internal static bool MatchesDeclaredFormat(
        DocumentFormat declaredFormat,
        DocumentFormat detectedFormat)
    {
        if (declaredFormat == detectedFormat)
        {
            return true;
        }

        // FileSignatures reports OOXML variants as their base Word, Excel, or PowerPoint
        // type for some valid packages. Keep the exception narrow so cross-family renames fail.
        return declaredFormat switch
        {
            DocumentFormat.Docm or DocumentFormat.Dotx or DocumentFormat.Dotm =>
                detectedFormat == DocumentFormat.Docx,
            DocumentFormat.Xlsm or DocumentFormat.Xltx or DocumentFormat.Xltm =>
                detectedFormat == DocumentFormat.Xlsx,
            DocumentFormat.Pptm => detectedFormat == DocumentFormat.Pptx,
            _ => false
        };
    }

    private static DocumentFormat MapDetectedFormat(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return DocumentFormat.Unknown;
        }

        if (extension.Equals("jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Jpeg;
        }

        if (extension.Equals("tif", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals("tiff", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Tiff;
        }

        return Enum.TryParse<DocumentFormat>(extension, ignoreCase: true, out var format) &&
            format is not DocumentFormat.Unknown and not DocumentFormat.EncryptedOffice
                ? format
                : DocumentFormat.Unknown;
    }
}
