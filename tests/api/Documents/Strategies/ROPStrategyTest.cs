using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Documents.Strategies;
using Scv.Api.Helpers;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;
using Scv.Models;
using Scv.Core.Helpers;

namespace tests.api.Documents.Strategies;

public class ROPStrategyTest : ServiceTestBase
{
    private Mock<FileServicesClient> _mockFileServicesClient;
    private Mock<IConfiguration> _mockConfiguration;
    private ClaimsPrincipal _mockUser;
    private readonly string fakeContent = "Hello, world!";
    private readonly byte[] fakeContentBytes;

    public ROPStrategyTest()
    {
        fakeContentBytes = Encoding.UTF8.GetBytes(fakeContent);
        SetupFileServiceClient();
    }

    private void SetupFileServiceClient()
    {
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, 1.ToString()),
            new(CustomClaimTypes.JcAgencyCode, "TESTAGENCY"),
            new(CustomClaimTypes.JcParticipantId, "TESTPART"),
        };

        var identity = new ClaimsIdentity(claims, "HELLO");
        _mockUser = new ClaimsPrincipal(identity);
        _mockFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        _mockConfiguration = new Mock<IConfiguration>();
        var mockSection = Mock.Of<IConfigurationSection>(s => s.Value == "TESTAPP");
        _mockConfiguration.Setup(c => c.GetSection("Request:ApplicationCd")).Returns(mockSection);
        var fakeStream = new MemoryStream(fakeContentBytes);
        var documentResponse = new DocumentResponse
        {
            Stream = fakeStream
        };

        _mockFileServicesClient.Setup(c => c.FilesRecordOfProceedingsAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CourtLevelCd>(),
            It.IsAny<CourtClassCd>()
            )).Returns((string agencyId, string partId, string appCode, string documentId, string fileId, CourtLevelCd someOtherId, CourtClassCd flatten) =>
            Task.FromResult(new RopResponse
            {
                B64Content = Convert.ToBase64String(fakeContentBytes)
            }));
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
            CorrelationId = fakeCorrelationId,
            CourtLevelCd = "P",
            CourtClassCd = "Y"
        };
        var strategy = new ROPStrategy(_mockFileServicesClient.Object, _mockUser, _mockConfiguration.Object);
        var resultStream = await strategy.Invoke(documentRequest);

        Assert.NotNull(resultStream);
        resultStream.Position = 0;
        var resultBytes = resultStream.ToArray();
        Assert.Equal(fakeContentBytes, resultBytes);
    }

    [Fact]
    public void Type_ReturnsROP()
    {
        var strategy = new ROPStrategy(_mockFileServicesClient.Object, _mockUser, _mockConfiguration.Object);

        var type = strategy.Type;

        Assert.Equal(DocumentType.ROP, type);
    }
}