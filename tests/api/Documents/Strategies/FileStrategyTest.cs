using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Scv.Api.Documents.Strategies;
using Scv.Api.Helpers;
using Scv.Api.Models.Document;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents.Strategies;

public class FileStrategyTest : ServiceTestBase
{
    private Mock<FileServicesClient> _mockFileServicesClient;
    private ClaimsPrincipal _mockUser;
    private readonly string _fakeContent = "Hello, world!";
    private readonly byte[] _fakeContentBytes;

    public FileStrategyTest()
    {
        _fakeContentBytes = Encoding.UTF8.GetBytes(_fakeContent);
        SetupFileServiceClient();
    }

    private void SetupFileServiceClient()
    {
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, 1.ToString()),
            new(CustomClaimTypes.ApplicationCode, "TESTAPP"),
            new(CustomClaimTypes.JcAgencyCode, "TESTAGENCY"),
            new(CustomClaimTypes.JcParticipantId, "TESTPART"),
        };

        var identity = new ClaimsIdentity(claims, "HELLO");
        _mockUser = new ClaimsPrincipal(identity);

        _mockFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        var fakeStream = new MemoryStream(_fakeContentBytes);
        var documentResponse = new DocumentResponse
        {
            Stream = fakeStream
        };

        _mockFileServicesClient.Setup(c => c.FilesDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()))
            .ReturnsAsync((string agencyId, string partId, string appCode, string documentId, string fileId, string someOtherId, bool flatten, string correlationId) =>
            new FileResponse(
                200,
                new Dictionary<string, IEnumerable<string>>(),
                fakeStream,
                null,
                null
            ));
    }

    [Fact]
    public async Task Invoke_ReturnsMemoryStreamWithDocumentContent()
    {
        var fakeDocumentId = "test-document-id";
        var encodedDocumentId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(fakeDocumentId));
        var fakeFileId = "file-id";
        var fakeCorrelationId = "correlation-id";
        var documentRequest = new PdfDocumentRequestDetails
        {
            DocumentId = encodedDocumentId,
            FileId = fakeFileId,
            CorrelationId = fakeCorrelationId
        };
        var strategy = new FileStrategy(_mockFileServicesClient.Object, _mockUser);
        var resultStream = await strategy.Invoke(documentRequest);

        Assert.NotNull(resultStream);
        resultStream.Position = 0;
        var resultBytes = resultStream.ToArray();
        Assert.Equal(_fakeContentBytes, resultBytes);
    }

    [Fact]
    public void Type_ReturnsFile()
    {
        var strategy = new FileStrategy(_mockFileServicesClient.Object, _mockUser);

        var type = strategy.Type;

        Assert.Equal(Scv.Api.Documents.DocumentType.File, type);
    }
}

// Minimal stub for DocumentResponse to make the test compile
public class DocumentResponse
{
    public Stream Stream { get; set; }
}