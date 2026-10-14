using System.Collections.Generic;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Xunit;

namespace tests.api.Documents.Conversion;

public class DocumentConversionOptionsValidatorTests
{
    [Fact]
    public void Validate_AcceptsExtensionRoutesAndExplicitPassthrough()
    {
        var result = new DocumentConversionOptionsValidator().Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsUnknownExtensionAndOverlappingLibreOfficeTimeout()
    {
        var options = ValidOptions();
        options.Formats[".unknown"] = ["OfficeIMO"];
        options.LibreOfficeProcessTimeoutSeconds = options.OverallConversionTimeoutSeconds;

        var result = new DocumentConversionOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("not registered"));
        Assert.Contains(result.Failures, failure => failure.Contains("must be less"));
    }

    [Fact]
    public void Validate_RejectsEmptyConverterRoute()
    {
        var options = ValidOptions();
        options.Formats[".xlsx"] = [];

        var result = new DocumentConversionOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("at least one converter"));
    }

    private static DocumentConversionOptions ValidOptions() => new()
    {
        Formats = new Dictionary<string, List<string>>
        {
            [".pdf"] = ["passthrough"],
            [".docx"] = ["OfficeIMO", "LibreOffice"],
            [".xlsx"] = ["OfficeIMO", "LibreOffice"],
            [".png"] = ["OfficeIMO", "Magick"]
        }
    };
}
