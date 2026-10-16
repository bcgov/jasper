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
    public static TheoryData<ConversionDocumentFormat, string> OfficeFormats => new()
    {
        { ConversionDocumentFormat.Xls, ".xls" },
        { ConversionDocumentFormat.Xlsx, ".xlsx" },
        { ConversionDocumentFormat.Xlsm, ".xlsm" },
        { ConversionDocumentFormat.Xltx, ".xltx" },
        { ConversionDocumentFormat.Xltm, ".xltm" },
        { ConversionDocumentFormat.Xlsb, ".xlsb" },
        { ConversionDocumentFormat.Docx, ".docx" },
        { ConversionDocumentFormat.Pptx, ".pptx" }
    };

    [Theory]
    [MemberData(nameof(OfficeFormats))]
    public void GetStrategies_UsesConfiguredExtensionOrdering(
        ConversionDocumentFormat format,
        string extension)
    {
        var routeTable = CreateProductionRouteTable();

        var selected = routeTable.GetStrategies(format, extension);

        Assert.Equal(
            [DocumentPdfConverterId.OfficeIMO, DocumentPdfConverterId.LibreOffice],
            selected.Select(strategy => strategy.Id));
    }

    [Fact]
    public void Constructor_RejectsUnregisteredConverter()
    {
        var options = new DocumentConversionOptions
        {
            Formats = new Dictionary<string, List<string>>
            {
                [".docx"] = ["LibreOffice"]
            }
        };

        var exception = Assert.Throws<OptionsValidationException>(() =>
            new DocumentPdfRouteTable(
                Options.Create(options),
                [CreateStrategy(DocumentPdfConverterId.OfficeIMO)]));

        Assert.Contains("unregistered converter 'LibreOffice'", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsConverterThatDoesNotSupportFormat()
    {
        var options = new DocumentConversionOptions
        {
            Formats = new Dictionary<string, List<string>>
            {
                [".docx"] = ["OfficeIMO"]
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
