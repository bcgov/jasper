using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OfficeIMO;
using OfficeIMO.Excel.Xlsb;
using OfficeIMO.Reader;
using OfficeIMO.Rtf.Diagnostics;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal sealed class OfficeImoDiagnosticPolicy
{
    private const string ConversionErrorMessage = DocumentPdfConversionService.ConversionErrorMessage;
    private const string HeaderFooterZoneOverlapDiagnosticCode = "HeaderFooterZoneOverlap";

    private static readonly HashSet<string> CosmeticXlsbDiagnosticCodes = new(StringComparer.Ordinal)
    {
        "XLSB-PAGESETUP-FIRST-PAGE-PRESERVED"
    };

    private static readonly HashSet<string> MaterialXlsbDiagnosticCodes = new(StringComparer.Ordinal)
    {
        "XLSB-RECORDS-PRESERVED",
        "XLSB-DEFINED-NAME-PRESERVED",
        "XLSB-AUTOFILTER-PARTIAL"
    };

    private static readonly HashSet<string> NonLossImageDiagnosticCodes = new(StringComparer.Ordinal)
    {
        "IMAGE_SOURCE_DECODED_BY_CALLER_CODEC"
    };

    private readonly ILogger _logger;

    public OfficeImoDiagnosticPolicy(ILogger logger)
    {
        _logger = logger;
    }

    public void ValidateXlsb(string fileName, IReadOnlyList<XlsbImportDiagnostic> diagnostics)
    {
        var informationalDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == XlsbImportDiagnosticSeverity.Information)
            .ToArray();
        if (informationalDiagnostics.Length > 0)
        {
            _logger.LogInformation(
                "XLSB import produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatXlsbDiagnostics(informationalDiagnostics));
        }

        var warningDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == XlsbImportDiagnosticSeverity.Warning)
            .ToArray();
        if (warningDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "XLSB import produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatXlsbDiagnostics(warningDiagnostics));
        }

        var errorDiagnostics = diagnostics
            .Where(diagnostic => IsFatalXlsbDiagnostic(diagnostic.Severity))
            .ToArray();
        if (errorDiagnostics.Length > 0)
        {
            _logger.LogError(
                "XLSB parsing was rejected - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatXlsbDiagnostics(errorDiagnostics));
            throw new DocumentParsingRejectedException(ConversionErrorMessage);
        }

        var fidelityDiagnostics = diagnostics
            .Where(diagnostic => ClassifyXlsbDiagnostic(diagnostic.Code) is
                DocumentDiagnosticImpact.Material or DocumentDiagnosticImpact.Unknown)
            .ToArray();
        if (fidelityDiagnostics.Length == 0) return;

        _logger.LogError(
            "XLSB import rejected potential material content loss - FileName: {FileName}, Diagnostics: {@Diagnostics}",
            fileName,
            fidelityDiagnostics.Select(diagnostic => new
            {
                diagnostic.Code,
                Severity = diagnostic.Severity.ToString(),
                diagnostic.Message
            }).ToArray());
        throw new DocumentFidelityRejectedException(ConversionErrorMessage);
    }

    public void ValidateRtf(string fileName, IReadOnlyList<RtfDiagnostic> diagnostics)
    {
        var informationalDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == RtfDiagnosticSeverity.Info)
            .ToArray();
        if (informationalDiagnostics.Length > 0)
        {
            _logger.LogInformation(
                "RTF parsing produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatRtfDiagnostics(informationalDiagnostics));
        }

        var warningDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == RtfDiagnosticSeverity.Warning)
            .ToArray();
        if (warningDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "RTF parsing produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatRtfDiagnostics(warningDiagnostics));
        }

        var errorDiagnostics = diagnostics
            .Where(diagnostic => IsFatalRtfDiagnostic(diagnostic.Severity))
            .ToArray();
        if (errorDiagnostics.Length == 0) return;

        _logger.LogError(
            "RTF parsing was rejected - FileName: {FileName}, Diagnostics: {@Diagnostics}",
            fileName,
            FormatRtfDiagnostics(errorDiagnostics));
        throw new DocumentParsingRejectedException(ConversionErrorMessage);
    }

    public void ValidateImage(
        string fileName,
        IReadOnlyList<OfficeDocumentDiagnostic> diagnostics)
    {
        var informationalDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Information)
            .ToArray();
        if (informationalDiagnostics.Length > 0)
        {
            _logger.LogInformation(
                "Image reading produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(informationalDiagnostics));
        }

        var warningDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Warning)
            .ToArray();
        if (warningDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "Image reading produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(warningDiagnostics));
        }

        var fidelityDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity is not OfficeDocumentDiagnosticSeverity.Information)
            .Where(diagnostic => ClassifyImageDiagnostic(diagnostic.Code) is not
                DocumentDiagnosticImpact.NoContentLoss)
            .ToArray();
        if (fidelityDiagnostics.Length > 0)
        {
            _logger.LogError(
                "Image conversion rejected potential material content loss - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(fidelityDiagnostics));
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
        }

        var errorDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Error)
            .ToArray();
        if (errorDiagnostics.Length == 0) return;

        _logger.LogError(
            "Image conversion rejected fatal reader diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
            fileName,
            FormatImageDiagnostics(errorDiagnostics));
        throw new UnprocessableDocumentException(ConversionErrorMessage);
    }

    public void ValidateFidelity(
        string fileName,
        DocumentFormat format,
        IReadOnlyList<OfficeConversionFidelityDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0) return;

        _logger.LogWarning(
            "OfficeIMO conversion produced diagnostics - FileName: {FileName}, Format: {Format}, DiagnosticCount: {DiagnosticCount}, Diagnostics: {@Diagnostics}",
            fileName,
            format,
            diagnostics.Count,
            diagnostics.Select(diagnostic => new
            {
                diagnostic.Code,
                LossKind = diagnostic.LossKind.ToString(),
                diagnostic.Source,
                diagnostic.Message,
                diagnostic.Location
            }).ToArray());

        var rendererFallbackDiagnostics = diagnostics
            .Where(IsHeaderFooterZoneOverlapDiagnostic)
            .ToArray();
        if (rendererFallbackDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "OfficeIMO conversion rejected a renderer-specific layout diagnostic; allowing renderer fallback - FileName: {FileName}, Format: {Format}, DiagnosticCodes: {DiagnosticCodes}",
                fileName,
                format,
                rendererFallbackDiagnostics.Select(diagnostic => diagnostic.Code).ToArray());
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
        }

        var potentiallyMaterial = diagnostics
            .Where(diagnostic => diagnostic.LossKind is not
                OfficeConversionLossKind.None and not
                OfficeConversionLossKind.Approximation)
            .ToArray();
        if (potentiallyMaterial.Length == 0) return;

        _logger.LogError(
            "Document conversion rejected potential material content loss - FileName: {FileName}, Format: {Format}, DiagnosticCodes: {DiagnosticCodes}",
            fileName,
            format,
            potentiallyMaterial.Select(diagnostic => diagnostic.Code).ToArray());
        throw new DocumentFidelityRejectedException(ConversionErrorMessage);
    }

    internal static DocumentDiagnosticImpact ClassifyXlsbDiagnostic(string code)
    {
        if (CosmeticXlsbDiagnosticCodes.Contains(code)) return DocumentDiagnosticImpact.Cosmetic;
        if (MaterialXlsbDiagnosticCodes.Contains(code)) return DocumentDiagnosticImpact.Material;
        return DocumentDiagnosticImpact.Unknown;
    }

    internal static DocumentDiagnosticImpact ClassifyImageDiagnostic(string code) =>
        NonLossImageDiagnosticCodes.Contains(code)
            ? DocumentDiagnosticImpact.NoContentLoss
            : DocumentDiagnosticImpact.Unknown;

    internal static bool IsFatalRtfDiagnostic(RtfDiagnosticSeverity severity) =>
        severity == RtfDiagnosticSeverity.Error;

    internal static bool IsFatalXlsbDiagnostic(XlsbImportDiagnosticSeverity severity) =>
        severity == XlsbImportDiagnosticSeverity.Error;

    private static bool IsHeaderFooterZoneOverlapDiagnostic(
        OfficeConversionFidelityDiagnostic diagnostic)
    {
        if (string.Equals(
                diagnostic.Code?.Trim(),
                HeaderFooterZoneOverlapDiagnosticCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
                diagnostic.Source?.Trim(),
                "OfficeIMO.Word.Pdf",
                StringComparison.OrdinalIgnoreCase) &&
            diagnostic.Message?.Contains(
                "Header zones overlap",
                StringComparison.OrdinalIgnoreCase) == true;
    }

    private static object[] FormatXlsbDiagnostics(IEnumerable<XlsbImportDiagnostic> diagnostics) =>
        diagnostics.Select(diagnostic => (object)new
        {
            diagnostic.Code,
            Severity = diagnostic.Severity.ToString(),
            diagnostic.Message,
            diagnostic.PartName,
            diagnostic.RecordType,
            diagnostic.RecordOffset
        }).ToArray();

    private static object[] FormatRtfDiagnostics(IEnumerable<RtfDiagnostic> diagnostics) =>
        diagnostics.Select(diagnostic => (object)new
        {
            diagnostic.Code,
            Severity = diagnostic.Severity.ToString(),
            diagnostic.Message,
            diagnostic.Position
        }).ToArray();

    private static object[] FormatImageDiagnostics(IEnumerable<OfficeDocumentDiagnostic> diagnostics) =>
        diagnostics.Select(diagnostic => (object)new
        {
            diagnostic.Code,
            Severity = diagnostic.Severity.ToString(),
            diagnostic.Category,
            diagnostic.Message,
            diagnostic.Location
        }).ToArray();
}

internal enum DocumentDiagnosticImpact
{
    NoContentLoss,
    Cosmetic,
    Material,
    Unknown
}
