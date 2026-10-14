using System.Collections.Generic;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Xunit;

namespace tests.api.Documents.Conversion;

public class DocumentConversionOptionsValidatorTests
{
    [Fact]
    public void Validate_AcceptsDefaultEnabledExtensionsAndFamilyRoutes()
    {
        var result = new DocumentConversionOptionsValidator().Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsUnknownFamilyAndOverlappingLibreOfficeTimeout()
    {
        var options = ValidOptions();
        options.Routes["Unknown"] = ["OfficeIMO"];
        options.LibreOfficeProcessTimeoutSeconds = options.OverallConversionTimeoutSeconds;

        var result = new DocumentConversionOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Unknown"));
        Assert.Contains(result.Failures, failure => failure.Contains("must be less"));
    }

    [Fact]
    public void Validate_RejectsEnabledExtensionWithoutFamilyRoute()
    {
        var options = ValidOptions();
        options.Routes.Remove("Spreadsheet");

        var result = new DocumentConversionOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Spreadsheet"));
    }

    private static DocumentConversionOptions ValidOptions() => new()
    {
        DefaultEnabledExtensions = [".pdf", ".docx", ".xlsx", ".png"],
        Routes = new Dictionary<string, List<string>>
        {
            ["Word"] = ["OfficeIMO", "LibreOffice"],
            ["Spreadsheet"] = ["OfficeIMO", "LibreOffice"],
            ["Presentation"] = ["OfficeIMO", "LibreOffice"],
            ["Image"] = ["OfficeIMO", "Magick"]
        }
    };
}
