using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Documents.Conversion;

public interface IDocumentConversionSecurityPreflight
{
    void Validate(Stream input, DocumentFormat format, long maximumExpandedBytes);
}

internal sealed class DocumentConversionSecurityPreflight : IDocumentConversionSecurityPreflight
{
    private readonly ILogger<DocumentConversionSecurityPreflight> _logger;

    private static readonly DocumentFormat[] OpenXmlFormats =
    [
        DocumentFormat.Docx,
        DocumentFormat.Docm,
        DocumentFormat.Dotx,
        DocumentFormat.Dotm,
        DocumentFormat.Xlsx,
        DocumentFormat.Xlsm,
        DocumentFormat.Xltx,
        DocumentFormat.Xltm,
        DocumentFormat.Xlsb,
        DocumentFormat.Pptx,
        DocumentFormat.Pptm
    ];

    public DocumentConversionSecurityPreflight(
        ILogger<DocumentConversionSecurityPreflight> logger = null)
    {
        _logger = logger;
    }

    public void Validate(Stream input, DocumentFormat format, long maximumExpandedBytes)
    {
        if (!OpenXmlFormats.Contains(format))
        {
            return;
        }

        var originalPosition = input.Position;
        try
        {
            input.Position = 0;
            using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > OfficePackageLimits.MaximumEntries)
            {
                throw new InvalidDataException("The Office package contains too many entries.");
            }

            ValidateArchiveEntries(archive, maximumExpandedBytes);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException("The Office package contains malformed relationship metadata.", exception);
        }
        finally
        {
            input.Position = originalPosition;
        }
    }

    private void ValidateArchiveEntries(ZipArchive archive, long maximumExpandedBytes)
    {
        long expandedBytes = 0;
        long relationshipBytes = 0;
        foreach (var entry in archive.Entries)
        {
            expandedBytes = checked(expandedBytes + entry.Length);
            if (expandedBytes > maximumExpandedBytes)
            {
                throw new InvalidDataException("The expanded Office package exceeds the configured limit.");
            }

            var normalizedName = entry.FullName.Replace('\\', '/');
            ValidatePartName(normalizedName);

            if (!normalizedName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            relationshipBytes = checked(relationshipBytes + entry.Length);
            if (entry.Length > OfficePackageLimits.MaximumRelationshipPartBytes ||
                relationshipBytes > OfficePackageLimits.MaximumRelationshipPartsBytes)
            {
                throw new InvalidDataException("Office relationship metadata exceeds the configured limit.");
            }

            InspectRelationships(entry);
        }
    }

    private static void ValidatePartName(string normalizedName)
    {
        if (normalizedName.StartsWith("/", StringComparison.Ordinal) ||
            normalizedName.Split('/').Any(segment => segment == ".."))
        {
            throw new InvalidDataException("The Office package contains an unsafe part path.");
        }

        if (normalizedName.Contains("/activeX/", StringComparison.OrdinalIgnoreCase) ||
            normalizedName.StartsWith("activeX/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Office package contains ActiveX content.");
        }

        if (normalizedName.Contains("/embeddings/", StringComparison.OrdinalIgnoreCase) ||
            normalizedName.StartsWith("embeddings/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Office package contains an embedded payload.");
        }
    }

    private void InspectRelationships(ZipArchiveEntry entry)
    {
        using var relationshipStream = entry.Open();
        using var reader = XmlReader.Create(relationshipStream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = OfficePackageLimits.MaximumRelationshipPartBytes
        });
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element ||
                !string.Equals(reader.LocalName, "Relationship", StringComparison.Ordinal) ||
                !string.Equals(reader.GetAttribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relationshipType = reader.GetAttribute("Type");
            var target = reader.GetAttribute("Target");
            var uri = GetAbsoluteUri(target);
            var targetIsAbsolute = uri is not null;
            var targetScheme = targetIsAbsolute
                ? uri.Scheme
                : "relative-or-invalid";

            // Retain relationship metadata without dereferencing it. Converter-specific
            // resource policies and LibreOffice link-update settings prevent access.
            _logger?.LogWarning(
                "Office package contains an external relationship that will be preserved without retrieval - PackagePart: {PackagePart}, RelationshipType: {RelationshipType}, TargetScheme: {TargetScheme}, TargetIsAbsolute: {TargetIsAbsolute}",
                entry.FullName,
                relationshipType,
                targetScheme,
                targetIsAbsolute);
        }
    }

    private static Uri GetAbsoluteUri(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri;
    }
}
