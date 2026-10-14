using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Xunit;
using ConversionDocumentFormat = Scv.Api.Documents.Conversion.DocumentFormat;

namespace tests.api.Documents.Conversion;

public class DocumentPdfRouteTableTests
{
    public static TheoryData<ConversionDocumentFormat> OfficeFormats => new()
    {
        ConversionDocumentFormat.Xls,
        ConversionDocumentFormat.Xlsx,
        ConversionDocumentFormat.Xlsm,
        ConversionDocumentFormat.Xltx,
        ConversionDocumentFormat.Xltm,
        ConversionDocumentFormat.Xlsb,
        ConversionDocumentFormat.Docx,
        ConversionDocumentFormat.Pptx
    };

    [Theory]
    [MemberData(nameof(OfficeFormats))]
    public void GetStrategies_UsesDetectedFamilyAndProductionOrdering(
        ConversionDocumentFormat format)
    {
        var routeTable = CreateProductionRouteTable();

        var selected = routeTable.GetStrategies(format);

        Assert.Equal(
            [DocumentPdfConverterId.OfficeIMO, DocumentPdfConverterId.LibreOffice],
            selected.Select(strategy => strategy.Id));
    }

    [Fact]
    public void Constructor_RejectsUnregisteredConverter()
    {
        var options = new DocumentConversionOptions
        {
            Routes = new Dictionary<string, List<string>>
            {
                ["Word"] = ["LibreOffice"]
            }
        };

        var exception = Assert.Throws<OptionsValidationException>(() =>
            new DocumentPdfRouteTable(
                Options.Create(options),
                [CreateStrategy(DocumentPdfConverterId.OfficeIMO)]));

        Assert.Contains("unregistered converter 'LibreOffice'", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsConverterThatDoesNotSupportRouteFamily()
    {
        var options = new DocumentConversionOptions
        {
            Routes = new Dictionary<string, List<string>>
            {
                ["Word"] = ["OfficeIMO"]
            }
        };
        var strategy = new Mock<IDocumentPdfConversionStrategy>();
        strategy.SetupGet(value => value.Id).Returns(DocumentPdfConverterId.OfficeIMO);
        strategy.Setup(value => value.CanConvert(It.IsAny<ConversionDocumentFormat>())).Returns(false);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            new DocumentPdfRouteTable(Options.Create(options), [strategy.Object]));

        Assert.Contains("does not support format", exception.Message);
    }

    private static DocumentPdfRouteTable CreateProductionRouteTable()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(System.AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        var options = new DocumentConversionOptions();
        configuration.GetSection(DocumentConversionOptions.SectionName).Bind(options);
        return new DocumentPdfRouteTable(Options.Create(options), CreateStrategies());
    }

    private static IReadOnlyList<IDocumentPdfConversionStrategy> CreateStrategies() =>
    [
        CreateStrategy(DocumentPdfConverterId.OfficeIMO),
        CreateStrategy(DocumentPdfConverterId.LibreOffice),
        CreateStrategy(DocumentPdfConverterId.Magick)
    ];

    private static IDocumentPdfConversionStrategy CreateStrategy(DocumentPdfConverterId id)
    {
        var strategy = new Mock<IDocumentPdfConversionStrategy>();
        strategy.SetupGet(value => value.Id).Returns(id);
        strategy.Setup(value => value.CanConvert(It.IsAny<ConversionDocumentFormat>())).Returns(true);
        return strategy.Object;
    }
}
