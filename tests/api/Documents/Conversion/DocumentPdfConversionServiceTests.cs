using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OfficeIMO.Drawing;
using OfficeIMO.Excel;
using OfficeIMO.Excel.Xlsb;
using OfficeIMO.OpenDocument;
using OfficeIMO.PowerPoint;
using OfficeIMO.Reader;
using OfficeIMO.Reader.Pdf;
using OfficeIMO.Rtf.Diagnostics;
using OpenMcdf;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Core.Exceptions;
using Scv.Models.Configuration;
using Xunit;
using ConversionDocumentFormat = Scv.Api.Documents.Conversion.DocumentFormat;

namespace tests.api.Documents.Conversion;

public class DocumentPdfConversionServiceTests
{
    private const string SafeMessage = "This document format could not be converted for viewing.";

    public static TheoryData<string> SupportedExtensions => new()
    {
        ".pdf", ".doc", ".docx", ".docm", ".dotx", ".dotm",
        ".xlsx", ".xlsm", ".xltx", ".xltm", ".xls", ".xlsb",
        ".pptx", ".pptm", ".rtf", ".odt", ".png", ".jpg", ".jpeg",
        ".tiff", ".webp"
    };

    [Theory]
    [MemberData(nameof(SupportedExtensions))]
    public async Task ConvertToPdfAsync_RecognizesEveryConfiguredExtensionCaseInsensitively_BeforeRejectingMalformedContent(
        string extension)
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream("not a valid document"u8.ToArray());

