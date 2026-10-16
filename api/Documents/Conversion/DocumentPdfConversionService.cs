using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Core.Exceptions;
using Scv.Models.Configuration;

namespace Scv.Api.Documents.Conversion;

internal sealed class DocumentPdfConversionService : IDocumentPdfConversionService
{
    public const string ConversionErrorMessage = "This document format could not be converted for viewing.";

    private readonly ILogger<DocumentPdfConversionService> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly DocumentConversionOptions _options;
    private readonly DocumentConversionConcurrencyLimiter _concurrencyLimiter;
    private readonly DocumentPdfConversionPipeline _conversionPipeline;
    private readonly IDocumentConversionSecurityPreflight _securityPreflight;

    public DocumentPdfConversionService(
        ILogger<DocumentPdfConversionService> logger,
        IConfigurationService configurationService,
        IOptions<DocumentConversionOptions> options,
        DocumentConversionConcurrencyLimiter concurrencyLimiter,
        DocumentPdfConversionPipeline conversionPipeline,
        IDocumentConversionSecurityPreflight securityPreflight)
    {
        _logger = logger;
        _configurationService = configurationService;
        _options = options.Value;
        _concurrencyLimiter = concurrencyLimiter;
        _securityPreflight = securityPreflight;
        _conversionPipeline = conversionPipeline;
    }

    public async Task<MemoryStream> ConvertToPdfAsync(
        DocumentPdfConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Stream);

        var normalizedFileName = (request.FileName ?? string.Empty).Replace('\\', '/');
        var safeFileName = Path.GetFileName(normalizedFileName);
        var extension = Path.GetExtension(safeFileName);
        var totalStopwatch = Stopwatch.StartNew();
        long queueMilliseconds = 0;
        long executionMilliseconds = 0;
        if (!DocumentFormatDetector.TryGetDeclaredFormat(safeFileName, out var declaredFormat))
        {
            throw new UnsupportedDocumentFormatException(ConversionErrorMessage);
        }

