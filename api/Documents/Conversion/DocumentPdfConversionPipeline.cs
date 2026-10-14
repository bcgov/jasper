using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

public sealed record DocumentPdfConversionContext(
    MemoryStream Input,
    DocumentFormat Format,
    string FileName,
    DateTimeOffset DeadlineUtc);

public enum DocumentPdfConversionAttemptStatus
{
    Succeeded,
    Unsupported,
    ConverterIncompatible,
    FidelityRejected,
    InvalidDocument,
    UnsafeDocument,
    ConverterUnavailable,
    ConverterTimedOut,
    ConverterExecutionFailed,
    InvalidConverterOutput,
    OutputLimitExceeded
}

public sealed record DocumentPdfConversionAttempt(
    string ConverterName,
    DocumentPdfConversionAttemptStatus Status,
    MemoryStream Output = null,
    Exception Exception = null)
{
    public static DocumentPdfConversionAttempt Succeeded(string converterName, MemoryStream output) =>
        new(converterName, DocumentPdfConversionAttemptStatus.Succeeded, output);

    public static DocumentPdfConversionAttempt Failed(
        string converterName,
        DocumentPdfConversionAttemptStatus status,
        Exception exception) =>
        new(converterName, status, Exception: exception);
}

public interface IDocumentPdfConversionStrategy
{
    DocumentPdfConverterId Id { get; }

    string Name { get; }

    bool CanConvert(DocumentFormat format);

    Task<DocumentPdfConversionAttempt> ConvertAsync(
        DocumentPdfConversionContext context,
        CancellationToken cancellationToken);
}

public enum DocumentPdfFallbackDecision
{
    Stop,
    TryNext
}

public interface IDocumentPdfFallbackPolicy
{
    DocumentPdfFallbackDecision Evaluate(
    DocumentPdfConversionAttempt attempt,
    DocumentPdfConversionContext context);
}

public sealed class DocumentPdfFallbackPolicy : IDocumentPdfFallbackPolicy
{
    private readonly TimeSpan _minimumRemainingDuration;

    public DocumentPdfFallbackPolicy(IOptions<DocumentConversionOptions> options)
    {
        _minimumRemainingDuration = TimeSpan.FromSeconds(options.Value.MinimumFallbackTimeSeconds);
    }

    public DocumentPdfFallbackDecision Evaluate(
        DocumentPdfConversionAttempt attempt,
        DocumentPdfConversionContext context)
    {
        var eligible = attempt.Status is DocumentPdfConversionAttemptStatus.Unsupported or
            DocumentPdfConversionAttemptStatus.ConverterIncompatible or
            DocumentPdfConversionAttemptStatus.FidelityRejected or
            DocumentPdfConversionAttemptStatus.ConverterUnavailable or
            DocumentPdfConversionAttemptStatus.ConverterExecutionFailed or
            DocumentPdfConversionAttemptStatus.InvalidConverterOutput;
        if (!eligible)
        {
            return DocumentPdfFallbackDecision.Stop;
        }

        return context.DeadlineUtc - DateTimeOffset.UtcNow >= _minimumRemainingDuration
            ? DocumentPdfFallbackDecision.TryNext
            : DocumentPdfFallbackDecision.Stop;
    }
}

internal sealed class DocumentPdfConversionPipeline
{
    internal sealed record Result(MemoryStream Output, string ConverterName, int AttemptCount);

    private readonly IDocumentPdfFallbackPolicy _fallbackPolicy;
    private readonly ILogger<DocumentPdfConversionPipeline> _logger;
    private readonly IDocumentPdfRouteTable _routeTable;

    public DocumentPdfConversionPipeline(
        IDocumentPdfFallbackPolicy fallbackPolicy,
        ILogger<DocumentPdfConversionPipeline> logger,
        IDocumentPdfRouteTable routeTable)
    {
        _fallbackPolicy = fallbackPolicy;
        _logger = logger;
        _routeTable = routeTable;
    }

    internal async Task<Result> ConvertAsync(
        DocumentPdfConversionContext context,
        CancellationToken cancellationToken)
    {
        var totalStopwatch = Stopwatch.StartNew();
        DocumentPdfConversionAttempt lastAttempt = null;
        var strategies = _routeTable.GetStrategies(
            context.Format,
            Path.GetExtension(context.FileName));
        var attemptNumber = 0;
        foreach (var strategy in strategies)
        {
            attemptNumber++;
            cancellationToken.ThrowIfCancellationRequested();
            context.Input.Position = 0;

            var stopwatch = Stopwatch.StartNew();
            var attempt = await strategy.ConvertAsync(context, cancellationToken);
            stopwatch.Stop();
            if (attempt.Status == DocumentPdfConversionAttemptStatus.Succeeded)
            {
                var output = attempt.Output ?? throw new InvalidOperationException(
                    $"Converter '{attempt.ConverterName}' reported success without a PDF output stream.");
                output.Position = 0;
                _logger.LogInformation(
                    "PDF conversion attempt completed - Converter: {Converter}, InputFormat: {InputFormat}, AttemptNumber: {AttemptNumber}, AttemptStatus: {AttemptStatus}, FallbackReason: {FallbackReason}, ProcessExitCode: {ProcessExitCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, OutputBytes: {OutputBytes}",
                    attempt.ConverterName,
                    context.Format,
                    attemptNumber,
                    attempt.Status,
                    null,
                    null,
                    stopwatch.ElapsedMilliseconds,
                    output.Length);
                _logger.LogInformation(
                    "PDF conversion pipeline completed - FinalConverter: {FinalConverter}, InputFormat: {InputFormat}, AttemptCount: {AttemptCount}, TotalMilliseconds: {TotalMilliseconds}, OutputBytes: {OutputBytes}",
                    attempt.ConverterName,
                    context.Format,
                    attemptNumber,
                    totalStopwatch.ElapsedMilliseconds,
                    output.Length);
                return new Result(output, attempt.ConverterName, attemptNumber);
            }

            var outputBytes = attempt.Output?.Length ?? 0;
            if (attempt.Output is not null)
            {
                await attempt.Output.DisposeAsync();
            }

            lastAttempt = attempt;
            var decision = _fallbackPolicy.Evaluate(attempt, context);
            _logger.LogWarning(
                attempt.Exception,
                "PDF conversion attempt failed - Converter: {Converter}, InputFormat: {InputFormat}, AttemptNumber: {AttemptNumber}, AttemptStatus: {AttemptStatus}, FallbackReason: {FallbackReason}, Decision: {Decision}, ProcessExitCode: {ProcessExitCode}, ElapsedMilliseconds: {ElapsedMilliseconds}, OutputBytes: {OutputBytes}",
                attempt.ConverterName,
                context.Format,
                attemptNumber,
                attempt.Status,
                attempt.Status,
                decision,
                null,
                stopwatch.ElapsedMilliseconds,
                outputBytes);
            if (decision == DocumentPdfFallbackDecision.Stop)
            {
                throw attempt.Exception ?? new InvalidOperationException(
                    $"Converter '{attempt.ConverterName}' failed without an exception.");
            }

        }

        if (lastAttempt?.Exception is not null)
        {
            throw lastAttempt.Exception;
        }

        throw new UnsupportedDocumentFormatException(DocumentPdfConversionService.ConversionErrorMessage);
    }
}

public sealed class DocumentFidelityRejectedException : Exception
{
    public DocumentFidelityRejectedException(string message)
        : base(message)
    {
    }

    public DocumentFidelityRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DocumentParsingRejectedException : Exception
{
    public DocumentParsingRejectedException(string message)
        : base(message)
    {
    }

    public DocumentParsingRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
