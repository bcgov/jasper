using System;
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

    private readonly ILogger<OfficeImoPdfConversionStrategy> _logger;
    private readonly DocumentConversionOptions _options;
    private readonly OfficeDocumentReader _imageReader;
    private readonly OfficeImoDiagnosticPolicy _diagnosticPolicy;

    public OfficeImoPdfConversionStrategy(
        ILogger<OfficeImoPdfConversionStrategy> logger,
        IOptions<DocumentConversionOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _diagnosticPolicy = new OfficeImoDiagnosticPolicy(logger);
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                ClassifyFailure(exception),
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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var writeResult = await PdfOutputWriter.WriteAsync(
                conversion,
                _options.MaxOutputSizeBytes,
                ConversionErrorMessage,
                cancellationToken,
                saveResult => _diagnosticPolicy.ValidateFidelity(
                    fileName,
                    format,
                    saveResult.FidelityDiagnostics.ToArray()));
            if (writeResult.Failure == PdfOutputFailure.SerializationFailed)
            {
                _logger.LogError(
                    writeResult.SaveResult.Exception,
                    "OfficeIMO PDF serialization failed - FileName: {FileName}, Format: {Format}, BytesWritten: {BytesWritten}, Diagnostics: {@Diagnostics}",
                    fileName,
                    format,
                    writeResult.SaveResult.BytesWritten,
                    writeResult.SaveResult.Diagnostics);
                throw writeResult.SaveResult.Exception ?? new Exception("OfficeIMO PDF serialization failed.");
            }

            if (!writeResult.Succeeded)
            {
                _logger.LogError(
                    "OfficeIMO produced an invalid PDF artifact - FileName: {FileName}, Format: {Format}, OutputBytes: {OutputBytes}, Failure: {Failure}",
                    fileName,
                    format,
                    writeResult.OutputBytes,
                    writeResult.Failure);
                throw new UnprocessableDocumentException(ConversionErrorMessage);
            }

            return writeResult.Output;
        }
        catch (Exception exception) when (IsPdfTextEncodingPreflightException(exception))
        {
            _logger.LogWarning(
                exception,
                "OfficeIMO PDF text encoding preflight rejected the rendered document; allowing renderer fallback - FileName: {FileName}, Format: {Format}",
                fileName,
                format);
            throw new DocumentFidelityRejectedException(ConversionErrorMessage, exception);
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
        _diagnosticPolicy.ValidateFidelity(
            fileName,
            DocumentFormat.Doc,
            loadResult.FidelityDiagnostics.ToArray());
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
        _diagnosticPolicy.ValidateFidelity(
            fileName,
            DocumentFormat.Xls,
            loadResult.FidelityDiagnostics.ToArray());
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
            _diagnosticPolicy.ValidateXlsb(fileName, document.XlsbImportDiagnostics.ToArray());
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
        _diagnosticPolicy.ValidateRtf(fileName, readResult.Diagnostics.ToArray());

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
        cancellationToken.ThrowIfCancellationRequested();
        input.Position = 0;
        var metadata = ImageConversionPreflight.InspectAndValidate(input.ToArray(), _options);
        input.Position = 0;

        // OfficeIMO does not expose a hard in-process memory or temporary-disk ceiling for its
        // raster decoder. Metadata-only preflight bounds dimensions, frames, and decoded pixels
        // before that decoder runs; the global conversion permit and timeout remain defense in depth.
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
        _diagnosticPolicy.ValidateImage(fileName, readResult.Diagnostics.ToArray());

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

        if (imageAssets.Length != metadata.FrameCount)
        {
            _logger.LogError(
                "Image conversion rejected frame loss - FileName: {FileName}, SourceFrames: {SourceFrames}, MaterializedFrames: {MaterializedFrames}",
                fileName,
                metadata.FrameCount,
                imageAssets.Length);
            throw new DocumentFidelityRejectedException(ConversionErrorMessage);
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
        => OfficeImoDiagnosticPolicy.ClassifyXlsbDiagnostic(code);

    internal static DocumentDiagnosticImpact ClassifyImageDiagnostic(string code) =>
        OfficeImoDiagnosticPolicy.ClassifyImageDiagnostic(code);

    internal static bool IsFatalRtfDiagnostic(RtfDiagnosticSeverity severity) =>
        OfficeImoDiagnosticPolicy.IsFatalRtfDiagnostic(severity);

    internal static bool IsFatalXlsbDiagnostic(XlsbImportDiagnosticSeverity severity) =>
        OfficeImoDiagnosticPolicy.IsFatalXlsbDiagnostic(severity);

    private static DocumentPdfConversionAttemptStatus ClassifyFailure(Exception exception) =>
        exception switch
        {
            UnsupportedDocumentFormatException => DocumentPdfConversionAttemptStatus.Unsupported,
            DocumentFidelityRejectedException => DocumentPdfConversionAttemptStatus.FidelityRejected,
            DocumentParsingRejectedException => DocumentPdfConversionAttemptStatus.InvalidDocument,
            OfficePackageSecurityException => DocumentPdfConversionAttemptStatus.UnsafeDocument,
            DocumentSizeLimitExceededException => DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
            InvalidDataException or FormatException or OpenXmlPackageException =>
                DocumentPdfConversionAttemptStatus.InvalidDocument,
            NotSupportedException => DocumentPdfConversionAttemptStatus.ConverterIncompatible,
            _ => DocumentPdfConversionAttemptStatus.ConverterExecutionFailed
        };

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
