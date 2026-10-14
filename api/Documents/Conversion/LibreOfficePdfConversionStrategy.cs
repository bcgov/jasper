using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal sealed class LibreOfficePdfConversionStrategy : IDocumentPdfConversionStrategy
{
    private const string ConverterName = "LibreOffice";
    private const string WriterPdfExportFilter = "pdf:writer_pdf_Export";
    private const string CalcPdfExportFilter = "pdf:calc_pdf_Export";
    private const string ImpressPdfExportFilter = "pdf:impress_pdf_Export";
    private const string SecurityProfileResourceName =
        "Scv.Api.Documents.Conversion.LibreOfficeSecurityProfile.xcu";

    private sealed record LibreOfficeFormat(string Extension, string PdfExportFilter);

    private static readonly IReadOnlyDictionary<DocumentFormat, LibreOfficeFormat> SupportedFormats =
        new Dictionary<DocumentFormat, LibreOfficeFormat>
        {
            [DocumentFormat.Doc] = new(".doc", WriterPdfExportFilter),
            [DocumentFormat.Docx] = new(".docx", WriterPdfExportFilter),
            [DocumentFormat.Docm] = new(".docm", WriterPdfExportFilter),
            [DocumentFormat.Dotx] = new(".dotx", WriterPdfExportFilter),
            [DocumentFormat.Dotm] = new(".dotm", WriterPdfExportFilter),
            [DocumentFormat.Xls] = new(".xls", CalcPdfExportFilter),
            [DocumentFormat.Xlsx] = new(".xlsx", CalcPdfExportFilter),
            [DocumentFormat.Xlsm] = new(".xlsm", CalcPdfExportFilter),
            [DocumentFormat.Xltx] = new(".xltx", CalcPdfExportFilter),
            [DocumentFormat.Xltm] = new(".xltm", CalcPdfExportFilter),
            [DocumentFormat.Xlsb] = new(".xlsb", CalcPdfExportFilter),
            [DocumentFormat.Pptx] = new(".pptx", ImpressPdfExportFilter),
            [DocumentFormat.Pptm] = new(".pptm", ImpressPdfExportFilter),
            [DocumentFormat.Rtf] = new(".rtf", WriterPdfExportFilter),
            [DocumentFormat.Odt] = new(".odt", WriterPdfExportFilter)
        };

    private readonly DocumentConversionOptions _options;
    private readonly IDocumentProcessRunner _processRunner;
    private readonly ILogger<LibreOfficePdfConversionStrategy> _logger;
    private readonly LibreOfficeConversionConcurrencyLimiter _concurrencyLimiter;

    public LibreOfficePdfConversionStrategy(
        IOptions<DocumentConversionOptions> options,
        IDocumentProcessRunner processRunner,
        ILogger<LibreOfficePdfConversionStrategy> logger,
        LibreOfficeConversionConcurrencyLimiter concurrencyLimiter)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
        _concurrencyLimiter = concurrencyLimiter;
    }

    public string Name => ConverterName;

    public DocumentPdfConverterId Id => DocumentPdfConverterId.LibreOffice;

    public bool CanConvert(DocumentFormat format) =>
        SupportedFormats.ContainsKey(format);

    public async Task<DocumentPdfConversionAttempt> ConvertAsync(
        DocumentPdfConversionContext context,
        CancellationToken cancellationToken)
    {
        if (!CanConvert(context.Format))
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.Unsupported,
                new UnsupportedDocumentFormatException(DocumentPdfConversionService.ConversionErrorMessage));
        }

        var attemptId = Guid.NewGuid().ToString("N");
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"jasper-libreoffice-{attemptId}");
        var inputDirectory = Path.Combine(workingDirectory, "input");
        var outputDirectory = Path.Combine(workingDirectory, "output");
        var profileDirectory = Path.Combine(workingDirectory, "profile");
        var format = SupportedFormats[context.Format];
        var inputPath = Path.Combine(inputDirectory, $"source{format.Extension}");
        var outputPath = Path.Combine(outputDirectory, "source.pdf");

        await _concurrencyLimiter.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(inputDirectory);
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(profileDirectory);
            await WriteSecurityProfileAsync(profileDirectory, cancellationToken);

            context.Input.Position = 0;
            await using (var inputFile = new FileStream(
                             inputPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             useAsync: true))
            {
                await context.Input.CopyToAsync(inputFile, cancellationToken);
            }

            var startInfo = CreateStartInfo(
                _options.LibreOfficeExecutablePath,
                inputPath,
                outputDirectory,
                profileDirectory,
                workingDirectory,
                format.PdfExportFilter);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.LibreOfficeProcessTimeoutSeconds));

            DocumentProcessResult processResult;
            try
            {
                processResult = await _processRunner.RunAsync(
                    new DocumentProcessRequest(startInfo),
                    timeoutSource.Token);
            }
            catch (OperationCanceledException exception) when (
                !cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
            {
                var timeoutException = new DocumentConversionTimeoutException(
                    DocumentPdfConversionService.ConversionErrorMessage,
                    exception);
                return CreateFailedAttempt(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.ConverterTimedOut,
                    "process-timeout",
                    timeoutException);
            }

            if (processResult.ExitCode != 0)
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                return CreateFailedAttempt(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                    "nonzero-exit-code",
                    exception,
                    processResult);
            }

            if (!File.Exists(outputPath))
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                return CreateFailedAttempt(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    "missing-output",
                    exception,
                    processResult);
            }

            var outputLength = new FileInfo(outputPath).Length;
            if (outputLength == 0)
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                return CreateFailedAttempt(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    "empty-output",
                    exception,
                    processResult,
                    outputLength);
            }

            if (outputLength > _options.MaxOutputSizeBytes)
            {
                var exception = new DocumentSizeLimitExceededException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                return CreateFailedAttempt(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                    "output-size-limit",
                    exception,
                    processResult,
                    outputLength);
            }

            var output = new BoundedMemoryStream(
                _options.MaxOutputSizeBytes,
                DocumentPdfConversionService.ConversionErrorMessage);
            var outputTransferred = false;
            try
            {
                await using var outputFile = new FileStream(
                    outputPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true);
                await outputFile.CopyToAsync(output, cancellationToken);
                if (!PdfFileValidation.HasPdfSignature(output))
                {
                    var exception = new UnprocessableDocumentException(
                        DocumentPdfConversionService.ConversionErrorMessage);
                    return CreateFailedAttempt(
                        context.Format,
                        DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                        "invalid-pdf-signature",
                        exception,
                        processResult,
                        outputLength);
                }

                output.Position = 0;
                outputTransferred = true;
                return DocumentPdfConversionAttempt.Succeeded(Name, output);
            }
            finally
            {
                if (!outputTransferred)
                {
                    await output.DisposeAsync();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Win32Exception exception)
        {
            var safeException = new UnprocessableDocumentException(
                DocumentPdfConversionService.ConversionErrorMessage,
                exception);
            return CreateFailedAttempt(
                context.Format,
                DocumentPdfConversionAttemptStatus.ConverterUnavailable,
                "process-unavailable",
                safeException);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var safeException = new UnprocessableDocumentException(
                DocumentPdfConversionService.ConversionErrorMessage,
                exception);
            return CreateFailedAttempt(
                context.Format,
                DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                "process-or-filesystem-failure",
                safeException);
        }
        finally
        {
            TryDeleteWorkingDirectory(workingDirectory, context.Format);
            _concurrencyLimiter.Release();
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string executablePath,
        string inputPath,
        string outputDirectory,
        string profileDirectory,
        string workingDirectory,
        string pdfExportFilter)
    {
        var profileUri = new Uri(
            Path.GetFullPath(profileDirectory) + Path.DirectorySeparatorChar).AbsoluteUri;
        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--norestore");
        // Do not use --safe-mode: it replaces the configured user profile and would
        // bypass the per-attempt security settings written below.
        startInfo.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add(pdfExportFilter);
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        // Do not expose API credentials or unrelated application configuration to the child.
        var allowedEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PATH"] = Environment.GetEnvironmentVariable("PATH"),
            ["LANG"] = Environment.GetEnvironmentVariable("LANG"),
            ["LC_ALL"] = Environment.GetEnvironmentVariable("LC_ALL"),
            ["TZ"] = Environment.GetEnvironmentVariable("TZ"),
            ["LD_LIBRARY_PATH"] = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")
        };
        startInfo.Environment.Clear();
        foreach (var (name, value) in allowedEnvironment)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                startInfo.Environment[name] = value;
            }
        }

        // Keep LibreOffice state and temporary files inside the per-attempt directory.
        startInfo.Environment["HOME"] = workingDirectory;
        startInfo.Environment["TMPDIR"] = workingDirectory;
        startInfo.Environment["SAL_USE_VCLPLUGIN"] = "svp";
        return startInfo;
    }

    private static async Task WriteSecurityProfileAsync(
        string profileDirectory,
        CancellationToken cancellationToken)
    {
        var userDirectory = Path.Combine(profileDirectory, "user");
        Directory.CreateDirectory(userDirectory);
        var registryPath = Path.Combine(userDirectory, "registrymodifications.xcu");
        await using var source = typeof(LibreOfficePdfConversionStrategy)
            .Assembly
            .GetManifestResourceStream(SecurityProfileResourceName)
            ?? throw new InvalidOperationException(
                "The LibreOffice security profile resource is missing.");
        await using var destination = new FileStream(
            registryPath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous
            });
        await source.CopyToAsync(destination, cancellationToken);
    }

    private DocumentPdfConversionAttempt CreateFailedAttempt(
        DocumentFormat format,
        DocumentPdfConversionAttemptStatus status,
        string fallbackReason,
        Exception exception,
        DocumentProcessResult processResult = null,
        long outputBytes = 0)
    {
        _logger.LogError(
            exception,
            "Document conversion attempt failed - Converter: {Converter}, InputFormat: {InputFormat}, AttemptStatus: {AttemptStatus}, FallbackReason: {FallbackReason}, ProcessExitCode: {ProcessExitCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, OutputBytes: {OutputBytes}",
            Name,
            format,
            status,
            fallbackReason,
            processResult?.ExitCode,
            processResult?.ElapsedMilliseconds ?? 0,
            outputBytes);

        return DocumentPdfConversionAttempt.Failed(Name, status, exception);
    }

    private void TryDeleteWorkingDirectory(string workingDirectory, DocumentFormat format)
    {
        try
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                exception,
                "Document conversion temporary cleanup failed - Converter: {Converter}, InputFormat: {InputFormat}",
                Name,
                format);
        }
    }

}

internal sealed class LibreOfficeConversionConcurrencyLimiter : IDisposable
{
    private readonly SemaphoreSlim _gate;

    public LibreOfficeConversionConcurrencyLimiter(IOptions<DocumentConversionOptions> options)
    {
        _gate = new SemaphoreSlim(
            options.Value.MaxConcurrentLibreOfficeConversions,
            options.Value.MaxConcurrentLibreOfficeConversions);
    }

    public Task WaitAsync(CancellationToken cancellationToken) => _gate.WaitAsync(cancellationToken);

    public void Release() => _gate.Release();

    public void Dispose() => _gate.Dispose();
}