        var exception = await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input,
                $"document{extension.ToUpperInvariant()}", "safe-id")));

        Assert.Equal(SafeMessage, exception.Message);
    }

    [Fact]
    public async Task ConvertToPdfAsync_PassesPdfThroughWithoutRerendering_AndRewindsOutput()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\npass-through");
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(bytes);
        input.Position = input.Length;

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.PDF", "safe-id"));

        Assert.Equal(0, output.Position);
        Assert.Equal(bytes, output.ToArray());
        Assert.True(input.CanRead);
    }

    [Fact]
    public async Task ConvertToPdfAsync_ConvertsRtfToNonemptyRewoundPdf()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(
            @"{\rtf1\ansi Meaningful conversion text\par Second paragraph.}"));

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.rtf", "safe-id"));

        Assert.Equal(0, output.Position);
        Assert.True(output.Length > 5);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
        AssertPdfContains(output, "Meaningful conversion text");
    }

    [Fact]
    public async Task ConvertToPdfAsync_PassesValidPdfThroughByteForByte()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var rtf = new MemoryStream(Encoding.ASCII.GetBytes(
            @"{\rtf1\ansi Valid PDF pass-through evidence.}"));
        await using var generatedPdf = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(rtf, "source.rtf", "safe-id"));
        var expected = generatedPdf.ToArray();
        using var input = new MemoryStream(expected);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.pdf", "safe-id"));

        Assert.Equal(expected, output.ToArray());
        AssertPdfContains(output, "Valid PDF pass-through evidence");
    }

    [Fact]
    public async Task ConvertToPdfAsync_PreservesMeaningfulOdtText()
    {
        const string meaningfulText = "ODT conversion evidence 9184";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = await CreateOdtAsync(meaningfulText);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.odt", "safe-id"));

        AssertPdfContains(output, meaningfulText);
    }

    [Fact]
    public async Task ConvertToPdfAsync_PreservesMeaningfulWordText()
    {
        const string meaningfulText = "Meaningful conversion evidence 4729";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocx(meaningfulText);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(
                input,
                "document.docx", "safe-id"));

        var pdfReader = new OfficeDocumentReaderBuilder().AddPdfHandler().Build();
        var convertedDocument = pdfReader.ReadDocument(
            output,
            "converted.pdf",
            new ReaderOptions { MaxInputBytes = 10 * 1024 * 1024 });

        Assert.NotEmpty(convertedDocument.Search(meaningfulText).Hits);
    }

    [Fact]
    public async Task ConvertToPdfAsync_ConvertsMacroEnabledWordDocumentWithMacroPayloadInert()
    {
        const string meaningfulText = "Macro-enabled document text 7251";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocmWithMacroPayload(meaningfulText);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.docm", "safe-id"));

        AssertPdfContains(output, meaningfulText);
    }

    [Fact]
    public async Task ConvertToPdfAsync_AllowsHttpHyperlinkRelationshipsWithoutResolvingThem()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocxWithExternalRelationship();

        await using var output = await service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
            input, "document.docx", "safe-id"));

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Fact]
    public async Task ConvertToPdfAsync_AllowsExternalResourcesWithoutResolvingThem()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocxWithExternalTemplateRelationship();

        await using var output = await service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
            input, "document.docx", "safe-id"));

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsFileHyperlinkRelationships()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocxWithExternalRelationship("file:///server/share/document.docx");

        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.docx", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsEmbeddedPackagePayloads()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateDocxWithEmbeddedPackage();

        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.docx", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsActiveXPayloads()
    {
        var fallback = new Mock<IDocumentPdfConversionStrategy>();
        fallback.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter, fallbackStrategies: [fallback.Object]);
        using var input = CreateDocxWithActiveXPayload();

        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.docx", "safe-id")));

        fallback.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(RtfDiagnosticSeverity.Info, false)]
    [InlineData(RtfDiagnosticSeverity.Warning, false)]
    [InlineData(RtfDiagnosticSeverity.Error, true)]
    public void RtfDiagnosticSeverity_OnlyErrorsRejectPrimaryConversion(
        RtfDiagnosticSeverity severity,
        bool expected)
    {
        Assert.Equal(expected, OfficeImoPdfConversionStrategy.IsFatalRtfDiagnostic(severity));
    }

    [Theory]
    [InlineData(XlsbImportDiagnosticSeverity.Information, false)]
    [InlineData(XlsbImportDiagnosticSeverity.Warning, false)]
    [InlineData(XlsbImportDiagnosticSeverity.Error, true)]
    public void XlsbDiagnosticSeverity_OnlyErrorsRejectPrimaryConversion(
        XlsbImportDiagnosticSeverity severity,
        bool expected)
    {
        Assert.Equal(expected, OfficeImoPdfConversionStrategy.IsFatalXlsbDiagnostic(severity));
    }

    [Fact]
    public async Task ConvertToPdfAsync_ConvertsPngThroughTheImageRoute()
    {
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(png);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "pixel.png", "safe-id"));

        Assert.Equal(0, output.Position);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Fact]
    public async Task ConvertToPdfAsync_NormalizesUnsupportedTiffIccProfileAndFitsPage()
    {
        var tiff = AddUnsupportedIccProfile(OfficeTiffCodec.Encode(
            new OfficeRasterImage(1600, 400, OfficeColor.White),
            new OfficeTiffEncodeOptions()));
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(tiff);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "wide.tiff", "safe-id"));

        Assert.Equal(0, output.Position);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Fact]
    public async Task ConvertToPdfAsync_NormalizesFourComponentJpegWithEmbeddedIccProfile()
    {
        using var image = new MagickImage(MagickColors.Red, 8, 8);
        Assert.True(image.TransformColorSpace(ColorProfiles.SRGB, ColorProfiles.USWebCoatedSWOP));
        var jpeg = image.ToByteArray(MagickFormat.Jpeg);
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(jpeg);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "cmyk.jpg", "safe-id"));

        Assert.Equal(0, output.Position);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Fact]
    public async Task ConvertToPdfAsync_PreservesMeaningfulWorksheetText()
    {
        const string meaningfulText = "Worksheet evidence 5831";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateXlsx(meaningfulText);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "workbook.xlsx", "safe-id"));

        AssertPdfContains(output, meaningfulText);
    }

    [Fact]
    public async Task ConvertToPdfAsync_PreservesMeaningfulSlideText()
    {
        const string meaningfulText = "Slide evidence 6942";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreatePptx(meaningfulText);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "slides.pptx", "safe-id"));

        AssertPdfContains(output, meaningfulText);
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData(".docm")]
    [InlineData(".dotx")]
    [InlineData(".dotm")]
    public async Task ConvertToPdfAsync_ConvertsEveryOpenXmlWordVariant(string extension)
    {
        var text = $"Word variant evidence {extension}";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateWordVariant(text, extension);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, $"document{extension}", "safe-id"));

        AssertPdfContains(output, text);
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xlsm")]
    [InlineData(".xltx")]
    [InlineData(".xltm")]
    public async Task ConvertToPdfAsync_ConvertsEveryOpenXmlExcelVariant(string extension)
    {
        var text = $"Excel variant evidence {extension}";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateExcelVariant(text, extension);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, $"workbook{extension}", "safe-id"));

        AssertPdfContains(output, text);
    }

    [Theory]
    [InlineData(".pptx")]
    [InlineData(".pptm")]
    public async Task ConvertToPdfAsync_ConvertsEveryOpenXmlPowerPointVariant(string extension)
    {
        var text = $"PowerPoint variant evidence {extension}";
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreatePowerPointVariant(text, extension);

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, $"slides{extension}", "safe-id"));

        AssertPdfContains(output, text);
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsUnsupportedExtensionWith415Exception()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream("content"u8.ToArray());

        var exception = await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.txt", "safe-id")));

        Assert.Equal(SafeMessage, exception.Message);
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsExtensionDisabledInMongoDb()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter, enabledExtensions: [".pdf"]);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(@"{\rtf1\ansi text}"));

        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.rtf", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsMongoDbEnabledExtensionWithoutRegisteredConverter()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter, enabledExtensions: [".txt"]);
        using var input = new MemoryStream("meaningful text"u8.ToArray());

        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.txt", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_FailsClosedWhenMongoDbConfigurationIsMissing()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter, enabledExtensions: []);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));

        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.pdf", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_NormalizesAndIgnoresMalformedMongoDbExtensions()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(
            limiter: limiter,
            enabledExtensions: [" ", "pdf", "..pdf", "  .PDF  ", ".PDF"]);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));

        await using var output = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(input, "document.pdf", "safe-id"));

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(output.ToArray(), 0, 5));
    }

    [Theory]
    [InlineData(" pdf ", ".pdf")]
    [InlineData(".PDF", ".pdf")]
    [InlineData("docx", ".docx")]
    [InlineData("..pdf", null)]
    [InlineData(".doc/x", null)]
    [InlineData(" ", null)]
    public void NormalizeExtension_IsDeterministic(string input, string expected)
    {
        Assert.Equal(expected, DocumentPdfConversionService.NormalizeExtension(input));
    }

    [Theory]
    [InlineData("XLSB-PAGESETUP-FIRST-PAGE-PRESERVED", "Cosmetic")]
    [InlineData("XLSB-RECORDS-PRESERVED", "Material")]
    [InlineData("XLSB-DEFINED-NAME-PRESERVED", "Material")]
    [InlineData("XLSB-AUTOFILTER-PARTIAL", "Material")]
    [InlineData("XLSB-FUTURE-DIAGNOSTIC", "Unknown")]
    public void ClassifyXlsbDiagnostic_UsesContentImpactRatherThanSeverity(
        string code,
        string expected)
    {
        Assert.Equal(expected, OfficeImoPdfConversionStrategy.ClassifyXlsbDiagnostic(code).ToString());
    }

    [Theory]
    [InlineData("IMAGE_SOURCE_DECODED_BY_CALLER_CODEC", "NoContentLoss")]
    [InlineData("IMAGE_SOURCE_DECODE_FALLBACK", "Unknown")]
    [InlineData("IMAGE_FUTURE_DIAGNOSTIC", "Unknown")]
    public void ClassifyImageDiagnostic_FailsClosedExceptForVerifiedNonLossCode(
        string code,
        string expected)
    {
        Assert.Equal(expected, OfficeImoPdfConversionStrategy.ClassifyImageDiagnostic(code).ToString());
    }

    [Fact]
    public async Task ConvertToPdfAsync_UsesChangedOperationalConfiguration()
    {
        var currentValues = new[] { ".pdf" };
        var configurationService = new Mock<IConfigurationService>();
        configurationService
            .Setup(service => service.GetConfigurationAsync())
            .ReturnsAsync(() =>
            [
                new ConstantDto
                {
                    Key = ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions,
                    Values = [.. currentValues]
                }
            ]);
        using var limiter = CreateLimiter();
        var service = CreateService(
            limiter: limiter,
            configurationService: configurationService.Object);
        using var firstInput = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));

        await using var firstOutput = await service.ConvertToPdfAsync(
            new DocumentPdfConversionRequest(firstInput, "document.pdf", "safe-id"));

        currentValues = [".rtf"];
        using var secondInput = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));
        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                secondInput, "document.pdf", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsExtensionContentMismatchWith415Exception()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));

        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.docx", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsCorruptAllowlistedDocumentWith422Exception()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream("corrupt"u8.ToArray());

        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.xlsx", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsEncryptedOfficeContainerWith422Exception()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = CreateEncryptedOfficeContainer();

        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.docx", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_RejectsInputOverConfiguredLimitWith413Exception()
    {
        var options = new DocumentConversionOptions
        {
            MaxInputSizeBytes = 4,
            MaxOutputSizeBytes = 100,
            MaxPageCount = 10,
            MaxConcurrentConversions = 1
        };
        using var limiter = CreateLimiter(options);
        var service = CreateService(options, limiter);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7"));

        await Assert.ThrowsAsync<DocumentSizeLimitExceededException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.pdf", "safe-id")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_PropagatesCancellation()
    {
        using var limiter = CreateLimiter();
        var service = CreateService(limiter: limiter);
        using var input = new MemoryStream(Encoding.ASCII.GetBytes(@"{\rtf1\ansi text}"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ConvertToPdfAsync(
                new DocumentPdfConversionRequest(input, "document.rtf", "safe-id"),
                cancellation.Token));
    }

    [Fact]
    public async Task ConvertToPdfAsync_PdfPassThroughDoesNotConsumeConversionCapacity()
    {
        var options = new DocumentConversionOptions
        {
            MaxInputSizeBytes = 100,
            MaxOutputSizeBytes = 100,
            MaxPageCount = 10,
            MaxConcurrentConversions = 1
        };
        using var limiter = CreateLimiter(options);
        var service = CreateService(options, limiter);
        using var firstInput = new BlockingMemoryStream("%PDF-1.7\npass-through"u8.ToArray());
        using var secondInput = new ReadTrackingMemoryStream("corrupt-two"u8.ToArray());

        var first = service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
            firstInput, "first.pdf", "first"));
        await firstInput.ReadStarted.Task;

        var second = service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
            secondInput, "second.rtf", "second"));
        await Task.Delay(25);
        Assert.True(secondInput.ReadCount > 0);

        firstInput.AllowRead.TrySetResult(true);
        await using var firstOutput = await first;
        await Assert.ThrowsAsync<UnprocessableDocumentException>(() => second);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(firstOutput.ToArray(), 0, 5));
    }

    [Fact]
    public void BoundedMemoryStream_RejectsWriteBeforeExceedingConfiguredOutputLimit()
    {
        using var output = new BoundedMemoryStream(4, SafeMessage);
        output.Write([1, 2, 3, 4]);

        var exception = Assert.Throws<DocumentSizeLimitExceededException>(() => output.WriteByte(5));

        Assert.Equal(SafeMessage, exception.Message);
        Assert.Equal(4, output.Length);
    }

    [Fact]
    public async Task BoundedMemoryStream_AllowsExactLimitAcrossRepeatedSynchronousAndAsynchronousWrites()
    {
        using var output = new BoundedMemoryStream(6, SafeMessage);
        output.Write([1, 2]);
        await output.WriteAsync(new byte[] { 3, 4 }, 0, 2, CancellationToken.None);
        await output.WriteAsync(new ReadOnlyMemory<byte>([5, 6]), CancellationToken.None);

        Assert.Equal(6, output.Length);
        Assert.Throws<DocumentSizeLimitExceededException>(() => output.Write([7]));
        Assert.Equal(6, output.Length);
    }

    [Fact]
    public void BoundedMemoryStream_RejectsLengthAndCapacityAboveLimit()
    {
        using var output = new BoundedMemoryStream(4, SafeMessage);

        Assert.Throws<DocumentSizeLimitExceededException>(() => output.SetLength(5));
        Assert.Throws<DocumentSizeLimitExceededException>(() => output.Capacity = 5);
        output.SetLength(4);

        Assert.Equal(4, output.Length);
    }

    [Fact]
    public void BoundedMemoryStream_GrowsGeometricallyAndPreservesRepeatedSmallWrites()
    {
        using var output = new BoundedMemoryStream(4096, SafeMessage);
        var capacityChanges = 0;
        var previousCapacity = output.Capacity;

        for (var value = 0; value < 1000; value++)
        {
            output.WriteByte((byte)(value % 251));
            if (output.Capacity != previousCapacity)
            {
                capacityChanges++;
                previousCapacity = output.Capacity;
            }
        }

        Assert.True(capacityChanges <= 4, $"Expected geometric growth but observed {capacityChanges} expansions.");
        Assert.Equal(1000, output.Length);
        Assert.Equal(
            Enumerable.Range(0, 1000).Select(value => (byte)(value % 251)),
            output.ToArray());
    }

    [Fact]
    public void BoundedMemoryStream_SeekingAndFailedWritesPreserveStreamSemantics()
    {
        using var output = new BoundedMemoryStream(8, SafeMessage);
        output.Write([1, 2, 3, 4]);
        output.Position = 1;
        output.Write([9, 8]);

        Assert.Equal(4, output.Length);
        Assert.Equal(new byte[] { 1, 9, 8, 4 }, output.ToArray());

        output.Position = 8;
        var positionBeforeFailure = output.Position;
        Assert.Throws<DocumentSizeLimitExceededException>(() => output.WriteByte(7));
        Assert.Equal(positionBeforeFailure, output.Position);
        Assert.Equal(4, output.Length);

        output.Position = 9;
        Assert.Throws<DocumentSizeLimitExceededException>(() => output.Write([], 0, 0));
        Assert.Equal(9, output.Position);
        Assert.Equal(4, output.Length);
    }

    [Fact]
    public async Task ConvertToPdfAsync_TimesOutInterruptibleWork_AndReleasesConcurrencyPermit()
    {
        var options = new DocumentConversionOptions
        {
            MaxInputSizeBytes = 100,
            MaxOutputSizeBytes = 100,
            MaxPageCount = 10,
            MaxConcurrentConversions = 1,
            OverallConversionTimeoutSeconds = 1
        };
        using var limiter = CreateLimiter(options);
        var service = CreateService(options, limiter);
        using var blockedInput = new BlockingMemoryStream("blocked"u8.ToArray());

        var timedOut = service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
            blockedInput, "blocked.rtf", "blocked"));
        await blockedInput.ReadStarted.Task;
        await Assert.ThrowsAsync<DocumentConversionTimeoutException>(() => timedOut);

        using var nextInput = new MemoryStream("corrupt"u8.ToArray());
        await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                nextInput, "next.rtf", "next")));
    }

    [Fact]
    public async Task ConvertToPdfAsync_TimesOutWhileWaitingForConcurrencyPermit_AfterPreparingInput()
    {
        var options = new DocumentConversionOptions
        {
            MaxInputSizeBytes = 100,
            MaxOutputSizeBytes = 100,
            MaxPageCount = 10,
            MaxConcurrentConversions = 1,
            MaximumQueueWaitSeconds = 1
        };
        using var limiter = CreateLimiter(options);
        await limiter.WaitAsync(CancellationToken.None);
        try
        {
            var service = CreateService(options, limiter);
            using var input = new ReadTrackingMemoryStream(@"{\rtf1\ansi content}"u8.ToArray());

            await Assert.ThrowsAsync<DocumentConversionTimeoutException>(() =>
                service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                    input, "document.rtf", "safe-id")));

            Assert.True(input.ReadCount > 0);
        }
        finally
        {
            limiter.Release();
        }
    }

    [Fact]
    public async Task ConvertToPdfAsync_TimesOutConfigurationLookupBeforeWaitingForPermit()
    {
        var options = new DocumentConversionOptions
        {
            MaxConcurrentConversions = 1,
            OverallConversionTimeoutSeconds = 1
        };
        var configurationService = new Mock<IConfigurationService>();
        configurationService
            .Setup(service => service.GetConfigurationAsync())
            .Returns(new TaskCompletionSource<IEnumerable<ConstantDto>>(
                TaskCreationOptions.RunContinuationsAsynchronously).Task);
        using var limiter = CreateLimiter(options);
        var service = CreateService(
            options,
            limiter,
            configurationService: configurationService.Object);
        using var input = new ReadTrackingMemoryStream("content"u8.ToArray());

        await Assert.ThrowsAsync<DocumentConversionTimeoutException>(() =>
            service.ConvertToPdfAsync(new DocumentPdfConversionRequest(
                input, "document.rtf", "safe-id")));

        Assert.Equal(0, input.ReadCount);
    }

    [Fact]
    public async Task ConvertToPdfAsync_PropagatesClientCancellationWhileWaitingForConcurrencyPermit()
    {
        using var limiter = CreateLimiter(new DocumentConversionOptions
        {
            MaxConcurrentConversions = 1,
            OverallConversionTimeoutSeconds = 60
        });
        await limiter.WaitAsync(CancellationToken.None);
        try
        {
            var service = CreateService(limiter: limiter);
            using var input = new ReadTrackingMemoryStream(
                @"{\rtf1\ansi content}"u8.ToArray());
            using var cancellation = new CancellationTokenSource();
            var conversion = service.ConvertToPdfAsync(
                new DocumentPdfConversionRequest(input, "document.rtf", "safe-id"),
                cancellation.Token);

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => conversion);
            Assert.True(input.ReadCount > 0);
        }
        finally
        {
            limiter.Release();
        }
    }

    [Theory]
    [InlineData(DocumentPdfConversionAttemptStatus.Unsupported, DocumentPdfFallbackDecision.TryNext)]
    [InlineData(DocumentPdfConversionAttemptStatus.FidelityRejected, DocumentPdfFallbackDecision.TryNext)]
    [InlineData(DocumentPdfConversionAttemptStatus.InvalidDocument, DocumentPdfFallbackDecision.Stop)]
    public void DocumentPdfFallbackPolicy_ClassifiesAttempt(
        DocumentPdfConversionAttemptStatus status,
        DocumentPdfFallbackDecision expected)
    {
        var policy = new DocumentPdfFallbackPolicy(Options.Create(new DocumentConversionOptions()));
        var attempt = DocumentPdfConversionAttempt.Failed(
            "converter",
            status,
            new InvalidOperationException());
        using var input = new MemoryStream();
        var context = new DocumentPdfConversionContext(
            input,
            ConversionDocumentFormat.Docx,
            "document.docx",
            DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(expected, policy.Evaluate(attempt, context));
    }

    [Theory]
    [InlineData(DocumentPdfConversionAttemptStatus.FidelityRejected)]
    public async Task DocumentPdfConversionPipeline_TriesNextStrategyAndRewindsInputForFallback(
        DocumentPdfConversionAttemptStatus rejectionStatus)
    {
        using var input = new MemoryStream("source"u8.ToArray());
        var first = new Mock<IDocumentPdfConversionStrategy>();
        first.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        first.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentPdfConversionContext context, CancellationToken _) =>
            {
                context.Input.Position = context.Input.Length;
                return DocumentPdfConversionAttempt.Failed(
                    "primary",
                    rejectionStatus,
                    new InvalidOperationException("fidelity"));
            });
        var second = new Mock<IDocumentPdfConversionStrategy>();
        second.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        second.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentPdfConversionContext context, CancellationToken _) =>
            {
                Assert.Equal(0, context.Input.Position);
                return DocumentPdfConversionAttempt.Succeeded(
                    "fallback",
                    new MemoryStream("%PDF-fallback"u8.ToArray()));
            });
        var pipeline = CreatePipeline([first.Object, second.Object]);

        var result = await pipeline.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);
        await using var output = result.Output;

        Assert.Equal("%PDF-fallback", Encoding.ASCII.GetString(output.ToArray()));
        first.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
        second.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DocumentPdfConversionPipeline_StopsAfterNonFallbackFailure()
    {
        using var input = new MemoryStream("source"u8.ToArray());
        var expected = new InvalidOperationException("failed");
        var first = new Mock<IDocumentPdfConversionStrategy>();
        first.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        first.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentPdfConversionAttempt.Failed(
                "primary",
                DocumentPdfConversionAttemptStatus.InvalidDocument,
                expected));
        var second = new Mock<IDocumentPdfConversionStrategy>();
        second.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        second.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentPdfConversionAttempt.Succeeded(
                "fallback",
                new MemoryStream("%PDF-unexpected"u8.ToArray())));
        var pipeline = CreatePipeline([first.Object, second.Object]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await pipeline.ConvertAsync(
                new DocumentPdfConversionContext(
                    input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
                CancellationToken.None);
        });

        Assert.Same(expected, exception);
        first.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
        second.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DocumentPdfConversionPipeline_DoesNotInvokeFallbackAfterPrimarySuccess()
    {
        using var input = new MemoryStream("source"u8.ToArray());
        var primary = new Mock<IDocumentPdfConversionStrategy>();
        primary.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Rtf)).Returns(true);
        primary.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentPdfConversionAttempt.Succeeded(
                "primary",
                new MemoryStream("%PDF-primary"u8.ToArray())));
        var fallback = new Mock<IDocumentPdfConversionStrategy>();
        fallback.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Rtf)).Returns(true);
        var pipeline = CreatePipeline([primary.Object, fallback.Object]);

        var result = await pipeline.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Rtf, "document.rtf", DateTimeOffset.MaxValue),
            CancellationToken.None);
        await using var output = result.Output;

        Assert.Equal("%PDF-primary", Encoding.ASCII.GetString(output.ToArray()));
        fallback.Verify(strategy => strategy.ConvertAsync(
            It.IsAny<DocumentPdfConversionContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DocumentPdfConversionPipeline_PreservesSafeErrorWhenBothConvertersFail()
    {
        using var input = new MemoryStream("source"u8.ToArray());
        var primary = new Mock<IDocumentPdfConversionStrategy>();
        primary.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        primary.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentPdfConversionAttempt.Failed(
                "OfficeIMO",
                DocumentPdfConversionAttemptStatus.ConverterIncompatible,
                new DocumentParsingRejectedException(SafeMessage)));
        var fallback = new Mock<IDocumentPdfConversionStrategy>();
        fallback.Setup(strategy => strategy.CanConvert(ConversionDocumentFormat.Docx)).Returns(true);
        fallback.Setup(strategy => strategy.ConvertAsync(
                It.IsAny<DocumentPdfConversionContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentPdfConversionAttempt.Failed(
                "LibreOffice",
                DocumentPdfConversionAttemptStatus.InvalidDocument,
                new UnprocessableDocumentException(SafeMessage)));
        var pipeline = CreatePipeline([primary.Object, fallback.Object]);

        var exception = await Assert.ThrowsAsync<UnprocessableDocumentException>(() =>
            pipeline.ConvertAsync(
                new DocumentPdfConversionContext(
                    input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
                CancellationToken.None));

        Assert.Equal(SafeMessage, exception.Message);
    }

    private static DocumentPdfConversionService CreateService(
        DocumentConversionOptions options = null,
        DocumentConversionConcurrencyLimiter limiter = null,
        string[] enabledExtensions = null,
        IEnumerable<IDocumentPdfConversionStrategy> fallbackStrategies = null,
        IConfigurationService configurationService = null)
    {
        options ??= new DocumentConversionOptions();
        if (configurationService is null)
        {
            var configurationServiceMock = new Mock<IConfigurationService>();
            configurationServiceMock
                .Setup(service => service.GetConfigurationAsync())
                .ReturnsAsync([
                    new ConstantDto
                    {
                        Key = ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions,
                        Values = enabledExtensions is null
                            ? [.. SupportedExtensions]
                            : [.. enabledExtensions]
                    }
                ]);
            configurationService = configurationServiceMock.Object;
        }

        var strategies = new List<IDocumentPdfConversionStrategy>
        {
            new OfficeImoPdfConversionStrategy(
                NullLogger<OfficeImoPdfConversionStrategy>.Instance,
                Options.Create(options))
        };
        if (fallbackStrategies is not null)
        {
            strategies.AddRange(fallbackStrategies);
        }

        var pipeline = CreatePipeline(strategies, options);

        return new DocumentPdfConversionService(
            NullLogger<DocumentPdfConversionService>.Instance,
            configurationService,
            Options.Create(options),
            limiter ?? CreateLimiter(options),
            pipeline,
            new DocumentConversionSecurityPreflight(
                NullLogger<DocumentConversionSecurityPreflight>.Instance));
    }

    private static DocumentPdfConversionPipeline CreatePipeline(
        IReadOnlyList<IDocumentPdfConversionStrategy> strategies,
        DocumentConversionOptions options = null)
    {
        options ??= new DocumentConversionOptions();
        return new DocumentPdfConversionPipeline(
            new DocumentPdfFallbackPolicy(Options.Create(options)),
            NullLogger<DocumentPdfConversionPipeline>.Instance,
            new TestDocumentPdfRouteTable(strategies));
    }

    private sealed class TestDocumentPdfRouteTable(
        IReadOnlyList<IDocumentPdfConversionStrategy> strategies) : IDocumentPdfRouteTable
    {
        public IReadOnlyList<IDocumentPdfConversionStrategy> GetStrategies(
            ConversionDocumentFormat format) =>
            strategies.Where(strategy => strategy.CanConvert(format)).ToArray();
    }

    private static DocumentConversionConcurrencyLimiter CreateLimiter(
        DocumentConversionOptions options = null) =>
        new(Options.Create(options ?? new DocumentConversionOptions()));

    private static MemoryStream CreateDocx(string text)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
            stream,
            WordprocessingDocumentType.Document,
            autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            var heading = new Paragraph();
            var headingRun = new Run();
            headingRun.AppendChild(new Text("Document heading"));
            heading.AppendChild(headingRun);
            body.AppendChild(heading);

            var tableText = new Run();
            tableText.AppendChild(new Text(text));
            var tableParagraph = new Paragraph();
            tableParagraph.AppendChild(tableText);
            var tableCell = new TableCell();
            tableCell.AppendChild(tableParagraph);
            var tableRow = new TableRow();
            tableRow.AppendChild(tableCell);
            var table = new Table();
            table.AppendChild(tableRow);
            body.AppendChild(table);

            mainPart.Document = new Document();
            mainPart.Document.AppendChild(body);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocmWithMacroPayload(string text)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
            stream,
            WordprocessingDocumentType.MacroEnabledDocument,
            autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(new Paragraph(new Run(new Text(text)))));
            var macroPart = mainPart.AddNewPart<VbaProjectPart>();
            using var macroBytes = new MemoryStream([0, 1, 2, 3]);
            macroPart.FeedData(macroBytes);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateWordVariant(string text, string extension)
    {
        var type = extension switch
        {
            ".docx" => WordprocessingDocumentType.Document,
            ".docm" => WordprocessingDocumentType.MacroEnabledDocument,
            ".dotx" => WordprocessingDocumentType.Template,
            ".dotm" => WordprocessingDocumentType.MacroEnabledTemplate,
            _ => throw new ArgumentOutOfRangeException(nameof(extension))
        };
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, type, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(text)))));
            if (extension is ".docm" or ".dotm")
            {
                var macroPart = mainPart.AddNewPart<VbaProjectPart>();
                using var macroBytes = new MemoryStream([0, 1, 2, 3]);
                macroPart.FeedData(macroBytes);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxWithExternalRelationship(
        string target = "https://example.invalid/resource")
    {
        var stream = CreateDocx("External relationship document");
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            document.MainDocumentPart!.AddHyperlinkRelationship(
                new Uri(target),
                isExternal: true);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxWithExternalTemplateRelationship()
    {
        var stream = CreateDocx("External template relationship document");
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            document.MainDocumentPart!.AddExternalRelationship(
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships/attachedTemplate",
                new Uri("https://example.invalid/template.dotx"));
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxWithEmbeddedPackage()
    {
        var stream = CreateDocx("Embedded package document");
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            var embeddedPart = document.MainDocumentPart!
                .AddEmbeddedPackagePart("application/octet-stream");
            using var payload = new MemoryStream("embedded payload"u8.ToArray());
            embeddedPart.FeedData(payload);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxWithActiveXPayload()
    {
        var stream = CreateDocx("ActiveX payload document");
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            var activeXPart = document.MainDocumentPart!
                .AddEmbeddedControlPersistencePart(EmbeddedControlPersistencePartType.ActiveX);
            using var payload = new MemoryStream("<ax:ocx xmlns:ax=\"urn:test\"/>"u8.ToArray());
            activeXPart.FeedData(payload);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateEncryptedOfficeContainer()
    {
        var stream = new MemoryStream();
        using (var root = RootStorage.Create(
            stream,
            OpenMcdf.Version.V3,
            StorageModeFlags.LeaveOpen))
        {
            using var encryptedPackage = root.CreateStream("EncryptedPackage");
            encryptedPackage.WriteByte(0);
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateXlsx(string text)
    {
        var stream = new MemoryStream();
        using (var document = ExcelDocument.Create(stream))
        {
            var sheet = document.AddWorksheet("Evidence");
            sheet.CellValue(1, 1, text);
            sheet.CellValue(1, 2, 42);
            document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateExcelVariant(string text, string extension)
    {
        var stream = CreateXlsx(text);
        if (extension == ".xlsx") return stream;

        using (var document = SpreadsheetDocument.Open(stream, true))
        {
            document.ChangeDocumentType(extension switch
            {
                ".xlsm" => SpreadsheetDocumentType.MacroEnabledWorkbook,
                ".xltx" => SpreadsheetDocumentType.Template,
                ".xltm" => SpreadsheetDocumentType.MacroEnabledTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(extension))
            });
            if (extension is ".xlsm" or ".xltm")
            {
                var macroPart = document.WorkbookPart!.AddNewPart<VbaProjectPart>();
                using var macroBytes = new MemoryStream([0, 1, 2, 3]);
                macroPart.FeedData(macroBytes);
            }
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreatePptx(string text)
    {
        var stream = new MemoryStream();
        using (var presentation = PowerPointPresentation.Create(stream))
        {
            presentation.AddSlide().AddTitle(text);
            presentation.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateOdtAsync(string text)
    {
        var document = OdtDocument.Create();
        document.AddParagraph(text);
        var stream = new MemoryStream();
        await document.SaveAsync(stream, new OdfSaveOptions(), CancellationToken.None);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreatePowerPointVariant(string text, string extension)
    {
        var stream = CreatePptx(text);
        if (extension == ".pptx") return stream;

        using (var document = PresentationDocument.Open(stream, true))
        {
            document.ChangeDocumentType(PresentationDocumentType.MacroEnabledPresentation);
            var macroPart = document.PresentationPart!.AddNewPart<VbaProjectPart>();
            using var macroBytes = new MemoryStream([0, 1, 2, 3]);
            macroPart.FeedData(macroBytes);
        }
        stream.Position = 0;
        return stream;
    }

    private static void AssertPdfContains(MemoryStream pdf, string expectedText)
    {
        pdf.Position = 0;
        var pdfReader = new OfficeDocumentReaderBuilder().AddPdfHandler().Build();
        var convertedDocument = pdfReader.ReadDocument(
            pdf,
            "converted.pdf",
            new ReaderOptions { MaxInputBytes = 10 * 1024 * 1024 });

        Assert.NotEmpty(convertedDocument.Search(expectedText).Hits);
    }

    private static byte[] AddUnsupportedIccProfile(byte[] tiff)
    {
        var source = tiff.AsSpan();
        var sourceIfdOffset = BinaryPrimitives.ReadInt32LittleEndian(source[4..8]);
        var sourceEntryCount = BinaryPrimitives.ReadUInt16LittleEndian(source[sourceIfdOffset..]);
        var sourceEntriesLength = sourceEntryCount * 12;
        var sourceEntriesOffset = sourceIfdOffset + 2;
        var sourceNextIfdOffset = sourceEntriesOffset + sourceEntriesLength;
        var unsupportedIccProfile = Encoding.ASCII.GetBytes("unsupported-icc-profile");
        var targetIfdOffset = tiff.Length;
        var targetEntryCount = sourceEntryCount + 1;
        var targetEntriesOffset = targetIfdOffset + 2;
        var targetNextIfdOffset = targetEntriesOffset + targetEntryCount * 12;
        var profileOffset = targetNextIfdOffset + 4;
        var result = new byte[profileOffset + unsupportedIccProfile.Length];

        source.CopyTo(result);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4, 4), targetIfdOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(targetIfdOffset, 2), (ushort)targetEntryCount);
        source.Slice(sourceEntriesOffset, sourceEntriesLength)
            .CopyTo(result.AsSpan(targetEntriesOffset));

        var iccEntry = result.AsSpan(targetEntriesOffset + sourceEntriesLength, 12);
        BinaryPrimitives.WriteUInt16LittleEndian(iccEntry, 34675);
        BinaryPrimitives.WriteUInt16LittleEndian(iccEntry[2..], 7);
        BinaryPrimitives.WriteInt32LittleEndian(iccEntry[4..], unsupportedIccProfile.Length);
        BinaryPrimitives.WriteInt32LittleEndian(iccEntry[8..], profileOffset);
        source.Slice(sourceNextIfdOffset, 4)
            .CopyTo(result.AsSpan(targetNextIfdOffset, 4));
        unsupportedIccProfile.CopyTo(result, profileOffset);

        return result;
    }

    private class ReadTrackingMemoryStream(byte[] buffer) : MemoryStream(buffer)
    {
        public int ReadCount { get; private set; }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private sealed class BlockingMemoryStream(byte[] buffer) : ReadTrackingMemoryStream(buffer)
    {
        public TaskCompletionSource<bool> ReadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AllowRead { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            ReadStarted.TrySetResult(true);
            await AllowRead.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
