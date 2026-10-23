using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Scv.Api.Infrastructure.Options;

public sealed class DocumentConversionOptions
{
    public const string SectionName = "DocumentConversion";

    [Range(1, int.MaxValue)]
    public long MaxInputSizeBytes { get; set; } = 100L * 1024L * 1024L;

    [Range(1, int.MaxValue)]
    public long MaxOutputSizeBytes { get; set; } = 200L * 1024L * 1024L;

    [Range(1, int.MaxValue)]
    public long MaxAggregateConvertedOutputSizeBytes { get; set; } = 400L * 1024L * 1024L;

    [Range(1, int.MaxValue)]
    public long MaxBase64ResponseSizeBytes { get; set; } = 280L * 1024L * 1024L;

    [Range(1, int.MaxValue)]
    public int MaxPageCount { get; set; } = 1000;

    [Range(1, int.MaxValue)]
    public long MaxDecodedImagePixels { get; set; } = 100_000_000;

    [Range(1, int.MaxValue)]
    public int MaxImageDimension { get; set; } = 20_000;

    [Range(1, int.MaxValue)]
    public int MaxImageFrames { get; set; } = 1000;

    [Range(1, int.MaxValue)]
    public long MaxImageMemoryBytes { get; set; } = 256L * 1024L * 1024L;

    [Range(1, int.MaxValue)]
    public long MaxImageDiskBytes { get; set; } = 512L * 1024L * 1024L;

    [Range(1, 32)]
    public int MaxImageThreads { get; set; } = 1;

    [Range(1, 32)]
    public int MaxConcurrentConversions { get; set; } = 2;

    [Range(1, 8)]
    public int MaxConcurrentLibreOfficeConversions { get; set; } = 1;

    [Range(1, 3600)]
    public int OverallConversionTimeoutSeconds { get; set; } = 60;

    [Range(1, 3600)]
    public int MaximumQueueWaitSeconds { get; set; } = 60;

    [Required(AllowEmptyStrings = false)]
    public string LibreOfficeExecutablePath { get; set; } = "soffice";

    [Range(1, 3600)]
    public int LibreOfficeProcessTimeoutSeconds { get; set; } = 45;

    [Range(1, 300)]
    public int MinimumFallbackTimeSeconds { get; set; } = 5;

    public Dictionary<string, List<string>> Formats { get; set; } = new();
}
