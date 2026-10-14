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
                LogFailure(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.ConverterTimedOut,
                    "process-timeout",
                    null,
                    0,
                    timeoutException);
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.ConverterTimedOut,
                    timeoutException);
            }

            if (processResult.ExitCode != 0)
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                LogFailure(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                    "nonzero-exit-code",
                    processResult.ExitCode,
                    0,
                    exception,
                    processResult.ElapsedMilliseconds);
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                    exception);
            }

            if (!File.Exists(outputPath))
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                LogFailure(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    "missing-output",
                    processResult.ExitCode,
                    0,
                    exception,
                    processResult.ElapsedMilliseconds);
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    exception);
            }

            var outputLength = new FileInfo(outputPath).Length;
            if (outputLength == 0)
            {
                var exception = new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                LogFailure(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    "empty-output",
                    processResult.ExitCode,
                    outputLength,
                    exception,
                    processResult.ElapsedMilliseconds);
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    exception);
            }

            if (outputLength > _options.MaxOutputSizeBytes)
            {
                var exception = new DocumentSizeLimitExceededException(
                    DocumentPdfConversionService.ConversionErrorMessage);
                LogFailure(
                    context.Format,
                    DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                    "output-size-limit",
                    processResult.ExitCode,
                    outputLength,
                    exception,
                    processResult.ElapsedMilliseconds);
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                    exception);
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
                    LogFailure(
                        context.Format,
                        DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                        "invalid-pdf-signature",
                        processResult.ExitCode,
                        outputLength,
                        exception,
                        processResult.ElapsedMilliseconds);
                    return DocumentPdfConversionAttempt.Failed(
                        Name,
                        DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                        exception);
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
            LogFailure(
                context.Format,
                DocumentPdfConversionAttemptStatus.ConverterUnavailable,
                "process-unavailable",
                null,
                0,
                safeException);
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.ConverterUnavailable,
                safeException);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var safeException = new UnprocessableDocumentException(
                DocumentPdfConversionService.ConversionErrorMessage,
                exception);
            LogFailure(
                context.Format,
                DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                "process-or-filesystem-failure",
                null,
                0,
                safeException);
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
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
        const string registry = """
            <?xml version="1.0" encoding="UTF-8"?>
            <oor:items xmlns:oor="http://openoffice.org/2001/registry">
              <item oor:path="/org.openoffice.Office.Common/Security/Scripting">
                <prop oor:name="MacroSecurityLevel" oor:op="fuse"><value>3</value></prop>
                <prop oor:name="DisableMacrosExecution" oor:op="fuse"><value>true</value></prop>
                <prop oor:name="DisableActiveContent" oor:op="fuse"><value>true</value></prop>
                <prop oor:name="DisablePythonRuntime" oor:op="fuse"><value>true</value></prop>
                <prop oor:name="DisableOLEAutomation" oor:op="fuse"><value>true</value></prop>
                <prop oor:name="BlockUntrustedRefererLinks" oor:op="fuse"><value>true</value></prop>
              </item>
              <item oor:path="/org.openoffice.Office.Calc/Content/Update">
                <prop oor:name="Link" oor:op="fuse"><value>1</value></prop>
              </item>
              <item oor:path="/org.openoffice.Office.Writer/Content/Update">
                <prop oor:name="Link" oor:op="fuse"><value>2</value></prop>
                <prop oor:name="Field" oor:op="fuse"><value>false</value></prop>
                <prop oor:name="Chart" oor:op="fuse"><value>false</value></prop>
              </item>
            </oor:items>
            """;
        await File.WriteAllTextAsync(registryPath, registry, cancellationToken);
    }

    private void LogFailure(
        DocumentFormat format,
        DocumentPdfConversionAttemptStatus status,
        string fallbackReason,
        int? exitCode,
        long outputBytes,
        Exception exception,
        long elapsedMilliseconds = 0)
    {
        _logger.LogError(
            exception,
            "Document conversion attempt failed - Converter: {Converter}, InputFormat: {InputFormat}, AttemptStatus: {AttemptStatus}, FallbackReason: {FallbackReason}, ProcessExitCode: {ProcessExitCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, OutputBytes: {OutputBytes}",
            Name,
            format,
            status,
            fallbackReason,
            exitCode,
            elapsedMilliseconds,
            outputBytes);
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
