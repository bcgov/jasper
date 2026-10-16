using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents.Conversion;
using Scv.Api.Documents.Strategies;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;
using Xunit;

namespace tests.api.Documents.Strategies;

public class TransitoryDocumentStrategyTests
{
    [Fact]
    public async Task Invoke_RewindsSourceStreamBeforeCopying()
    {
        var contentBytes = Encoding.UTF8.GetBytes("%PDF-test-content");
        var sourceStream = new MemoryStream(contentBytes);
        sourceStream.Position = sourceStream.Length;
        var transitoryDocumentsServiceMock = new Mock<ITransitoryDocumentsService>();
        var conversionServiceMock = new Mock<IDocumentPdfConversionService>();
        var loggerMock = new Mock<ILogger<TransitoryDocumentStrategy>>();

        transitoryDocumentsServiceMock
            .Setup(x => x.DownloadFile("path/to/document.pdf", default))
            .ReturnsAsync(new FileStreamResponse(
                sourceStream,
                "document.pdf",
                "application/pdf"));
        conversionServiceMock
            .Setup(x => x.ConvertToPdfAsync(
                It.IsAny<DocumentPdfConversionRequest>(),
                default))
            .Returns<DocumentPdfConversionRequest, System.Threading.CancellationToken>(
                async (request, cancellationToken) =>
                {
                    var output = new MemoryStream();
                    request.Stream.Position = 0;
                    await request.Stream.CopyToAsync(output, cancellationToken);
                    output.Position = 0;
                    return output;
                });

        var strategy = new TransitoryDocumentStrategy(
            transitoryDocumentsServiceMock.Object,
            conversionServiceMock.Object,
            loggerMock.Object);

        var result = await strategy.Invoke(new PdfDocumentRequestDetails
        {
            Path = "path/to/document.pdf",
        });

        Assert.Equal(contentBytes, result.ToArray());
        Assert.False(sourceStream.CanRead);
        conversionServiceMock.Verify(x => x.ConvertToPdfAsync(
            It.Is<DocumentPdfConversionRequest>(request =>
                request.FileName == "document.pdf" &&
                request.SourceIdentifier == "path/to/document.pdf"),
            default), Times.Once);
    }
}
