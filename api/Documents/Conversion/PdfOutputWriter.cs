using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfficeIMO.Pdf;

namespace Scv.Api.Documents.Conversion;

internal enum PdfOutputFailure
{
    None,
    SerializationFailed,
    EmptyOutput,
    InvalidSignature
}

internal sealed record PdfOutputWriteResult(
    MemoryStream Output,
    PdfSaveResult SaveResult,
    PdfOutputFailure Failure,
    long OutputBytes)
{
    public bool Succeeded => Failure == PdfOutputFailure.None;
}

internal static class PdfOutputWriter
{
    public static async Task<PdfOutputWriteResult> WriteAsync(
        PdfDocumentConversionResult conversion,
        long maximumOutputBytes,
        string errorMessage,
        CancellationToken cancellationToken,
        Action<PdfSaveResult> validateSaveResult = null)
    {
        var output = new BoundedMemoryStream(maximumOutputBytes, errorMessage);
        var saveResult = await conversion.SaveAsync(output, cancellationToken);
        if (!saveResult.Succeeded)
        {
            var outputBytes = output.Length;
            await output.DisposeAsync();
            return new PdfOutputWriteResult(
                null,
                saveResult,
                PdfOutputFailure.SerializationFailed,
                outputBytes);
        }

        validateSaveResult?.Invoke(saveResult);

        var failure = output.Length switch
        {
            0 => PdfOutputFailure.EmptyOutput,
            _ when !PdfFileValidation.HasPdfSignature(output) => PdfOutputFailure.InvalidSignature,
            _ => PdfOutputFailure.None
        };
        if (failure != PdfOutputFailure.None)
        {
            var outputBytes = output.Length;
            await output.DisposeAsync();
            return new PdfOutputWriteResult(null, saveResult, failure, outputBytes);
        }

        output.Position = 0;
        return new PdfOutputWriteResult(
            output,
            saveResult,
            PdfOutputFailure.None,
            output.Length);
    }
}
