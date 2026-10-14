using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Documents.Strategies;
using Scv.Core.Exceptions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Document;
using Scv.Models.Order;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents.Strategies;

public class OrderDocumentStrategyTest : ServiceTestBase
{
    private readonly Mock<IJudicialServicesClient> _mockJudicialClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRepositoryBase<Order>> _mockOrderRepo;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IPdfPageEditor> _mockPdfPageEditor;
    private readonly string _fakeContent = "fake-pdf-bytes";
    private readonly byte[] _fakeContentBytes;

    public OrderDocumentStrategyTest()
    {
        _fakeContentBytes = Encoding.UTF8.GetBytes(_fakeContent);
        _mockJudicialClient = new Mock<IJudicialServicesClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOrderRepo = new Mock<IRepositoryBase<Order>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockPdfPageEditor = new Mock<IPdfPageEditor>();

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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await strategy.Invoke(request);

        Assert.Equal("existing-correlation-id", request.CorrelationId);
    }

    [Fact]
    public async Task Invoke_ThrowsArgumentNullException_WhenDocumentIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = null };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await Assert.ThrowsAsync<System.ArgumentNullException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsArgumentNullException_WhenOrderIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = "123", OrderId = null };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await Assert.ThrowsAsync<System.ArgumentNullException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidDataException_WhenDocumentIdIsNotValidBase64Number()
    {
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("not-a-number"));
        var request = new PdfDocumentRequestDetails { DocumentId = encoded, OrderId = "12345" };
        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await Assert.ThrowsAsync<InvalidDataException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidDataException_WhenAgencyIdIsInvalid()
    {
        var badAgencySection = Mock.Of<IConfigurationSection>(s => s.Value == "not-a-number");
        _mockConfiguration
            .Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(badAgencySection);

        var strategy = new OrderDocumentStrategy(
            _mockJudicialClient.Object,
            _mockConfiguration.Object,
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await Assert.ThrowsAsync<InvalidDataException>(() => strategy.Invoke(BuildRequest()));
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
            _mockOrderRepo.Object,
            _mockHttpClientFactory.Object,
            _mockPdfPageEditor.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => strategy.Invoke(BuildRequest("789")));
    }

    [Fact]
    public async Task Invoke_ThrowsNotFoundException_WhenOrderDoesNotExist()
    {
        _mockOrderRepo
            .Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Order)null);
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<NotFoundException>(() => strategy.Invoke(BuildRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE)]
    public async Task Invoke_DoesNotAppendAdditionalPage_WhenOrderIsNotSmallClaimsDeskOrder(string courtListTypeCd)
    {
        SetupJudicialClientResponse();
        SetupOrderRepo(123, courtListTypeCd: courtListTypeCd);
        var strategy = CreateStrategy();

        var result = await strategy.Invoke(BuildRequest("123"));

        result.Position = 0;
        Assert.Equal(_fakeContentBytes, result.ToArray());
        _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_ThrowsConfigurationException_WhenSmallClaimsUrlIsMissing()
    {
        SetupJudicialClientResponse();
        SetupSmallClaimsOrderRepo(123);
        SetupSmallClaimsUrl(null);
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<ConfigurationException>(() => strategy.Invoke(BuildRequest("123")));

        _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_ThrowsHttpRequestException_WhenSmallClaimsPageDownloadFails()
    {
        SetupJudicialClientResponse();
        SetupSmallClaimsOrderRepo(123);
        SetupSmallClaimsUrl("https://example.com/scl029.pdf");
        SetupHttpClient(HttpStatusCode.NotFound, []);
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<HttpRequestException>(() => strategy.Invoke(BuildRequest("123")));
    }

    [Fact]
    public async Task Invoke_RequestsConfiguredSmallClaimsUrl_ForSmallClaimsDeskOrder()
    {
        const string url = "https://example.com/scl029.pdf";
        SetupJudicialClientResponse();
        SetupSmallClaimsOrderRepo(123);
        SetupSmallClaimsUrl(url);
        var handler = SetupHttpClient(HttpStatusCode.NotFound, []);
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<HttpRequestException>(() => strategy.Invoke(BuildRequest("123")));

        Assert.Equal(url, handler.RequestUri?.ToString());
    }

    private OrderDocumentStrategy CreateStrategy() => new(
        _mockJudicialClient.Object,
        _mockConfiguration.Object,
        _mockOrderRepo.Object,
        _mockHttpClientFactory.Object,
        _mockPdfPageEditor.Object);

    private void SetupSmallClaimsUrl(string url)
    {
        _mockConfiguration
            .Setup(c => c.GetSection("ORDER_SCL029_URL"))
            .Returns(Mock.Of<IConfigurationSection>(s => s.Value == url));
    }

    private StubHttpMessageHandler SetupHttpClient(HttpStatusCode statusCode, byte[] content)
    {
        var handler = new StubHttpMessageHandler(statusCode, content);
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));
        return handler;
    }

    private void SetupSmallClaimsOrderRepo(int packageDocumentId) =>
        SetupOrderRepo(
            packageDocumentId,
            courtListTypeCd: CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE);

    private void SetupOrderRepo(int packageDocumentId, int relevantCeisDocumentId = 456, string courtListTypeCd = null)
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
                    ],
                    Referral = courtListTypeCd is null ? null : new Referral { CourtListTypeCd = courtListTypeCd }
                }
            });
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, byte[] content) : HttpMessageHandler
    {
        public System.Uri RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new ByteArrayContent(content) });
        }
    }
}