        var gateAcquired = false;
        MemoryStream input = null;
        using var overallTimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overallTimeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.OverallConversionTimeoutSeconds));
        var deadlineUtc = DateTimeOffset.UtcNow.AddSeconds(_options.OverallConversionTimeoutSeconds);
        CancellationTokenSource queueTimeoutSource = null;
        try
        {
            if (!await IsEnabledAsync(extension).WaitAsync(overallTimeoutSource.Token))
            {
                throw new UnsupportedDocumentFormatException(ConversionErrorMessage);
            }

            input = await CopyToBoundedStreamAsync(request.Stream, overallTimeoutSource.Token);
            var detectedFormat = DocumentFormatDetector.Detect(input);
            ValidateDetectedFormat(safeFileName, declaredFormat, detectedFormat);
            _securityPreflight.Validate(input, detectedFormat, _options.MaxOutputSizeBytes);
            input.Position = 0;
            if (detectedFormat == DocumentFormat.Pdf)
            {
                var passThrough = input;
                input = null;
                _logger.LogInformation(
                    "PDF conversion completed - SourceIdentifier: {SourceIdentifier}, FinalConverter: {FinalConverter}, InputFormat: {InputFormat}, QueueMilliseconds: {QueueMilliseconds}, TotalMilliseconds: {TotalMilliseconds}, OutputBytes: {OutputBytes}",
                    request.SourceIdentifier,
                    "PassThrough",
                    detectedFormat,
                    queueMilliseconds,
                    totalStopwatch.ElapsedMilliseconds,
                    passThrough.Length);
                return passThrough;
            }

            queueTimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(overallTimeoutSource.Token);
            queueTimeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.MaximumQueueWaitSeconds));
            var queueStopwatch = Stopwatch.StartNew();
            await _concurrencyLimiter.WaitAsync(queueTimeoutSource.Token);
            queueStopwatch.Stop();
            queueMilliseconds = queueStopwatch.ElapsedMilliseconds;
            gateAcquired = true;
            var executionStopwatch = Stopwatch.StartNew();
            var pipelineResult = await _conversionPipeline.ConvertAsync(
                new DocumentPdfConversionContext(input, detectedFormat, safeFileName, deadlineUtc),
                overallTimeoutSource.Token);
            executionStopwatch.Stop();
            executionMilliseconds = executionStopwatch.ElapsedMilliseconds;
            var output = pipelineResult.Output;
            _logger.LogInformation(
                "PDF conversion completed - SourceIdentifier: {SourceIdentifier}, FinalConverter: {FinalConverter}, AttemptCount: {AttemptCount}, InputFormat: {InputFormat}, QueueMilliseconds: {QueueMilliseconds}, ExecutionMilliseconds: {ExecutionMilliseconds}, TotalMilliseconds: {TotalMilliseconds}, OutputBytes: {OutputBytes}",
                request.SourceIdentifier,
                pipelineResult.ConverterName,
                pipelineResult.AttemptCount,
                detectedFormat,
                queueMilliseconds,
                executionMilliseconds,
                totalStopwatch.ElapsedMilliseconds,
                output.Length);
            return output;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception) when (
            queueTimeoutSource?.IsCancellationRequested == true ||
            overallTimeoutSource.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Document conversion timed out - SourceIdentifier: {SourceIdentifier}, FileName: {FileName}, QueueMilliseconds: {QueueMilliseconds}, TotalMilliseconds: {TotalMilliseconds}",
                request.SourceIdentifier,
                safeFileName,
                queueMilliseconds,
                totalStopwatch.ElapsedMilliseconds);
            throw new DocumentConversionTimeoutException(ConversionErrorMessage, exception);
        }
        catch (Exception exception) when (IsExpectedDocumentFailure(exception))
        {
            _logger.LogError(
                exception,
                "Document conversion failed for a supported document - SourceIdentifier: {SourceIdentifier}, FileName: {FileName}, DeclaredFormat: {DeclaredFormat}, QueueMilliseconds: {QueueMilliseconds}, TotalMilliseconds: {TotalMilliseconds}",
                request.SourceIdentifier,
                safeFileName,
                declaredFormat,
                queueMilliseconds,
                totalStopwatch.ElapsedMilliseconds);
            throw new UnprocessableDocumentException(ConversionErrorMessage, exception);
        }
        finally
        {
            if (input is not null)
            {
                await input.DisposeAsync();
            }

            if (gateAcquired)
            {
                _concurrencyLimiter.Release();
            }

            queueTimeoutSource?.Dispose();
            totalStopwatch.Stop();
        }
    }

    private void ValidateDetectedFormat(
        string fileName,
        DocumentFormat declaredFormat,
        DocumentFormat detectedFormat)
    {
        if (detectedFormat is DocumentFormat.EncryptedOffice or DocumentFormat.Unknown)
        {
            _logger.LogError(
                "Document conversion rejected input - FileName: {FileName}, DeclaredFormat: {DeclaredFormat}, DetectedFormat: {DetectedFormat}",
                fileName,
                declaredFormat,
                detectedFormat);
            throw new UnprocessableDocumentException(ConversionErrorMessage);
        }

        if (detectedFormat != declaredFormat)
        {
            throw new UnsupportedDocumentFormatException(ConversionErrorMessage);
        }
    }

    private async Task<bool> IsEnabledAsync(string requestedExtension)
    {
        var normalizedRequestedExtension = NormalizeExtension(requestedExtension);
        if (normalizedRequestedExtension is null || _options.Formats is null ||
            !_options.Formats.Keys.Any(extension =>
                string.Equals(
                    NormalizeExtension(extension),
                    normalizedRequestedExtension,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var configuration = await _configurationService.GetConfigurationAsync();
        var configuredValues = configuration
            .FirstOrDefault(item => string.Equals(
                item.Key,
                ApplicationConfigurationKeys.TransitoryDocumentConvertibleExtensions,
                StringComparison.Ordinal))
            ?.Values;
        if (configuredValues is null || configuredValues.Count == 0)
        {
            return false;
        }

        return configuredValues
            .Select(NormalizeExtension)
            .Where(value => value is not null)
            .OfType<string>()
            .Where(value => DocumentFormatDetector.TryGetFormatForExtension(value, out _))
            .Contains(normalizedRequestedExtension, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<MemoryStream> CopyToBoundedStreamAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        if (!source.CanRead)
        {
            throw new UnprocessableDocumentException(ConversionErrorMessage);
        }

        if (source.CanSeek)
        {
            source.Position = 0;
            if (source.Length > _options.MaxInputSizeBytes)
            {
                throw new DocumentSizeLimitExceededException(ConversionErrorMessage);
            }
        }

        var destination = new BoundedMemoryStream(_options.MaxInputSizeBytes, ConversionErrorMessage);
        try
        {
            await source.CopyToAsync(destination, cancellationToken);
            if (destination.Length == 0)
            {
                throw new UnprocessableDocumentException(ConversionErrorMessage);
            }
            destination.Position = 0;
            return destination;
        }
        catch
        {
            await destination.DisposeAsync();
            throw;
        }
    }

    private static bool IsExpectedDocumentFailure(Exception exception) => exception is
        InvalidDataException or FormatException or IOException or UnprocessableDocumentException;

    internal static string NormalizeExtension(string extension)
    {
        var normalized = extension?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized[0] != '.') normalized = $".{normalized}";
        return normalized.Length is > 1 and <= 16 && normalized.Skip(1).All(char.IsLetterOrDigit)
            ? normalized
            : null;
    }
}
