using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using FileSignatures;
using FileSignatures.Formats;

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
    private const string OfficeDocumentRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    private static readonly XNamespace RelationshipNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypeNamespace =
        "http://schemas.openxmlformats.org/package/2006/content-types";
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
            var format = MapDetectedFormat(detected?.Extension);
            return detected is OfficeOpenXml && format != DocumentFormat.Unknown
                ? DetectOpenXmlPackage(stream)
                : format;
        }
        catch (Exception exception) when (exception is
            IOException or InvalidDataException or XmlException or FormatException or
            ArgumentException or OpenMcdf.FileFormatException)
        {
            return DocumentFormat.Unknown;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static DocumentFormat DetectOpenXmlPackage(Stream stream)
    {
        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > OfficePackageLimits.MaximumEntries ||
            archive.GetEntry("[Content_Types].xml") is not { } contentTypes ||
            contentTypes.Length > OfficePackageLimits.MaximumContentTypesBytes ||
            archive.GetEntry("_rels/.rels") is not { } relationships ||
            relationships.Length > OfficePackageLimits.MaximumRelationshipPartBytes)
        {
            return DocumentFormat.Unknown;
        }

        var relationshipDocument = LoadXml(relationships, OfficePackageLimits.MaximumRelationshipPartBytes);
        var primary = relationshipDocument
            .Descendants(RelationshipNamespace + "Relationship")
            .FirstOrDefault(element =>
                (string)element.Attribute("Type") == OfficeDocumentRelationshipType &&
                !string.Equals((string)element.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase));
        var target = (string)primary?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target) || target.Contains("..", StringComparison.Ordinal))
        {
            return DocumentFormat.Unknown;
        }

        var partName = "/" + target.TrimStart('/').Replace('\\', '/');
        if (archive.GetEntry(partName.TrimStart('/')) is null)
        {
            return DocumentFormat.Unknown;
        }

        var contentTypeDocument = LoadXml(contentTypes, OfficePackageLimits.MaximumContentTypesBytes);
        var contentType = contentTypeDocument
            .Descendants(ContentTypeNamespace + "Override")
            .Where(element => string.Equals(
                (string)element.Attribute("PartName"), partName, StringComparison.OrdinalIgnoreCase))
            .Select(element => (string)element.Attribute("ContentType"))
            .FirstOrDefault();

        contentType ??= contentTypeDocument
            .Descendants(ContentTypeNamespace + "Default")
            .Where(element => string.Equals(
                (string)element.Attribute("Extension"),
                Path.GetExtension(partName).TrimStart('.'),
                StringComparison.OrdinalIgnoreCase))
            .Select(element => (string)element.Attribute("ContentType"))
            .FirstOrDefault();

        return contentType is not null && OpenXmlContentTypes.TryGetValue(contentType, out var format)
                ? format
                : DocumentFormat.Unknown;
    }

    private static XDocument LoadXml(ZipArchiveEntry entry, long maximumCharacters)
    {
        using var entryStream = entry.Open();
        using var reader = XmlReader.Create(entryStream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maximumCharacters
        });
        return XDocument.Load(reader, LoadOptions.None);
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
