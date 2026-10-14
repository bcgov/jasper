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
            var metadata = ImageConversionPreflight.InspectAndValidate(encodedBytes, _options);
            var imageSources = ImageConversionPreflight.ExecuteWithMagickResourceLimits(
                _options,
                () => DecodeAndNormalizeImages(
                    encodedBytes,
                    metadata,
                    context.FileName,
                    cancellationToken));

            var document = PdfDocument.CreateFromImages(
                imageSources,
                new PdfImageDocumentOptions
                {
                    MaximumEncodedImageBytes = _options.MaxOutputSizeBytes,
                    FixedPageSize = PageSizes.A4
                },
                cancellationToken);
            var conversion = new PdfDocumentConversionResult(document, new PdfConversionReport());
            var writeResult = await PdfOutputWriter.WriteAsync(
                conversion,
                _options.MaxOutputSizeBytes,
                DocumentPdfConversionService.ConversionErrorMessage,
                cancellationToken);
            if (!writeResult.Succeeded)
            {
                return DocumentPdfConversionAttempt.Failed(
                    Name,
                    DocumentPdfConversionAttemptStatus.InvalidConverterOutput,
                    writeResult.SaveResult.Exception ?? new UnprocessableDocumentException(
                        DocumentPdfConversionService.ConversionErrorMessage));
            }

            return DocumentPdfConversionAttempt.Succeeded(Name, writeResult.Output);
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
        catch (MagickException exception) when (
            ImageConversionPreflight.IsResourceLimitFailure(exception))
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

    private List<PdfImageDocumentSource> DecodeAndNormalizeImages(
        byte[] encodedBytes,
        ImageConversionMetadata metadata,
        string fileName,
        CancellationToken cancellationToken)
    {
        using var images = new MagickImageCollection();
        images.Read(encodedBytes);

        if (images.Count != metadata.FrameCount)
        {
            throw new InvalidDataException(
                $"Magick metadata reported {metadata.FrameCount} frames but decoded {images.Count} frames.");
        }

        var imageSources = new List<PdfImageDocumentSource>(images.Count);
        var frameNumber = 0;
        foreach (var image in images)
        {
            cancellationToken.ThrowIfCancellationRequested();
            frameNumber++;
            if (image.Width == 0 || image.Height == 0)
            {
                throw new InvalidDataException($"Image frame {frameNumber} has invalid dimensions.");
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
                $"{fileName}#{frameNumber}"));
        }

        return imageSources;
    }
}
