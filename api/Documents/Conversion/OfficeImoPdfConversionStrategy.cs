using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeIMO;
using OfficeIMO.Drawing;
using OfficeIMO.Excel;
using OfficeIMO.Excel.LegacyXls;
using OfficeIMO.Excel.Pdf;
using OfficeIMO.Excel.Xlsb;
using OfficeIMO.OpenDocument;
using OfficeIMO.OpenDocument.Odt.Pdf;
using OfficeIMO.Pdf;
using OfficeIMO.PowerPoint;
using OfficeIMO.PowerPoint.Pdf;
using OfficeIMO.Reader;
using OfficeIMO.Reader.Image;
using OfficeIMO.Rtf;
using OfficeIMO.Rtf.Diagnostics;
using OfficeIMO.Rtf.Pdf;
using OfficeIMO.Word;
using OfficeIMO.Word.LegacyDoc;
using OfficeIMO.Word.Pdf;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal sealed class OfficeImoPdfConversionStrategy : IDocumentPdfConversionStrategy
{
    private const string ConversionErrorMessage = DocumentPdfConversionService.ConversionErrorMessage;
    private const string HeaderFooterZoneOverlapDiagnosticCode =
        "HeaderFooterZoneOverlap";

    private readonly ILogger<OfficeImoPdfConversionStrategy> _logger;
    private readonly DocumentConversionOptions _options;
    private readonly OfficeDocumentReader _imageReader;

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

    public OfficeImoPdfConversionStrategy(
        ILogger<OfficeImoPdfConversionStrategy> logger,
        IOptions<DocumentConversionOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _imageReader = new OfficeDocumentReaderBuilder()
            .AddImageHandler(new ReaderImageOptions
            {
                IncludePayload = true,
                CreateOcrCandidate = false
            })
            .Build();
    }

    public DocumentPdfConverterId Id => DocumentPdfConverterId.OfficeIMO;

    public string Name => "OfficeIMO";

    public bool CanConvert(DocumentFormat format) => format is not
        DocumentFormat.Unknown and not DocumentFormat.EncryptedOffice and not DocumentFormat.Pdf;

    public async Task<DocumentPdfConversionAttempt> ConvertAsync(
        DocumentPdfConversionContext context,
        CancellationToken cancellationToken)
    {
        if (!CanConvert(context.Format))
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.Unsupported,
                new UnsupportedDocumentFormatException(ConversionErrorMessage));
        }

        try
        {
            var output = await ConvertWithOfficeImoAsync(
                context.Input,
                context.Format,
                context.FileName,
                cancellationToken);
            return DocumentPdfConversionAttempt.Succeeded(Name, output);
        }
        catch (UnsupportedDocumentFormatException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.Unsupported,
                exception);
        }
        catch (DocumentFidelityRejectedException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.FidelityRejected,
                exception);
        }
        catch (DocumentParsingRejectedException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.InvalidDocument,
                exception);
        }
        catch (OfficePackageSecurityException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.UnsafeDocument,
                exception);
        }
        catch (DocumentSizeLimitExceededException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                exception);
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or OpenXmlPackageException)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.InvalidDocument,
                exception);
        }
        catch (NotSupportedException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.ConverterIncompatible,
                exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                exception);
        }
    }

    private async Task<MemoryStream> ConvertWithOfficeImoAsync(
        MemoryStream input,
        DocumentFormat format,
        string fileName,
        CancellationToken cancellationToken)
    {
        // Some synchronous OfficeIMO package loaders cannot be interrupted. The permit and input
        // remain owned until they return; cancellation is enforced by subsequent projection/save APIs.
        var conversion = format switch
        {
            DocumentFormat.Doc => ConvertLegacyWord(input, fileName, cancellationToken),
            DocumentFormat.Docx or DocumentFormat.Docm or DocumentFormat.Dotx or DocumentFormat.Dotm =>
                ConvertWord(input, cancellationToken),
            DocumentFormat.Xls => ConvertLegacyExcel(input, fileName, cancellationToken),
            DocumentFormat.Xlsx or DocumentFormat.Xlsm or DocumentFormat.Xltx or DocumentFormat.Xltm or
                DocumentFormat.Xlsb => ConvertExcel(input, format, fileName, cancellationToken),
            DocumentFormat.Pptx or DocumentFormat.Pptm => ConvertPowerPoint(input, cancellationToken),
            DocumentFormat.Rtf => ConvertRtf(input, fileName, cancellationToken),
            DocumentFormat.Odt => await ConvertOdtAsync(input, cancellationToken),
            DocumentFormat.Png or DocumentFormat.Jpeg or DocumentFormat.Tiff or DocumentFormat.Webp =>
                ConvertImage(input, fileName, cancellationToken),
            _ => throw new UnsupportedDocumentFormatException(ConversionErrorMessage)
        };

        cancellationToken.ThrowIfCancellationRequested();
        var output = new BoundedMemoryStream(_options.MaxOutputSizeBytes, ConversionErrorMessage);
        try
        {
            var saveResult = await conversion.SaveAsync(output, cancellationToken);
            if (!saveResult.Succeeded)
            {
                _logger.LogError(
                    saveResult.Exception,
                    "OfficeIMO PDF serialization failed - FileName: {FileName}, Format: {Format}, BytesWritten: {BytesWritten}, Diagnostics: {@Diagnostics}",
                    fileName,
                    format,
                    saveResult.BytesWritten,
                    saveResult.Diagnostics);
                throw saveResult.Exception ?? new Exception("OfficeIMO PDF serialization failed.");
            }

            // PdfSaveResult aggregates source, projection, and final serialization fidelity
            // evidence, avoiding duplicate evaluation of diagnostics already propagated to save.
            ValidateFidelityDiagnostics(fileName, format, saveResult.FidelityDiagnostics.ToArray());

            if (output.Length == 0 || !PdfFileValidation.HasPdfSignature(output))
            {
                _logger.LogError(
                    "OfficeIMO produced an invalid PDF artifact - FileName: {FileName}, Format: {Format}, OutputBytes: {OutputBytes}",
                    fileName,
                    format,
                    output.Length);
                throw new UnprocessableDocumentException(ConversionErrorMessage);
            }

            output.Position = 0;
            return output;
        }
        catch (Exception exception) when (IsPdfTextEncodingPreflightException(exception))
        {
            await output.DisposeAsync();
            _logger.LogWarning(
                exception,
                "OfficeIMO PDF text encoding preflight rejected the rendered document; allowing renderer fallback - FileName: {FileName}, Format: {Format}",
                fileName,
                format);
            throw new DocumentFidelityRejectedException(ConversionErrorMessage, exception);
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }

    private PdfDocumentConversionResult ConvertLegacyWord(
        MemoryStream input,
        string fileName,
        CancellationToken cancellationToken)
    {
        var importOptions = new LegacyDocImportOptions
        {
            MaxInputBytes = (int)Math.Min(_options.MaxInputSizeBytes, int.MaxValue)
        };
        using var loadResult = LoadWithParsingFallback(
            () => WordDocument.LoadLegacyDocWithReport(input, importOptions));
        ValidateFidelityDiagnostics(fileName, DocumentFormat.Doc, loadResult.FidelityDiagnostics.ToArray());
        return loadResult.Document.ToPdfDocumentResult(CreateWordPdfOptions(), cancellationToken);
    }

    private PdfDocumentConversionResult ConvertLegacyExcel(
        MemoryStream input,
        string fileName,
        CancellationToken cancellationToken)
    {
        var importOptions = new LegacyXlsImportOptions
        {
            MaxInputBytes = (int)Math.Min(_options.MaxInputSizeBytes, int.MaxValue)
        };
        using var loadResult = LoadWithParsingFallback(
            () => ExcelDocument.LoadLegacyXlsWithReport(input, importOptions));
        ValidateFidelityDiagnostics(fileName, DocumentFormat.Xls, loadResult.FidelityDiagnostics.ToArray());
        return loadResult.Document.ToPdfDocumentResult(CreateExcelPdfOptions(), cancellationToken);
    }

    private PdfDocumentConversionResult ConvertWord(MemoryStream input, CancellationToken cancellationToken)
    {
        var packageSecurity = CreateUntrustedPackageSecurityOptions();
        var loadOptions = new WordLoadOptions
        {
            AccessMode = DocumentAccessMode.ReadOnly,
            MaxInputBytes = _options.MaxInputSizeBytes,
            PackageSecurity = packageSecurity
        };
        using var document = LoadWithParsingFallback(() => WordDocument.Load(input, loadOptions));
        return document.ToPdfDocumentResult(CreateWordPdfOptions(), cancellationToken);
    }

    private PdfDocumentConversionResult ConvertExcel(
        MemoryStream input,
        DocumentFormat format,
        string fileName,
        CancellationToken cancellationToken)
    {
        var packageSecurity = CreateUntrustedPackageSecurityOptions();
        var loadOptions = new ExcelLoadOptions
        {
            AccessMode = DocumentAccessMode.ReadOnly,
            MaxInputBytes = _options.MaxInputSizeBytes,
            PackageSecurity = packageSecurity
        };
        using var document = LoadWithParsingFallback(() => ExcelDocument.Load(input, loadOptions));
        if (format == DocumentFormat.Xlsb)
        {
            var diagnostics = document.XlsbImportDiagnostics.ToArray();
            var informationalDiagnostics = diagnostics
                .Where(diagnostic => diagnostic.Severity == XlsbImportDiagnosticSeverity.Information)
                .ToArray();
            if (informationalDiagnostics.Length > 0)
            {
                _logger.LogInformation(
                    "XLSB import produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                    fileName,
                    informationalDiagnostics.Select(diagnostic => new
                    {
                        diagnostic.Code,
                        Severity = diagnostic.Severity.ToString(),
                        diagnostic.Message,
                        diagnostic.PartName,
                        diagnostic.RecordType,
                        diagnostic.RecordOffset
                    }).ToArray());
            }

            var warningDiagnostics = diagnostics
                .Where(diagnostic => diagnostic.Severity == XlsbImportDiagnosticSeverity.Warning)
                .ToArray();
            if (warningDiagnostics.Length > 0)
            {
                _logger.LogWarning(
                    "XLSB import produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                    fileName,
                    warningDiagnostics.Select(diagnostic => new
                    {
                        diagnostic.Code,
                        Severity = diagnostic.Severity.ToString(),
                        diagnostic.Message,
                        diagnostic.PartName,
                        diagnostic.RecordType,
                        diagnostic.RecordOffset
                    }).ToArray());
            }

            var errorDiagnostics = diagnostics
                .Where(diagnostic => IsFatalXlsbDiagnostic(diagnostic.Severity))
                .ToArray();
            if (errorDiagnostics.Length > 0)
            {
                _logger.LogError(
                    "XLSB parsing was rejected - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                    fileName,
                    errorDiagnostics.Select(diagnostic => new
                    {
                        diagnostic.Code,
                        Severity = diagnostic.Severity.ToString(),
                        diagnostic.Message,
                        diagnostic.PartName,
                        diagnostic.RecordType,
                        diagnostic.RecordOffset
                    }).ToArray());
                throw new DocumentParsingRejectedException(ConversionErrorMessage);
            }

            var fidelityDiagnostics = diagnostics
                .Where(diagnostic => ClassifyXlsbDiagnostic(diagnostic.Code) is
                    DocumentDiagnosticImpact.Material or DocumentDiagnosticImpact.Unknown)
                .ToArray();
            if (fidelityDiagnostics.Length > 0)
            {
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
        }

        return document.ToPdfDocumentResult(CreateExcelPdfOptions(), cancellationToken);
    }

    private PdfDocumentConversionResult ConvertPowerPoint(MemoryStream input, CancellationToken cancellationToken)
    {
        var packageSecurity = CreateUntrustedPackageSecurityOptions();
        var loadOptions = new PowerPointLoadOptions
        {
            AccessMode = DocumentAccessMode.ReadOnly,
            MaxInputBytes = _options.MaxInputSizeBytes,
            PackageSecurity = packageSecurity
        };
        using var presentation = LoadWithParsingFallback(
            () => PowerPointPresentation.Load(input, loadOptions, cancellationToken));
        return presentation.ToPdfDocumentResult(CreatePowerPointPdfOptions(), cancellationToken);
    }

    private PdfDocumentConversionResult ConvertRtf(
        MemoryStream input,
        string fileName,
        CancellationToken cancellationToken)
    {
        var readOptions = RtfReadOptions.CreateOfficeIMOProfile();
        readOptions.MaxInputBytes = _options.MaxInputSizeBytes;
        var readResult = RtfDocument.LoadResult(input, readOptions, cancellationToken: cancellationToken);
        var parserDiagnostics = readResult.Diagnostics.ToArray();

        var informationalDiagnostics = parserDiagnostics
            .Where(diagnostic => diagnostic.Severity == RtfDiagnosticSeverity.Info)
            .ToArray();
        if (informationalDiagnostics.Length > 0)
        {
            _logger.LogInformation(
                "RTF parsing produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                informationalDiagnostics.Select(diagnostic => new
                {
                    diagnostic.Code,
                    Severity = diagnostic.Severity.ToString(),
                    diagnostic.Message,
                    diagnostic.Position
                }).ToArray());
        }

        var warningDiagnostics = parserDiagnostics
            .Where(diagnostic => diagnostic.Severity == RtfDiagnosticSeverity.Warning)
            .ToArray();
        if (warningDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "RTF parsing produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                warningDiagnostics.Select(diagnostic => new
                {
                    diagnostic.Code,
                    Severity = diagnostic.Severity.ToString(),
                    diagnostic.Message,
                    diagnostic.Position
                }).ToArray());
        }

        var errorDiagnostics = parserDiagnostics
            .Where(diagnostic => IsFatalRtfDiagnostic(diagnostic.Severity))
            .ToArray();
        if (errorDiagnostics.Length > 0)
        {
            _logger.LogError(
                "RTF parsing was rejected - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                errorDiagnostics.Select(diagnostic => new
                {
                    diagnostic.Code,
                    Severity = diagnostic.Severity.ToString(),
                    diagnostic.Message,
                    diagnostic.Position
                }).ToArray());
            throw new DocumentParsingRejectedException(ConversionErrorMessage);
        }

        return readResult.Document.ToPdfDocumentResult(new RtfToPdfOptions
        {
            ResourcePolicy = PdfResourcePolicy.CreateDefault()
        }, cancellationToken);
    }

    private async Task<PdfDocumentConversionResult> ConvertOdtAsync(
        MemoryStream input,
        CancellationToken cancellationToken)
    {
        var loadOptions = new OdfLoadOptions
        {
            MaxPackageBytes = _options.MaxInputSizeBytes,
            MaxEntryUncompressedBytes = _options.MaxOutputSizeBytes,
            MaxTotalUncompressedBytes = _options.MaxOutputSizeBytes
        };
        var document = await OdtDocument.LoadAsync(input, loadOptions, cancellationToken);
        return document.ToPdfDocumentResult(
            pdfOptions: CreateWordPdfOptions(),
            cancellationToken: cancellationToken);
    }

    private PdfDocumentConversionResult ConvertImage(
        MemoryStream input,
        string fileName,
        CancellationToken cancellationToken)
    {
        var readResult = _imageReader.ReadDocument(
            input,
            fileName,
            new ReaderOptions
            {
                MaxInputBytes = _options.MaxInputSizeBytes,
                // JASPER has already validated the file signature against the declared
                // extension. Avoid a second, heuristic detection pass misrouting binary
                // image content through the Reader text fallback.
                DetectionMode = ReaderDetectionMode.ExtensionOnly
            },
            cancellationToken);
        var readerDiagnostics = readResult.Diagnostics.ToArray();

        var informationalDiagnostics = readerDiagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Information)
            .ToArray();
        if (informationalDiagnostics.Length > 0)
        {
            _logger.LogInformation(
                "Image reading produced informational diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(informationalDiagnostics));
        }

        var warningDiagnostics = readerDiagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Warning)
            .ToArray();
        if (warningDiagnostics.Length > 0)
        {
            _logger.LogWarning(
                "Image reading produced warning diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(warningDiagnostics));
        }

        var fidelityDiagnostics = readerDiagnostics
            .Where(diagnostic => diagnostic.Severity is not OfficeDocumentDiagnosticSeverity.Information)
            .Where(diagnostic => ClassifyImageDiagnostic(diagnostic.Code) is not DocumentDiagnosticImpact.NoContentLoss)
            .ToArray();
        if (fidelityDiagnostics.Length > 0)
        {
            _logger.LogError(
                "Image conversion rejected potential material content loss - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(fidelityDiagnostics));
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
        }

        var errorDiagnostics = readerDiagnostics
            .Where(diagnostic => diagnostic.Severity == OfficeDocumentDiagnosticSeverity.Error)
            .ToArray();
        if (errorDiagnostics.Length > 0)
        {
            _logger.LogError(
                "Image conversion rejected fatal reader diagnostics - FileName: {FileName}, Diagnostics: {@Diagnostics}",
                fileName,
                FormatImageDiagnostics(errorDiagnostics));
            throw new UnprocessableDocumentException(ConversionErrorMessage);
        }

        var imageAssets = readResult.Assets
            .Where(asset =>
                asset.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true &&
                asset.PayloadBytes is { Length: > 0 })
            .ToArray();
        if (imageAssets.Length == 0)
        {
            _logger.LogError(
                "Image conversion produced no materializable image asset - FileName: {FileName}, AssetCount: {AssetCount}, Assets: {@Assets}",
                fileName,
                readResult.Assets.Count,
                readResult.Assets.Select(asset => new
                {
                    asset.Kind,
                    asset.MediaType,
                    PayloadLength = asset.PayloadBytes?.Length ?? 0
                }).ToArray());
            throw new UnprocessableDocumentException(ConversionErrorMessage);
        }

        var imageSources = imageAssets.Select(asset =>
            new PdfImageDocumentSource(
                PrepareImageForPdf(asset.PayloadBytes, fileName, cancellationToken),
                fileName));
        var document = PdfDocument.CreateFromImages(
            imageSources,
            new PdfImageDocumentOptions
            {
                MaximumEncodedImageBytes = _options.MaxInputSizeBytes,
                FixedPageSize = PageSizes.A4
            },
            cancellationToken);

        return new PdfDocumentConversionResult(document, new PdfConversionReport());
    }

    private byte[] PrepareImageForPdf(
        byte[] imageBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (PdfDocument.TryPrepareImageBytes(
                imageBytes,
                out var preparedBytes,
                out _,
                out _,
                out var unsupportedReason))
        {
            return preparedBytes;
        }

        if (unsupportedReason?.Contains("ICC profile", StringComparison.OrdinalIgnoreCase) != true)
        {
            _logger.LogError(
                "Image conversion rejected unsupported raster payload - FileName: {FileName}, Reason: {Reason}",
                fileName,
                unsupportedReason);
            throw new UnprocessableDocumentException(ConversionErrorMessage);
        }

        var decodeOptions = new OfficeRasterDecodeOptions
        {
            MaximumEncodedBytes = checked((int)Math.Min(_options.MaxInputSizeBytes, int.MaxValue)),
            CancellationToken = cancellationToken
        };
        if (!OfficeRasterImageDecoder.TryDecode(
                imageBytes,
                decodeOptions,
                out var raster,
                out var decodeInfo) || raster is null)
        {
            _logger.LogWarning(
                "OfficeIMO could not normalize an unsupported ICC profile; allowing converter fallback - FileName: {FileName}, Reason: {Reason}, DecodeDiagnostic: {DecodeDiagnostic}",
                fileName,
                unsupportedReason,
                decodeInfo.Diagnostic);
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
        }

        _logger.LogWarning(
            "Image conversion removed an unsupported ICC profile during PDF normalization - FileName: {FileName}, SourceFormat: {SourceFormat}, AdditionalFramesDiscarded: {AdditionalFramesDiscarded}, DecodeDiagnostic: {DecodeDiagnostic}",
            fileName,
            decodeInfo.Format,
            decodeInfo.FramesOrPagesDiscarded,
            decodeInfo.Diagnostic);

        if (decodeInfo.FramesOrPagesDiscarded)
        {
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
        }

        return OfficeRasterImageEncoder.Encode(
            raster,
            OfficeImageExportFormat.Png,
            new OfficeRasterEncodingOptions(),
            _options.MaxOutputSizeBytes,
            cancellationToken);
    }

    private static object[] FormatImageDiagnostics(IEnumerable<OfficeDocumentDiagnostic> diagnostics) =>
        diagnostics.Select(diagnostic => (object)new
        {
            diagnostic.Code,
            Severity = diagnostic.Severity.ToString(),
            diagnostic.Category,
            diagnostic.Message,
            diagnostic.Location
        }).ToArray();

    private void ValidateFidelityDiagnostics(
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

    private static T LoadWithParsingFallback<T>(Func<T> load)
    {
        try
        {
            return load();
        }
        catch (Exception exception) when (
            IsKnownOfficeImoParserException(exception) &&
            !ContainsSecurityBoundaryException(exception))
        {
            throw new DocumentParsingRejectedException(ConversionErrorMessage, exception);
        }
    }

    private static bool IsKnownOfficeImoParserException(Exception exception) =>
        exception is WordConversionException or
            ExcelDocumentConversionException or
            PowerPointPresentationConversionException;

    private static bool IsPdfTextEncodingPreflightException(Exception exception) =>
        string.Equals(
            exception.GetType().FullName,
            "OfficeIMO.Pdf.PdfTextEncodingPreflightException",
            StringComparison.Ordinal);

    private static bool ContainsSecurityBoundaryException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OfficePackageSecurityException or
                OdfEncryptedPackageException or
                DocumentSizeLimitExceededException)
            {
                return true;
            }
        }

        return false;
    }

    private OfficePackageSecurityOptions CreateUntrustedPackageSecurityOptions()
    {
        var options = OfficePackageSecurityOptions.UntrustedDefaults;
        options.MaxPackageBytes = _options.MaxInputSizeBytes;
        // OfficeIMO inventories macro payloads without executing them. Allow is required to
        // read macro-enabled formats; ActiveX is not required for viewing and remains rejected.
        options.Macros = OfficePackageContentPolicy.Allow;
        options.ActiveX = OfficePackageContentPolicy.Reject;
        options.EmbeddedPayloads = OfficePackageContentPolicy.Reject;
        // Preserve relationship metadata. PdfResourcePolicy.CreateDefault prevents the
        // renderers from retrieving arbitrary local or remote relationship targets.
        options.ExternalRelationships = OfficePackageContentPolicy.Allow;
        return options;
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

    internal static string NormalizeExtension(string extension)
    {
        var normalized = extension?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized[0] != '.') normalized = $".{normalized}";
        return normalized.Length is > 1 and <= 16 && normalized.Skip(1).All(char.IsLetterOrDigit)
            ? normalized
            : null;
    }

    private static WordToPdfOptions CreateWordPdfOptions()
    {
        var pdfOptions = new PdfOptions();
        TryRegisterFontFamilySubstitution(pdfOptions, "Calibri", "Carlito");
        TryRegisterFontFamilySubstitution(pdfOptions, "Arial", "Liberation Sans");
        TryRegisterFontFamilySubstitution(pdfOptions, "Arial Narrow", "Liberation Sans Narrow");

        return new WordToPdfOptions
        {
            PdfOptions = pdfOptions,
            ResourcePolicy = PdfResourcePolicy.CreateDefault()
        }.UseProfile(PdfExportProfile.Faithful);
    }

    private static void TryRegisterFontFamilySubstitution(
        PdfOptions options,
        string sourceFamily,
        string targetFamily)
    {
        if (!PdfEmbeddedFontFamily.TryFromSystem(targetFamily, out var embeddedFamily) ||
            embeddedFamily is null)
        {
            return;
        }

        options.RegisterNamedFontFamily(embeddedFamily)
            .RegisterFontFamilySubstitution(
                sourceFamily,
                targetFamily,
                PdfFontFamilySubstitutionImpact.Compatible);
    }

    private static ExcelToPdfOptions CreateExcelPdfOptions()
    {
        var options = new ExcelToPdfOptions
        {
            ResourcePolicy = PdfResourcePolicy.CreateDefault()
        }.UseProfile(PdfExportProfile.Faithful);

        options.WorksheetLayout = ExcelPdfWorksheetLayoutMode.WorksheetCanvas;
        options.UseWorksheetPageSetup = true;
        options.UseWorksheetCellStyles = true;
        options.UseWorksheetColumnWidths = true;
        options.UseWorksheetRowHeights = true;
        return options;
    }

    private static PowerPointToPdfOptions CreatePowerPointPdfOptions() => new PowerPointToPdfOptions
    {
        ResourcePolicy = PdfResourcePolicy.CreateDefault()
    }.UseProfile(PdfExportProfile.Faithful);
}

internal enum DocumentDiagnosticImpact
{
    NoContentLoss,
    Cosmetic,
    Material,
    Unknown
}
