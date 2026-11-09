using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using LazyCache;
using LazyCache.Providers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Documents.Strategies;
using Scv.Api.Services;
using Scv.Core.Exceptions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AppDocument;
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
    private readonly Mock<IPdfPageEditor> _mockPdfPageEditor;
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<IAppDocumentService> _mockAppDocumentService;
    private readonly Mock<ILogger<OrderDocumentStrategy>> _mockLogger;
    private readonly string _fakeContent = "fake-pdf-bytes";
    private readonly byte[] _fakeContentBytes;
    private readonly IAppCache _cache;

    public OrderDocumentStrategyTest()
    {
        _fakeContentBytes = Encoding.UTF8.GetBytes(_fakeContent);
        _mockJudicialClient = new Mock<IJudicialServicesClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOrderRepo = new Mock<IRepositoryBase<Order>>();
        _mockPdfPageEditor = new Mock<IPdfPageEditor>();
        _mockLocationService = new Mock<ILocationService>();
        _mockAppDocumentService = new Mock<IAppDocumentService>();
        _mockLogger = new Mock<ILogger<OrderDocumentStrategy>>();

        var mockAgencySection = Mock.Of<IConfigurationSection>(s => s.Value == "123");
        var mockAppCdSection = Mock.Of<IConfigurationSection>(s => s.Value == "TESTAPP");
        var mockPartIdSection = Mock.Of<IConfigurationSection>(s => s.Value == "456");

        _mockConfiguration
            .Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(mockAgencySection);
        _mockConfiguration
            .Setup(c => c.GetSection("Request:ApplicationCd"))
            .Returns(mockAppCdSection);
        _mockConfiguration
            .Setup(c => c.GetSection("Request:PartId"))
            .Returns(mockPartIdSection);

        _mockJudicialClient.Setup(j => j.JsonSerializerSettings).Returns(new Newtonsoft.Json.JsonSerializerSettings());

        _cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
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
        var strategy = CreateStrategy();

        Assert.Equal(DocumentType.Order, strategy.Type);
    }

    [Fact]
    public async Task Invoke_ReturnsMemoryStreamWithDocumentContent()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        SetupOrderRepo(documentId);
        var strategy = CreateStrategy();

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
        var strategy = CreateStrategy();

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
        var strategy = CreateStrategy();

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
        var strategy = CreateStrategy();

        await strategy.Invoke(request);

        Assert.Equal("existing-correlation-id", request.CorrelationId);
    }

    [Fact]
    public async Task Invoke_ThrowsArgumentNullException_WhenDocumentIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = null };
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<System.ArgumentNullException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsArgumentNullException_WhenOrderIdIsNull()
    {
        var request = new PdfDocumentRequestDetails { DocumentId = "123", OrderId = null };
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<System.ArgumentNullException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidDataException_WhenDocumentIdIsNotValidBase64Number()
    {
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("not-a-number"));
        var request = new PdfDocumentRequestDetails { DocumentId = encoded, OrderId = "12345" };
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<InvalidDataException>(() => strategy.Invoke(request));
    }

    [Fact]
    public async Task Invoke_ThrowsInvalidDataException_WhenAgencyIdIsInvalid()
    {
        var badAgencySection = Mock.Of<IConfigurationSection>(s => s.Value == "not-a-number");
        _mockConfiguration
            .Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(badAgencySection);
        var strategy = CreateStrategy();

        await Assert.ThrowsAsync<InvalidDataException>(() => strategy.Invoke(BuildRequest()));
    }

    [Fact]
    public async Task Invoke_ThrowsDocumentNotFound_WhenDocumentIdDoesNotExistInBothPackageDocumentsAndRelevantCeisDocuments()
    {
        SetupJudicialClientResponse();
        var documentId = 123;
        var ciesDocumentId = 456;
        SetupOrderRepo(documentId, ciesDocumentId);
        var strategy = CreateStrategy();

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
        _mockAppDocumentService.Verify(s => s.GetAppDocumentByKeyAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_DoesNotAppendAdditionalPage_WhenDocumentIsNotReferredDocument()
    {
        SetupJudicialClientResponse();
        SetupOrderRepo(123, courtListTypeCd: CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE, isPackageReferred: false);
        var strategy = CreateStrategy();

        var result = await strategy.Invoke(BuildRequest("123"));

        result.Position = 0;
        Assert.Equal(_fakeContentBytes, result.ToArray());
        _mockAppDocumentService.Verify(s => s.GetAppDocumentByKeyAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invoke_ThrowsInvalidOperationException_AndLogsWarning_WhenSmallClaimsTemplateIsMissing(bool templateExistsWithoutData)
    {
        SetupJudicialClientResponse();
        SetupSmallClaimsOrderRepo(123);
        _mockAppDocumentService
            .Setup(s => s.GetAppDocumentByKeyAsync(AppDocument.SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE))
            .ReturnsAsync(templateExistsWithoutData ? new AppDocumentDto { Data = null } : null);
        var strategy = CreateStrategy();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.Invoke(BuildRequest("123")));

        Assert.Equal("Small Claims Additional Page template is missing.", exception.Message);
        _mockPdfPageEditor.Verify(p => p.FillFormFields(It.IsAny<MemoryStream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<bool>()), Times.Never);
        _mockPdfPageEditor.Verify(p => p.AppendPages(It.IsAny<MemoryStream>(), It.IsAny<MemoryStream>()), Times.Never);
        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task Invoke_FillsTemplateFromAppDocument_AndAppendsEachCall_ForSmallClaimsDeskOrder()
    {
        SetupSmallClaimsOrderRepo(123);
        var templateBytes = new byte[] { 1, 2, 3 };
        SetupSmallClaimsTemplate(templateBytes);
        var filledBytes = new byte[] { 4, 5, 6 };
        var capturedTemplates = new List<byte[]>();
        var appendedPages = new List<byte[]>();
        _mockPdfPageEditor
            .Setup(p => p.FillFormFields(It.IsAny<MemoryStream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), true))
            .Callback<MemoryStream, IReadOnlyDictionary<string, string>, bool>((template, _, _) => capturedTemplates.Add(template.ToArray()))
            .Returns(() => new MemoryStream(filledBytes));
        _mockPdfPageEditor
            .Setup(p => p.AppendPages(It.IsAny<MemoryStream>(), It.IsAny<MemoryStream>()))
            .Callback<MemoryStream, MemoryStream>((_, pages) => appendedPages.Add(pages.ToArray()))
            .Returns(() => new MemoryStream());
        var strategy = CreateStrategy();

        SetupJudicialClientResponse();
        await strategy.Invoke(BuildRequest("123"));
        SetupJudicialClientResponse();
        await strategy.Invoke(BuildRequest("123"));

        Assert.Equal(2, capturedTemplates.Count);
        Assert.All(capturedTemplates, bytes => Assert.Equal(templateBytes, bytes));
        Assert.Equal(2, appendedPages.Count);
        Assert.All(appendedPages, bytes => Assert.Equal(filledBytes, bytes));
    }

    [Fact]
    public async Task Invoke_FillsAndFlattensAdditionalPage_BeforeAppending_ForSmallClaimsDeskOrder()
    {
        SetupJudicialClientResponse();
        SetupSmallClaimsTemplate([1, 2, 3]);
        _mockOrderRepo
            .Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Order
            {
                OrderRequest = new OrderRequest
                {
                    FullFileNo = "SC-123",
                    CourtLocationId = 10,
                    PackageDocuments = [new() { DocumentId = 123, ReferredDocument = true }],
                    Referral = new Referral
                    {
                        CourtListTypeCd = CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE
                    }
                }
            });

        var filled = new MemoryStream([4, 5, 6]);
        var merged = new MemoryStream([7, 8, 9]);
        IReadOnlyDictionary<string, string> capturedValues = null;
        _mockPdfPageEditor
            .Setup(p => p.FillFormFields(It.IsAny<MemoryStream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<bool>()))
            .Callback<MemoryStream, IReadOnlyDictionary<string, string>, bool>((_, values, _) => capturedValues = values)
            .Returns(filled);
        _mockPdfPageEditor
            .Setup(p => p.AppendPages(It.IsAny<MemoryStream>(), It.IsAny<MemoryStream>()))
            .Returns(merged);
        _mockLocationService.Setup(l => l.GetLocationShortNameByCode("10")).ReturnsAsync("Vancouver");
        var strategy = CreateStrategy();

        var result = await strategy.Invoke(BuildRequest("123"));

        Assert.Same(merged, result);
        Assert.NotNull(capturedValues);
        Assert.Equal("SC-123", capturedValues[OrderDocumentStrategy.COURT_FILE_NUMBER_FIELD]);
        Assert.Equal("Vancouver", capturedValues[OrderDocumentStrategy.COURT_LOCATION_FIELD]);
        _mockPdfPageEditor.Verify(p => p.FillFormFields(It.IsAny<MemoryStream>(), It.IsAny<IReadOnlyDictionary<string, string>>(), true), Times.Once);
        _mockPdfPageEditor.Verify(p => p.AppendPages(It.IsAny<MemoryStream>(), filled), Times.Once);
    }

    private OrderDocumentStrategy CreateStrategy() => new(
        _mockJudicialClient.Object,
        _mockConfiguration.Object,
        _mockOrderRepo.Object,
        _mockPdfPageEditor.Object,
        _mockLocationService.Object,
        _mockAppDocumentService.Object,
        _mockLogger.Object);

    private void SetupSmallClaimsTemplate(byte[] data)
    {
        _mockAppDocumentService
            .Setup(s => s.GetAppDocumentByKeyAsync(AppDocument.SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE))
            .ReturnsAsync(new AppDocumentDto { Key = AppDocument.SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE, Data = data });
    }

    private void SetupSmallClaimsOrderRepo(int packageDocumentId) =>
        SetupOrderRepo(
            packageDocumentId,
            courtListTypeCd: CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE);

    private void SetupOrderRepo(int packageDocumentId, int relevantCeisDocumentId = 456, string courtListTypeCd = null, bool isPackageReferred = true)
    {
        _mockOrderRepo
            .Setup(o => o.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new Order
            {
                OrderRequest = new OrderRequest
                {
                    PackageDocuments =
                    [
                        new()
                        {
                            DocumentId = packageDocumentId,
                            ReferredDocument = isPackageReferred
                        }
                    ],
                    RelevantCeisDocuments =
                    [
                        new() { CivilDocumentId = relevantCeisDocumentId }
                    ],
                    Referral = courtListTypeCd is null ? null : new Referral { CourtListTypeCd = courtListTypeCd }
                }
            });
    }
}
