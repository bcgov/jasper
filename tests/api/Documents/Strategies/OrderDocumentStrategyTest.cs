using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using Nutrient.NativeSDK.API.Exceptions;
using Scv.Api.Documents.Strategies;
using Scv.Core.Exceptions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents.Strategies;

public class OrderDocumentStrategyTest : ServiceTestBase
{
    private readonly Mock<IJudicialServicesClient> _mockJudicialClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRepositoryBase<Order>> _mockOrderRepo;
    private readonly string _fakeContent = "fake-pdf-bytes";
    private readonly byte[] _fakeContentBytes;

    public OrderDocumentStrategyTest()
    {
        _fakeContentBytes = Encoding.UTF8.GetBytes(_fakeContent);
        _mockJudicialClient = new Mock<IJudicialServicesClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOrderRepo = new Mock<IRepositoryBase<Order>>();

        var mockAgencySection = Mock.Of<IConfigurationSection>(s => s.Value == "123");
        var mockAppCdSection = Mock.Of<IConfigurationSection>(s => s.Value == "TESTAPP");

        _mockConfiguration
            .Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(mockAgencySection);
        _mockConfiguration
            .Setup(c => c.GetSection("Request:ApplicationCd"))
            .Returns(mockAppCdSection);

        _mockJudicialClient.Setup(j => j.JsonSerializerSettings).Returns(new Newtonsoft.Json.JsonSerializerSettings());
    }

    private static PdfDocumentRequestDetails BuildRequest(string rawDocumentId = "12345", string correlationId = null)
    {
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawDocumentId));
        return new PdfDocumentRequestDetails
        {
            DocumentId = encoded,
            CorrelationId = correlationId,
            OrderId = "12345"
        };
    }

    private void SetupJudicialClientResponse()
    {
        var fakeStream = new MemoryStream(_fakeContentBytes);
        var fakeResponse = new FileResponse(
            200,
            new Dictionary<string, IEnumerable<string>>(),
            fakeStream,
            null,
            null);

        _mockJudicialClient
            .Setup(c => c.GetJudicialDocumentAsync(
                It.IsAny<System.Guid>(),
                It.IsAny<double?>(),
                It.IsAny<double>(),
                It.IsAny<DocumentApplicationName>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fakeResponse);
    }

    [Fact]
    public void Type_ReturnsOrder()
    {
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        Assert.Equal(DocumentType.Order, strategy.Type);
    }

    [Fact]
    public async Task Invoke_ReturnsMemoryStreamWithDocumentContent()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        SetupOrderRepo(documentId);
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        var result = await strategy.Invoke(BuildRequest(documentId.ToString()));

        Assert.NotNull(result);
        result.Position = 0;
        Assert.Equal(_fakeContentBytes, result.ToArray());
    }

    [Fact]
    public async Task Invoke_ReturnsMemoryStream_WithCeisDocumentId()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        var ciesDocumentId = 456;
        SetupOrderRepo(documentId, ciesDocumentId);
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        var result = await strategy.Invoke(BuildRequest(ciesDocumentId.ToString()));

        Assert.NotNull(result);
        result.Position = 0;
        Assert.Equal(_fakeContentBytes, result.ToArray());
    }


    [Fact]
    public async Task Invoke_AssignsCorrelationId_WhenNotProvided()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        var request = BuildRequest(documentId.ToString(), correlationId: null);
        SetupOrderRepo(documentId);
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await strategy.Invoke(request);

        Assert.NotNull(request.CorrelationId);
        Assert.True(System.Guid.TryParse(request.CorrelationId, out _));
    }

    [Fact]
    public async Task Invoke_PreservesCorrelationId_WhenAlreadyProvided()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        SetupOrderRepo(documentId);
        var request = BuildRequest(documentId.ToString(), correlationId: "existing-correlation-id");
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await strategy.Invoke(request);

        Assert.Equal("existing-correlation-id", request.CorrelationId);
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidArgumentException_WhenDocumentIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = null };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await Assert.ThrowsAsync<InvalidArgumentException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidArgumentException_WhenOrderIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = "123", OrderId = null };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await Assert.ThrowsAsync<InvalidArgumentException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidArgumentException_WhenDocumentIdIsNotValidBase64Number()
    {
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("not-a-number"));
        var request = new PdfDocumentRequestDetails { DocumentId = encoded };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await Assert.ThrowsAsync<InvalidArgumentException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidArgumentException_WhenAgencyIdIsInvalid()
    {
        var badAgencySection = Mock.Of<IConfigurationSection>(s => s.Value == "not-a-number");
        _mockConfiguration
            .Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(badAgencySection);

        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await Assert.ThrowsAsync<InvalidArgumentException>(() => strategy.Invoke(BuildRequest()));
    }

    [Fact]
    public async Task Invoke_ThrowsDocumentNotFound_WhenDocumentIdDoesNotExistInBothPackageDocumentsAndRelevantCeisDocuments()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        var ciesDocumentId = 456;
        SetupOrderRepo(documentId, ciesDocumentId);
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => strategy.Invoke(BuildRequest("789")));
    }

    private void SetupOrderRepo(int packageDocumentId, int relevantCeisDocumentId = 456)
    {
        _mockOrderRepo
            .Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Order
            {
                OrderRequest = new OrderRequest
                {
                    PackageDocuments =
                    [
                        new() { DocumentId = packageDocumentId }
                    ],
                    RelevantCeisDocuments =
                    [
                        new() { CivilDocumentId = relevantCeisDocumentId }
                    ]
                }
            });
    }
}
