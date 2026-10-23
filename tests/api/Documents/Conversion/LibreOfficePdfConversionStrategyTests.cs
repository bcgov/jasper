using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;
using Xunit;
using ConversionDocumentFormat = Scv.Api.Documents.Conversion.DocumentFormat;

namespace tests.api.Documents.Conversion;

public class LibreOfficePdfConversionStrategyTests
{
    private const string SafeMessage = "This document format could not be converted for viewing.";

    public static IEnumerable<object[]> SupportedOfficeFormats()
    {
        yield return [ConversionDocumentFormat.Doc];
        yield return [ConversionDocumentFormat.Docx];
        yield return [ConversionDocumentFormat.Docm];
        yield return [ConversionDocumentFormat.Dotx];
        yield return [ConversionDocumentFormat.Dotm];
        yield return [ConversionDocumentFormat.Xls];
        yield return [ConversionDocumentFormat.Xlsx];
        yield return [ConversionDocumentFormat.Xlsm];
        yield return [ConversionDocumentFormat.Xltx];
        yield return [ConversionDocumentFormat.Xltm];
        yield return [ConversionDocumentFormat.Xlsb];
        yield return [ConversionDocumentFormat.Pptx];
        yield return [ConversionDocumentFormat.Pptm];
        yield return [ConversionDocumentFormat.Rtf];
        yield return [ConversionDocumentFormat.Odt];
    }

    public static IEnumerable<object[]> UnsupportedFormats()
    {
        yield return [ConversionDocumentFormat.Pdf];
        yield return [ConversionDocumentFormat.Png];
        yield return [ConversionDocumentFormat.Jpeg];
        yield return [ConversionDocumentFormat.Tiff];
        yield return [ConversionDocumentFormat.Webp];
        yield return [ConversionDocumentFormat.Unknown];
        yield return [ConversionDocumentFormat.EncryptedOffice];
    }

    [Theory]
    [MemberData(nameof(SupportedOfficeFormats))]
    public void CanConvert_ReturnsTrueOnlyForConfiguredOfficeFormats(ConversionDocumentFormat format)
    {
        var strategy = CreateStrategy(new Mock<IDocumentProcessRunner>().Object);

        Assert.True(strategy.CanConvert(format));
    }

    [Theory]
    [MemberData(nameof(UnsupportedFormats))]
    public void CanConvert_ReturnsFalseForPdfImagesAndUnknownFormats(ConversionDocumentFormat format)
    {
        var strategy = CreateStrategy(new Mock<IDocumentProcessRunner>().Object);

        Assert.False(strategy.CanConvert(format));
    }

