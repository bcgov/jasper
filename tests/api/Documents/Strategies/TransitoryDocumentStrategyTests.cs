using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
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
        var loggerMock = new Mock<ILogger<TransitoryDocumentStrategy>>();

        transitoryDocumentsServiceMock
            .Setup(x => x.DownloadFile("path/to/document.pdf", default))
            .ReturnsAsync(new FileStreamResponse(
                sourceStream,
                "document.pdf",
                "application/pdf"));

        var strategy = new TransitoryDocumentStrategy(
            transitoryDocumentsServiceMock.Object,
            loggerMock.Object);

        var result = await strategy.Invoke(new PdfDocumentRequestDetails
        {
            Path = "path/to/document.pdf",
        });

        Assert.Equal(contentBytes, result.ToArray());
    }
}
