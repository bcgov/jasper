using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;

namespace Scv.Api.Documents.Conversion;

internal readonly record struct ImageFrameMetadata(uint Width, uint Height);

internal sealed record ImageConversionMetadata(int FrameCount, long TotalDecodedPixels);

internal static class ImageConversionPreflight
{
    private static readonly object MagickResourceLimitsLock = new();

    internal static ImageConversionMetadata InspectAndValidate(
        byte[] encodedImage,
        DocumentConversionOptions options)
    {
        ImageFrameMetadata[] frames;
        lock (MagickResourceLimitsLock)
        {
            ConfigureMagickMetadataLimits(options);

            using var images = new MagickImageCollection();
            try
            {
                // Ping reads frame metadata without allocating each frame's pixel cache.
                images.Ping(encodedImage);
            }
            catch (MagickException exception) when (IsResourceLimitFailure(exception))
            {
                throw ResourceLimit(
                    "Image metadata inspection exceeded a configured resource limit.",
                    exception);
            }
            catch (MagickException exception)
            {
                throw new InvalidDataException("Image metadata could not be read.", exception);
            }

            frames = images
                .Select(image => new ImageFrameMetadata(image.Width, image.Height))
                .ToArray();
            ConfigureMagickResourceLimits(options);
        }

        return Validate(frames, options);
    }

    internal static ImageConversionMetadata Validate(
        IReadOnlyList<ImageFrameMetadata> frames,
        DocumentConversionOptions options)
    {
        if (frames.Count == 0)
        {
            throw new InvalidDataException("The image contains no frames.");
        }

        var maximumFrames = Math.Min(options.MaxPageCount, options.MaxImageFrames);
        if (frames.Count > maximumFrames)
        {
            throw ResourceLimit(
                $"The image contains {frames.Count} frames, exceeding the configured limit of {maximumFrames}.");
        }

        long totalDecodedPixels = 0;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            if (frame.Width == 0 || frame.Height == 0)
            {
                throw new InvalidDataException($"Image frame {index + 1} has invalid dimensions.");
            }

            if (frame.Width > options.MaxImageDimension || frame.Height > options.MaxImageDimension)
            {
                throw ResourceLimit(
                    $"Image frame {index + 1} dimensions {frame.Width}x{frame.Height} exceed the configured maximum dimension of {options.MaxImageDimension}.");
            }

            try
            {
                totalDecodedPixels = checked(
                    totalDecodedPixels + checked((long)frame.Width * frame.Height));
            }
            catch (OverflowException exception)
            {
                throw ResourceLimit("The aggregate decoded image pixel count overflowed.", exception);
            }

            if (totalDecodedPixels > options.MaxDecodedImagePixels)
            {
                throw ResourceLimit(
                    $"The image requires {totalDecodedPixels} decoded pixels, exceeding the configured limit of {options.MaxDecodedImagePixels}.");
            }
        }

        return new ImageConversionMetadata(frames.Count, totalDecodedPixels);
    }

    private static void ConfigureMagickResourceLimits(DocumentConversionOptions options)
    {
        // ImageMagick treats these native thresholds as exclusive. Add one so the
        // converter-neutral policy remains inclusive at its configured maxima.
        ResourceLimits.Area = checked((ulong)options.MaxDecodedImagePixels + 1);
        ResourceLimits.Width = checked((ulong)options.MaxImageDimension + 1);
        ResourceLimits.Height = checked((ulong)options.MaxImageDimension + 1);
        ResourceLimits.ListLength = checked(
            (ulong)Math.Min(options.MaxPageCount, options.MaxImageFrames) + 1);
        ResourceLimits.Memory = checked((ulong)options.MaxImageMemoryBytes);
        ResourceLimits.Disk = checked((ulong)options.MaxImageDiskBytes);
        ResourceLimits.Thread = checked((ulong)options.MaxImageThreads);
    }

    internal static T ExecuteWithMagickResourceLimits<T>(
        DocumentConversionOptions options,
        Func<T> operation)
    {
        lock (MagickResourceLimitsLock)
        {
            ConfigureMagickResourceLimits(options);
            return operation();
        }
    }

    private static void ConfigureMagickMetadataLimits(DocumentConversionOptions options)
    {
        // Ping does not allocate pixel caches. Leave pixel geometry broad enough for Ping to
        // return hostile dimensions to the shared validator instead of surfacing a codec error.
        ResourceLimits.Area = ulong.MaxValue;
        ResourceLimits.Width = uint.MaxValue;
        ResourceLimits.Height = uint.MaxValue;
        ResourceLimits.ListLength = checked(
            (ulong)Math.Min(options.MaxPageCount, options.MaxImageFrames) + 1);
        ResourceLimits.Memory = checked((ulong)options.MaxImageMemoryBytes);
        ResourceLimits.Disk = checked((ulong)options.MaxImageDiskBytes);
        ResourceLimits.Thread = checked((ulong)options.MaxImageThreads);
    }

    internal static bool IsResourceLimitFailure(MagickException exception) =>
        exception is MagickResourceLimitErrorException or MagickResourceLimitWarningException ||
        exception.Message.Contains("resource limit", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("cache resources exhausted", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("CacheResourcesExhausted", StringComparison.OrdinalIgnoreCase);

    private static DocumentSizeLimitExceededException ResourceLimit(
        string reason,
        Exception innerException = null) =>
        innerException is null
            ? new DocumentSizeLimitExceededException(
                DocumentPdfConversionService.ConversionErrorMessage,
                new InvalidDataException(reason))
            : new DocumentSizeLimitExceededException(
                DocumentPdfConversionService.ConversionErrorMessage,
                new InvalidDataException(reason, innerException));
}
