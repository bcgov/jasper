using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DARSCommon.Clients.TranscriptsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents.Strategies;
using Scv.Core.Helpers;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents.Strategies;

public class TranscriptStrategyTest : ServiceTestBase
{
    private Mock<TranscriptsServicesClient> _mockTranscriptsClient;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ILogger<TranscriptStrategy>> _mockLogger;
    private ClaimsPrincipal _mockUser;
    private readonly string _fakeContent = "PDF transcript content";
    private readonly byte[] _fakeContentBytes;

    public TranscriptStrategyTest()
    {
        _fakeContentBytes = Encoding.UTF8.GetBytes(_fakeContent);
        SetupTranscriptsClient();
    }

    private void SetupTranscriptsClient()
    {
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, 1.ToString()),
            new(CustomClaimTypes.JcAgencyCode, "TESTAGENCY"),
            new(CustomClaimTypes.JcParticipantId, "TESTPART"),
        };

        var identity = new ClaimsIdentity(claims, "HELLO");
        _mockUser = new ClaimsPrincipal(identity);

        _mockTranscriptsClient = new Mock<TranscriptsServicesClient>(MockBehavior.Strict, this.HttpClient);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TranscriptStrategy>>();

        _mockTranscriptsClient.Setup(c => c.GetAttachmentBaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((string orderId, string documentId, System.Threading.CancellationToken cancellationToken) =>
            {
                var stream = new MemoryStream(_fakeContentBytes);
                return new FileResponse(
                    200,
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Content-Type", new [] { "application/pdf" } },
                        { "Content-Length", new [] { _fakeContentBytes.Length.ToString() } },
                        { "Content-Disposition", new [] { "inline; filename=transcript.pdf" } }
                    },
                    stream,
                    null,
                    null
                );
            });
    }

    [Fact]
    public async Task Invoke_ReturnsMemoryStreamWithDocumentContent()
    {
        // Arrange
        var fakeOrderId = "123";
        var fakeDocumentId = "456";
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = fakeOrderId,
            DocumentId = fakeDocumentId
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var resultStream = await strategy.Invoke(documentRequest);

        // Assert
        Assert.NotNull(resultStream);
        resultStream.Position = 0;
        var resultBytes = resultStream.ToArray();
        Assert.Equal(_fakeContentBytes, resultBytes);

        _mockTranscriptsClient.Verify(c => c.GetAttachmentBaseAsync(
            fakeOrderId,
            fakeDocumentId,
            It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Type_ReturnsTranscript()
    {
        // Arrange
        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var type = strategy.Type;

        // Assert
        Assert.Equal(Scv.Models.DocumentType.Transcript, type);
    }

    [Fact]
    public async Task Invoke_WithNullOrderId_ThrowsException()
    {
        // Arrange
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = null,
            DocumentId = "456"
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await strategy.Invoke(documentRequest));
    }

    [Fact]
    public async Task Invoke_WithNullDocumentId_ThrowsException()
    {
        // Arrange
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = "123",
            DocumentId = null
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await strategy.Invoke(documentRequest));
    }

    [Fact]
    public async Task Invoke_CallsGetAttachmentWithCorrectParameters()
    {
        // Arrange
        var expectedOrderId = "789";
        var expectedDocumentId = "101112";
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = expectedOrderId,
            DocumentId = expectedDocumentId
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        await strategy.Invoke(documentRequest);

        // Assert
        _mockTranscriptsClient.Verify(c => c.GetAttachmentBaseAsync(
            expectedOrderId,
            expectedDocumentId,
            It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invoke_ResetsStreamPosition()
    {
        // Arrange
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = "123",
            DocumentId = "456"
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var resultStream = await strategy.Invoke(documentRequest);

        // Assert
        Assert.Equal(0, resultStream.Position);
    }

    [Fact]
    public async Task Invoke_LogsInformationMessages()
    {
        // Arrange
        var orderId = "123";
        var documentId = "456";
        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = orderId,
            DocumentId = documentId
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        await strategy.Invoke(documentRequest);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fetching transcript attachment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Transcript response - OrderId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_WithLargeDocument_ReturnsCompleteContent()
    {
        // Arrange
        var largeFakeContent = new byte[1024 * 1024]; // 1 MB
        new Random().NextBytes(largeFakeContent);

        _mockTranscriptsClient.Setup(c => c.GetAttachmentBaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((string orderId, string documentId, System.Threading.CancellationToken cancellationToken) =>
            {
                var stream = new MemoryStream(largeFakeContent);
                return new FileResponse(
                    200,
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Content-Type", new [] { "application/pdf" } },
                        { "Content-Length", new [] { largeFakeContent.Length.ToString() } }
                    },
                    stream,
                    null,
                    null
                );
            });

        var documentRequest = new PdfDocumentRequestDetails
        {
            OrderId = "123",
            DocumentId = "456"
        };

        var strategy = new TranscriptStrategy(
            _mockTranscriptsClient.Object,
            _mockUser,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var resultStream = await strategy.Invoke(documentRequest);

        // Assert
        Assert.NotNull(resultStream);
        Assert.Equal(largeFakeContent.Length, resultStream.Length);
        resultStream.Position = 0;
        var resultBytes = resultStream.ToArray();
        Assert.Equal(largeFakeContent, resultBytes);
    }
}