    [Fact]
    public async Task ConvertAsync_SkipsUnsupportedFormatsWithoutStartingProcess()
    {
        var runner = new Mock<IDocumentProcessRunner>();
        var strategy = CreateStrategy(runner.Object);
        using var input = new MemoryStream("%PDF-source"u8.ToArray());

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Pdf, "document.pdf", DateTimeOffset.MaxValue),
            CancellationToken.None);

        Assert.Equal(DocumentPdfConversionAttemptStatus.Unsupported, attempt.Status);
        runner.Verify(value => value.RunAsync(
            It.IsAny<DocumentProcessRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConvertAsync_UsesIsolatedArgumentListAndReturnsRewoundPdf()
    {
        string workingDirectory = null;
        ProcessStartInfo capturedStartInfo = null;
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((DocumentProcessRequest request, CancellationToken _) =>
            {
                capturedStartInfo = request.StartInfo;
                workingDirectory = request.StartInfo.WorkingDirectory;
                WriteOutput(request.StartInfo, "%PDF-fallback");
                return Task.FromResult(new DocumentProcessResult(0, 25));
            });
        var strategy = CreateStrategy(runner.Object);
        using var input = new MemoryStream("office-content"u8.ToArray());
        input.Position = input.Length;

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);

        Assert.Equal(DocumentPdfConversionAttemptStatus.Succeeded, attempt.Status);
        Assert.NotNull(attempt.Output);
        await using var output = attempt.Output;
        Assert.Equal(0, output.Position);
        Assert.Equal("%PDF-fallback", Encoding.ASCII.GetString(output.ToArray()));
        Assert.NotNull(capturedStartInfo);
        Assert.Equal("soffice", capturedStartInfo.FileName);
        Assert.Contains("--headless", capturedStartInfo.ArgumentList);
        Assert.Contains("--nologo", capturedStartInfo.ArgumentList);
        Assert.Contains("--nodefault", capturedStartInfo.ArgumentList);
        Assert.Contains("--norestore", capturedStartInfo.ArgumentList);
        Assert.DoesNotContain("--safe-mode", capturedStartInfo.ArgumentList);
        Assert.Contains("pdf:writer_pdf_Export", capturedStartInfo.ArgumentList);
        Assert.Single(capturedStartInfo.ArgumentList, argument =>
            argument.StartsWith("-env:UserInstallation=file:", StringComparison.Ordinal));
        Assert.False(Directory.Exists(workingDirectory));
    }

    [Theory]
    [InlineData(1, null, DocumentPdfConversionAttemptStatus.ConverterExecutionFailed)]
    [InlineData(0, "", DocumentPdfConversionAttemptStatus.InvalidConverterOutput)]
    [InlineData(0, "not-a-pdf", DocumentPdfConversionAttemptStatus.InvalidConverterOutput)]
    public async Task ConvertAsync_ReturnsSafeFailureAndCleansTemporaryFiles(
        int exitCode,
        string outputContents,
        DocumentPdfConversionAttemptStatus expectedStatus)
    {
        string workingDirectory = null;
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((DocumentProcessRequest request, CancellationToken _) =>
            {
                workingDirectory = request.StartInfo.WorkingDirectory;
                if (outputContents is not null)
                {
                    WriteOutput(request.StartInfo, outputContents);
                }

                return Task.FromResult(new DocumentProcessResult(
                    exitCode,
                    10));
            });
        var strategy = CreateStrategy(runner.Object);
        using var input = new MemoryStream("office-content"u8.ToArray());

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);

        Assert.Equal(expectedStatus, attempt.Status);
        var exception = Assert.IsType<UnprocessableDocumentException>(attempt.Exception);
        Assert.Equal(SafeMessage, exception.Message);
        Assert.False(Directory.Exists(workingDirectory));
    }

    [Fact]
    public async Task ConvertAsync_RejectsOversizedOutputAndCleansTemporaryFiles()
    {
        string workingDirectory = null;
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((DocumentProcessRequest request, CancellationToken _) =>
            {
                workingDirectory = request.StartInfo.WorkingDirectory;
                WriteOutput(request.StartInfo, "%PDF-output-too-large");
                return Task.FromResult(new DocumentProcessResult(0, 10));
            });
        var strategy = CreateStrategy(
            runner.Object,
            new DocumentConversionOptions { MaxOutputSizeBytes = 8 });
        using var input = new MemoryStream("office-content"u8.ToArray());

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);

        Assert.Equal(DocumentPdfConversionAttemptStatus.OutputLimitExceeded, attempt.Status);
        Assert.IsType<DocumentSizeLimitExceededException>(attempt.Exception);
        Assert.False(Directory.Exists(workingDirectory));
    }

    [Fact]
    public async Task ConvertAsync_MapsProcessTimeoutToSafeFailureAndCleansTemporaryFiles()
    {
        string workingDirectory = null;
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (DocumentProcessRequest request, CancellationToken cancellationToken) =>
            {
                workingDirectory = request.StartInfo.WorkingDirectory;
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("unreachable");
            });
        var strategy = CreateStrategy(
            runner.Object,
            new DocumentConversionOptions { LibreOfficeProcessTimeoutSeconds = 1 });
        using var input = new MemoryStream("office-content"u8.ToArray());

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);

        Assert.Equal(DocumentPdfConversionAttemptStatus.ConverterTimedOut, attempt.Status);
        Assert.IsType<DocumentConversionTimeoutException>(attempt.Exception);
        Assert.False(Directory.Exists(workingDirectory));
    }

    [Fact]
    public async Task ConvertAsync_PropagatesClientCancellationAndCleansTemporaryFiles()
    {
        string workingDirectory = null;
        using var cancellation = new CancellationTokenSource();
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (DocumentProcessRequest request, CancellationToken cancellationToken) =>
            {
                workingDirectory = request.StartInfo.WorkingDirectory;
                await cancellation.CancelAsync();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("unreachable");
            });
        var strategy = CreateStrategy(runner.Object);
        using var input = new MemoryStream("office-content"u8.ToArray());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            cancellation.Token));

        Assert.False(Directory.Exists(workingDirectory));
    }

    [Fact]
    public async Task ConvertAsync_ReturnsSafeFailureWhenProcessCannotStart()
    {
        var runner = new Mock<IDocumentProcessRunner>();
        runner.Setup(value => value.RunAsync(
                It.IsAny<DocumentProcessRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.ComponentModel.Win32Exception("process details"));
        var strategy = CreateStrategy(runner.Object);
        using var input = new MemoryStream("office-content"u8.ToArray());

        var attempt = await strategy.ConvertAsync(
            new DocumentPdfConversionContext(
                input, ConversionDocumentFormat.Docx, "document.docx", DateTimeOffset.MaxValue),
            CancellationToken.None);

        var exception = Assert.IsType<UnprocessableDocumentException>(attempt.Exception);
        Assert.Equal(SafeMessage, exception.Message);
        Assert.DoesNotContain("process details", exception.Message);
    }

    private static LibreOfficePdfConversionStrategy CreateStrategy(
        IDocumentProcessRunner runner,
        DocumentConversionOptions options = null)
    {
        options ??= new DocumentConversionOptions();
        return new LibreOfficePdfConversionStrategy(
            Options.Create(options),
            runner,
            NullLogger<LibreOfficePdfConversionStrategy>.Instance,
            new LibreOfficeConversionConcurrencyLimiter(Options.Create(options)));
    }

    private static void WriteOutput(ProcessStartInfo startInfo, string contents)
    {
        var arguments = startInfo.ArgumentList.ToArray();
        var outputDirectory = arguments[Array.IndexOf(arguments, "--outdir") + 1];
        File.WriteAllText(Path.Combine(outputDirectory, "source.pdf"), contents, Encoding.ASCII);
    }
}
