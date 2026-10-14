using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.Extensions.Options;
using OfficeIMO.Pdf;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal sealed class MagickDocumentPdfConversionStrategy : IDocumentPdfConversionStrategy
{
    private const uint A4PortraitWidthAt300Dpi = 2480;
    private const uint A4PortraitHeightAt300Dpi = 3508;

    private readonly DocumentConversionOptions _options;

    public MagickDocumentPdfConversionStrategy(IOptions<DocumentConversionOptions> options)
    {
        _options = options.Value;
    }

    public DocumentPdfConverterId Id => DocumentPdfConverterId.Magick;

    public string Name => "Magick";

    public bool CanConvert(DocumentFormat format) => format is
        DocumentFormat.Png or DocumentFormat.Jpeg or DocumentFormat.Tiff or DocumentFormat.Webp;

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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Input.Position = 0;
            var encodedBytes = context.Input.ToArray();
            ConfigureResourceLimits();
            using var images = new MagickImageCollection();
            images.Read(encodedBytes);

            if (images.Count == 0)
            {
                return InvalidDocument("Magick did not find an image frame in the input.");
            }

            if (images.Count > Math.Min(_options.MaxPageCount, _options.MaxImageFrames))
            {
                return OutputLimit(
                    $"The image contains {images.Count} frames, exceeding the configured page limit.");
            }

            var imageSources = new List<PdfImageDocumentSource>(images.Count);
            long normalizedPixels = 0;
            var frameNumber = 0;
            foreach (var image in images)
            {
                cancellationToken.ThrowIfCancellationRequested();
                frameNumber++;
                if (image.Width == 0 || image.Height == 0)
                {
                    return InvalidDocument($"Image frame {frameNumber} has invalid dimensions.");
                }

                var targetWidth = image.Width >= image.Height
                    ? A4PortraitHeightAt300Dpi
                    : A4PortraitWidthAt300Dpi;
                var targetHeight = image.Width >= image.Height
                    ? A4PortraitWidthAt300Dpi
                    : A4PortraitHeightAt300Dpi;
                if (image.Width > targetWidth || image.Height > targetHeight)
                {
                    image.Resize(new MagickGeometry(targetWidth, targetHeight));
                }

                normalizedPixels = checked(normalizedPixels + checked((long)image.Width * image.Height));
                if (normalizedPixels > _options.MaxDecodedImagePixels)
                {
                    return OutputLimit("The normalized image frames exceed the configured pixel limit.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                image.AutoOrient();
                image.TransformColorSpace(ColorProfiles.SRGB);
                image.Strip();

                using var normalized = new BoundedMemoryStream(
                    _options.MaxOutputSizeBytes,
                    DocumentPdfConversionService.ConversionErrorMessage);
                image.Write(normalized, MagickFormat.Png);
                imageSources.Add(new PdfImageDocumentSource(
                    normalized.ToArray(),
                    $"{context.FileName}#{frameNumber}"));
            }

            var document = PdfDocument.CreateFromImages(
                imageSources,
                new PdfImageDocumentOptions
                {
                    MaximumEncodedImageBytes = _options.MaxOutputSizeBytes,
                    FixedPageSize = PageSizes.A4
                },
                cancellationToken);
            var conversion = new PdfDocumentConversionResult(document, new PdfConversionReport());
            var output = new BoundedMemoryStream(
                _options.MaxOutputSizeBytes,
                DocumentPdfConversionService.ConversionErrorMessage);
            try
            {
                var saveResult = await conversion.SaveAsync(output, cancellationToken);
                if (!saveResult.Succeeded || output.Length == 0 ||
                    !PdfFileValidation.HasPdfSignature(output))
                {
                    await output.DisposeAsync();
                    return DocumentPdfConversionAttempt.Failed(
                        Name,
                        DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                        saveResult.Exception ?? new UnprocessableDocumentException(
                            DocumentPdfConversionService.ConversionErrorMessage));
                }

                output.Position = 0;
                return DocumentPdfConversionAttempt.Succeeded(Name, output);
            }
            catch
            {
                await output.DisposeAsync();
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DocumentSizeLimitExceededException exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                exception);
        }
        catch (MagickException exception) when (IsResourceLimitFailure(exception))
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
                new DocumentSizeLimitExceededException(
                    DocumentPdfConversionService.ConversionErrorMessage,
                    exception));
        }
        catch (Exception exception) when (
            exception is MagickException or InvalidDataException or OverflowException)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.InvalidDocument,
                new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage,
                    exception));
        }
        catch (Exception exception)
        {
            return DocumentPdfConversionAttempt.Failed(
                Name,
                DocumentPdfConversionAttemptStatus.ConverterExecutionFailed,
                new UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage,
                    exception));
        }
    }

    private DocumentPdfConversionAttempt InvalidDocument(string reason) =>
        DocumentPdfConversionAttempt.Failed(
            Name,
            DocumentPdfConversionAttemptStatus.InvalidDocument,
            new UnprocessableDocumentException(
                DocumentPdfConversionService.ConversionErrorMessage,
                new InvalidDataException(reason)));

    private void ConfigureResourceLimits()
    {
        ResourceLimits.Area = checked((ulong)_options.MaxDecodedImagePixels);
        ResourceLimits.Width = checked((ulong)_options.MaxImageDimension);
        ResourceLimits.Height = checked((ulong)_options.MaxImageDimension);
        ResourceLimits.ListLength = checked((ulong)_options.MaxImageFrames);
        ResourceLimits.Memory = checked((ulong)_options.MaxImageMemoryBytes);
        ResourceLimits.Disk = checked((ulong)_options.MaxImageDiskBytes);
        ResourceLimits.Thread = checked((ulong)_options.MaxImageThreads);
    }

    private static bool IsResourceLimitFailure(MagickException exception) =>
        exception.Message.Contains("resource limit", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("cache resources exhausted", StringComparison.OrdinalIgnoreCase);

    private DocumentPdfConversionAttempt OutputLimit(string reason) =>
        DocumentPdfConversionAttempt.Failed(
            Name,
            DocumentPdfConversionAttemptStatus.OutputLimitExceeded,
            new DocumentSizeLimitExceededException(
                DocumentPdfConversionService.ConversionErrorMessage,
                new InvalidDataException(reason)));

}
