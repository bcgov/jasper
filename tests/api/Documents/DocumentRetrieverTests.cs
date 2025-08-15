using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Documents.Strategies;
using Scv.Api.Models.Document;
using Xunit;

namespace tests.api.Documents;

public class DocumentRetrieverTest
{
    private readonly Mock<ILogger<DocumentRetriever>> _logger;

    public DocumentRetrieverTest()
    {
        _logger = new Mock<ILogger<DocumentRetriever>>();
    }

    [Fact]
    public async Task Retrieve_ValidType_StrategyInvoked_ReturnsMemoryStream()
    {
        var expectedStream = new MemoryStream();
        var requestData = new PdfDocumentRequestDetails
        {
        };
        var request = new PdfDocumentRequest
        {
            Type = DocumentType.File,
            Data = requestData
        };

        var strategyMock = new Mock<IDocumentStrategy>();
        strategyMock.SetupGet(s => s.Type).Returns(DocumentType.File);
        strategyMock.Setup(s => s.Invoke(requestData)).ReturnsAsync(expectedStream);

        var retriever = new DocumentRetriever([strategyMock.Object], _logger.Object);

        var result = await retriever.Retrieve(request);

        Assert.Same(expectedStream, result);
        strategyMock.Verify(s => s.Invoke(requestData), Times.Once);
    }

    [Fact]
    public async Task Retrieve_NoMatchingStrategy_ThrowsInvalidOperationException()
    {
        var request = new PdfDocumentRequest
        {
            Type = DocumentType.File,
            Data = new PdfDocumentRequestDetails { }
        };

        var strategyMock = new Mock<IDocumentStrategy>();
        strategyMock.SetupGet(s => s.Type).Returns((DocumentType)99); // Not matching

        var retriever = new DocumentRetriever([strategyMock.Object], _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => retriever.Retrieve(request));
    }
}