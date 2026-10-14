using System;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Scv.Core.Exceptions;
using Xunit;

namespace tests.api.Documents.Conversion;

public class ImageConversionPreflightTests
{
    [Theory]
    [InlineData(10u, 1u)]
    [InlineData(1u, 10u)]
    public void Validate_AcceptsDimensionsExactlyAtMaximum(uint width, uint height)
    {
        var options = CreateOptions(maximumDimension: 10, maximumPixels: 10);

        var metadata = ImageConversionPreflight.Validate(
            [new ImageFrameMetadata(width, height)],
            options);

        Assert.Equal(1, metadata.FrameCount);
        Assert.Equal(10, metadata.TotalDecodedPixels);
    }

    [Theory]
    [InlineData(11u, 1u)]
    [InlineData(1u, 11u)]
    public void Validate_RejectsDimensionsAboveMaximum(uint width, uint height)
    {
        var options = CreateOptions(maximumDimension: 10, maximumPixels: 100);

        Assert.Throws<DocumentSizeLimitExceededException>(() =>
            ImageConversionPreflight.Validate(
                [new ImageFrameMetadata(width, height)],
                options));
    }

    [Fact]
    public void Validate_AcceptsPixelsExactlyAtMaximum()
    {
        var options = CreateOptions(maximumDimension: 10, maximumPixels: 100);

        var metadata = ImageConversionPreflight.Validate(
            [new ImageFrameMetadata(10, 10)],
            options);

        Assert.Equal(100, metadata.TotalDecodedPixels);
    }

    [Fact]
    public void Validate_RejectsPixelsAboveMaximum()
    {
        var options = CreateOptions(maximumDimension: 11, maximumPixels: 100);

        Assert.Throws<DocumentSizeLimitExceededException>(() =>
            ImageConversionPreflight.Validate(
                [new ImageFrameMetadata(11, 10)],
                options));
    }

    [Fact]
    public void Validate_ClassifiesCheckedPixelOverflowAsResourceLimit()
    {
        var options = CreateOptions(
            maximumDimension: int.MaxValue,
            maximumPixels: long.MaxValue);

        Assert.Throws<DocumentSizeLimitExceededException>(() =>
            ImageConversionPreflight.Validate(
                [
                    new ImageFrameMetadata(int.MaxValue, int.MaxValue),
                    new ImageFrameMetadata(int.MaxValue, int.MaxValue),
                    new ImageFrameMetadata(int.MaxValue, int.MaxValue)
                ],
                options));
    }

    [Fact]
    public void Validate_AcceptsFramesExactlyAtMaximumAndAggregatesPixels()
    {
        var options = CreateOptions(
            maximumDimension: 10,
            maximumPixels: 18,
            maximumFrames: 2);

        var metadata = ImageConversionPreflight.Validate(
            [new ImageFrameMetadata(3, 3), new ImageFrameMetadata(3, 3)],
            options);

        Assert.Equal(2, metadata.FrameCount);
        Assert.Equal(18, metadata.TotalDecodedPixels);
    }

    [Fact]
    public void Validate_RejectsFramesAboveMaximum()
    {
        var options = CreateOptions(maximumFrames: 1);

        Assert.Throws<DocumentSizeLimitExceededException>(() =>
            ImageConversionPreflight.Validate(
                [new ImageFrameMetadata(1, 1), new ImageFrameMetadata(1, 1)],
                options));
    }

    [Fact]
    public void Validate_RejectsAggregatePixelsAcrossFrames()
    {
        var options = CreateOptions(
            maximumDimension: 10,
            maximumPixels: 17,
            maximumFrames: 2);

        Assert.Throws<DocumentSizeLimitExceededException>(() =>
            ImageConversionPreflight.Validate(
                [new ImageFrameMetadata(3, 3), new ImageFrameMetadata(3, 3)],
                options));
    }

    private static DocumentConversionOptions CreateOptions(
        int maximumDimension = 100,
        long maximumPixels = 10_000,
        int maximumFrames = 10) => new()
        {
            MaxImageDimension = maximumDimension,
            MaxDecodedImagePixels = maximumPixels,
            MaxImageFrames = maximumFrames,
            MaxPageCount = maximumFrames
        };
}
