using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using OpenMcdf;

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

public enum DocumentFormatFamily
{
    Word,
    Spreadsheet,
    Presentation,
    Image
}

internal static class DocumentFormatDetector
{
    private const string OfficeDocumentRelationshipSuffix = "/officeDocument";

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

    private static readonly IReadOnlyDictionary<string, DocumentFormat> OpenXmlContentTypes =
        new Dictionary<string, DocumentFormat>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"] = DocumentFormat.Docx,
            ["application/vnd.ms-word.document.macroEnabled.main+xml"] = DocumentFormat.Docm,
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.template.main+xml"] = DocumentFormat.Dotx,
            ["application/vnd.ms-word.template.macroEnabledTemplate.main+xml"] = DocumentFormat.Dotm,
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"] = DocumentFormat.Xlsx,
            ["application/vnd.ms-excel.sheet.macroEnabled.main+xml"] = DocumentFormat.Xlsm,
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.template.main+xml"] = DocumentFormat.Xltx,
            ["application/vnd.ms-excel.template.macroEnabled.main+xml"] = DocumentFormat.Xltm,
            ["application/vnd.ms-excel.sheet.binary.macroEnabled.main"] = DocumentFormat.Xlsb,
            ["application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"] = DocumentFormat.Pptx,
            ["application/vnd.ms-powerpoint.presentation.macroEnabled.main+xml"] = DocumentFormat.Pptm
        };

    internal static bool TryGetDeclaredFormat(string fileName, out DocumentFormat format)
    {
        return Extensions.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out format);
    }

    internal static bool TryGetFormatForExtension(string extension, out DocumentFormat format)
    {
        return Extensions.TryGetValue(extension, out format);
    }

    internal static DocumentFormatFamily? GetFamily(DocumentFormat format) => format switch
    {
        DocumentFormat.Doc or DocumentFormat.Docx or DocumentFormat.Docm or
            DocumentFormat.Dotx or DocumentFormat.Dotm or DocumentFormat.Rtf or
            DocumentFormat.Odt => DocumentFormatFamily.Word,
        DocumentFormat.Xls or DocumentFormat.Xlsx or DocumentFormat.Xlsm or
            DocumentFormat.Xltx or DocumentFormat.Xltm or DocumentFormat.Xlsb =>
            DocumentFormatFamily.Spreadsheet,
        DocumentFormat.Pptx or DocumentFormat.Pptm => DocumentFormatFamily.Presentation,
        DocumentFormat.Png or DocumentFormat.Jpeg or DocumentFormat.Tiff or DocumentFormat.Webp =>
            DocumentFormatFamily.Image,
        _ => null
    };

    internal static IEnumerable<DocumentFormat> GetFormats(DocumentFormatFamily family) =>
        Enum.GetValues<DocumentFormat>().Where(format => GetFamily(format) == family);

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
            Span<byte> header = stackalloc byte[16];
            var bytesRead = stream.Read(header);
            var bytes = header[..bytesRead];

            if (StartsWith(bytes, "%PDF-"u8)) return DocumentFormat.Pdf;
            if (StartsWith(bytes, "{\\rtf"u8)) return DocumentFormat.Rtf;
            if (StartsWith(bytes, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return DocumentFormat.Png;
            if (StartsWith(bytes, new byte[] { 0xFF, 0xD8, 0xFF })) return DocumentFormat.Jpeg;
            if (StartsWith(bytes, new byte[] { 0x49, 0x49, 0x2A, 0x00 }) ||
                StartsWith(bytes, new byte[] { 0x4D, 0x4D, 0x00, 0x2A })) return DocumentFormat.Tiff;
            if (bytes.Length >= 12 && StartsWith(bytes, "RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)) return DocumentFormat.Webp;
            if (StartsWith(bytes, new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 })) return DetectCompoundFile(stream);
            if (IsZip(bytes)) return DetectZipPackage(stream);

            return DocumentFormat.Unknown;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static DocumentFormat DetectCompoundFile(Stream stream)
    {
        stream.Position = 0;
        try
        {
            using var root = RootStorage.Open(
                stream,
                StorageModeFlags.LeaveOpen | StorageModeFlags.StrictValidation);

            if (root.TryGetEntryInfo("EncryptedPackage", out _)) return DocumentFormat.EncryptedOffice;
            if (root.TryGetEntryInfo("WordDocument", out _)) return DocumentFormat.Doc;
            if (root.TryGetEntryInfo("Workbook", out _) || root.TryGetEntryInfo("Book", out _)) return DocumentFormat.Xls;

            return DocumentFormat.Unknown;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or OpenMcdf.FileFormatException)
        {
            return DocumentFormat.Unknown;
        }
    }

    private static DocumentFormat DetectZipPackage(Stream stream)
    {
        stream.Position = 0;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > OfficePackageLimits.MaximumEntries)
            {
                return DocumentFormat.Unknown;
            }

            var mimetypeEntry = archive.GetEntry("mimetype");
            if (mimetypeEntry is not null && mimetypeEntry.Length <= 256)
            {
                using var reader = new StreamReader(mimetypeEntry.Open());
                if (string.Equals(
                    reader.ReadToEnd().Trim(),
                    "application/vnd.oasis.opendocument.text",
                    StringComparison.Ordinal))
                {
                    return DocumentFormat.Odt;
                }
            }

            var contentTypesEntry = archive.GetEntry("[Content_Types].xml");
            var relationshipsEntry = archive.GetEntry("_rels/.rels");
            if (contentTypesEntry is null ||
                contentTypesEntry.Length > OfficePackageLimits.MaximumContentTypesBytes ||
                relationshipsEntry is null ||
                relationshipsEntry.Length > OfficePackageLimits.MaximumRelationshipPartBytes)
            {
                return DocumentFormat.Unknown;
            }

            var primaryPartName = GetPrimaryOfficePartName(relationshipsEntry);
            if (primaryPartName is null)
            {
                return DocumentFormat.Unknown;
            }

            using var contentTypesStream = contentTypesEntry.Open();
            using var xmlReader = XmlReader.Create(contentTypesStream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = OfficePackageLimits.MaximumContentTypesBytes
            });
            var document = XDocument.Load(xmlReader, LoadOptions.None);
            var primaryContentType = document
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Override", StringComparison.Ordinal))
                .Where(element => string.Equals(
                    NormalizePartName((string)element.Attribute("PartName")),
                    primaryPartName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(element => (string)element.Attribute("ContentType"))
                .FirstOrDefault();

            primaryContentType ??= document
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Default", StringComparison.Ordinal))
                .Where(element => string.Equals(
                    (string)element.Attribute("Extension"),
                    Path.GetExtension(primaryPartName).TrimStart('.'),
                    StringComparison.OrdinalIgnoreCase))
                .Select(element => (string)element.Attribute("ContentType"))
                .FirstOrDefault();

            return primaryContentType is not null &&
                OpenXmlContentTypes.TryGetValue(primaryContentType, out var format)
                    ? format
                    : DocumentFormat.Unknown;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or XmlException)
        {
            return DocumentFormat.Unknown;
        }
    }

    private static string GetPrimaryOfficePartName(ZipArchiveEntry relationshipsEntry)
    {
        using var relationshipsStream = relationshipsEntry.Open();
        using var reader = XmlReader.Create(relationshipsStream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = OfficePackageLimits.MaximumRelationshipPartBytes
        });
        var relationships = XDocument.Load(reader, LoadOptions.None);
        var relationship = relationships
            .Descendants()
            .FirstOrDefault(element =>
                string.Equals(element.Name.LocalName, "Relationship", StringComparison.Ordinal) &&
                ((string)element.Attribute("Type"))?.EndsWith(
                    OfficeDocumentRelationshipSuffix,
                    StringComparison.Ordinal) == true &&
                !string.Equals((string)element.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase));

        var target = (string)relationship?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target) || target.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        return NormalizePartName(target);
    }

    private static string NormalizePartName(string partName)
    {
        if (string.IsNullOrWhiteSpace(partName)) return null;
        var normalized = partName.Trim().Replace('\\', '/');
        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : $"/{normalized}";
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> prefix)
    {
        return bytes.Length >= prefix.Length && bytes[..prefix.Length].SequenceEqual(prefix);
    }

    private static bool IsZip(ReadOnlySpan<byte> bytes)
    {
        return StartsWith(bytes, new byte[] { 0x50, 0x4B, 0x03, 0x04 }) ||
               StartsWith(bytes, new byte[] { 0x50, 0x4B, 0x05, 0x06 }) ||
               StartsWith(bytes, new byte[] { 0x50, 0x4B, 0x07, 0x08 });
    }
}
