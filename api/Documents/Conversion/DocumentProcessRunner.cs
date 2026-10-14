using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Documents.Conversion;

internal sealed record DocumentProcessRequest(ProcessStartInfo StartInfo);

internal sealed record DocumentProcessResult(
    int ExitCode,
    long ElapsedMilliseconds);

internal interface IDocumentProcessRunner
{
    Task<DocumentProcessResult> RunAsync(
        DocumentProcessRequest request,
        CancellationToken cancellationToken);
}

internal sealed class DocumentProcessRunner(ILogger<DocumentProcessRunner> logger) : IDocumentProcessRunner
{
    private static readonly TimeSpan ProcessCleanupTimeout = TimeSpan.FromSeconds(5);

    public async Task<DocumentProcessResult> RunAsync(
        DocumentProcessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.StartInfo);
        request.StartInfo.UseShellExecute = false;
        request.StartInfo.RedirectStandardOutput = true;
        request.StartInfo.RedirectStandardError = true;
        request.StartInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = request.StartInfo };
        var stopwatch = Stopwatch.StartNew();
        if (!process.Start())
        {
            throw new InvalidOperationException("The document conversion process could not be started.");
        }

        var standardOutput = DrainAsync(process.StandardOutput.BaseStream, cancellationToken);
        var standardError = DrainAsync(process.StandardError.BaseStream, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(standardOutput, standardError);
            stopwatch.Stop();
            return new DocumentProcessResult(
                process.ExitCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            try
            {
                using var cleanupTimeout = new CancellationTokenSource(ProcessCleanupTimeout);
                await process.WaitForExitAsync(cleanupTimeout.Token);
            }
            catch (OperationCanceledException exception)
            {
                logger.LogError(
                    exception,
                    "Document conversion process did not confirm termination within {CleanupTimeoutSeconds} seconds. ProcessId: {ProcessId}",
                    ProcessCleanupTimeout.TotalSeconds,
                    process.Id);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the kill request and the cleanup wait.
            }

            try
            {
                await Task.WhenAll(standardOutput, standardError);
            }
            catch (Exception exception) when (exception is OperationCanceledException or IOException)
            {
                // Expected: cancellation or process termination closes the redirected streams.
            }

            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static Task DrainAsync(Stream stream, CancellationToken cancellationToken) =>
        stream.CopyToAsync(Stream.Null, cancellationToken);

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // The process already exited.
        }
    }
}
