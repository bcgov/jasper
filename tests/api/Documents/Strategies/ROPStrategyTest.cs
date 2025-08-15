using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.FileDetailServices;
using Scv.Api.Documents.Strategies;
using Scv.Api.Helpers;
using Scv.Api.Models.Document;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using tests.api.Helpers;
using tests.api.Services;
using Xunit;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Documents.Strategies;

public class ROPStrategyTest : ServiceTestBase
{
    private FilesService _service;
    private Mock<IConfiguration> _mockConfig;
    private CachingService _cachingService;
    private IMapper _mapper;
    private readonly string fakeContent = "Hello, world!";
    private readonly byte[] fakeContentBytes;

    public ROPStrategyTest()
    {
        fakeContentBytes = Encoding.UTF8.GetBytes(fakeContent);
        _mockConfig = new Mock<IConfiguration>();
        var mockLocationSection = new Mock<IConfigurationSection>();
        var mockLookupSection = new Mock<IConfigurationSection>();
        var mockFileExpirySection = new Mock<IConfigurationSection>();
        mockLocationSection.Setup(s => s.Value).Returns(1.ToString());
        mockLookupSection.Setup(s => s.Value).Returns(1.ToString());
        mockFileExpirySection.Setup(s => s.Value).Returns("60");
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockLocationSection.Object);
        _mockConfig.Setup(c => c.GetSection("Caching:LookupExpiryMinutes")).Returns(mockLookupSection.Object);
        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockFileExpirySection.Object);
        _mockConfig.Setup(c => c.GetSection("ExcludeDocumentTypeCodesForCounsel")).Returns(mockFileExpirySection.Object);
        _mockConfig.Setup(c => c.GetSection("LookupServicesClient:Username")).Returns(mockFileExpirySection.Object);

        SetupFilesService();
    }


    private Mock<LocationService> SetupLocationService()
    {
        var mockJCLocationClient = new Mock<LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockPCSSLocationClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockPCSSLookupClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, this.HttpClient);

        mockJCLocationClient
            .Setup(c => c.LocationsGetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => []);
        mockJCLocationClient
            .Setup(c => c.LocationsRoomsGetAsync())
            .ReturnsAsync([]);

        mockPCSSLocationClient
            .Setup(c => c.GetLocationsAsync())
            .ReturnsAsync(() => []);

        mockPCSSLookupClient
            .Setup(c => c.GetCourtRoomsAsync())
            .ReturnsAsync(() => []);

        var mockLocationService = new Mock<LocationService>(
            MockBehavior.Strict,
            _mockConfig.Object,
            mockJCLocationClient.Object,
            mockPCSSLocationClient.Object,
            mockPCSSLookupClient.Object,
            _cachingService,
            _mapper);

        mockLocationService
            .Setup(l => l.GetLocationShortName(It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        return mockLocationService;
    }

    private FilesService SetupFilesService()
    {
        // IMapper setup
        var config = new TypeAdapterConfig();
        _mapper = new Mapper(config);

        _cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
        new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var contextAccessor = new TestHttpContextAccessor();

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, 1.ToString()),
        };

        var identity = new ClaimsIdentity(claims, "HELLO");
        var mockUser = new ClaimsPrincipal(identity);
        var mockFilesServiceClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockLocationService = new Mock<LocationService>(MockBehavior.Strict, this.HttpClient);
        var mockLookupCodeServicesClient = new Mock<LookupCodeServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockFileDetailClient = new Mock<FileDetailClient>(MockBehavior.Strict, this.HttpClient);
        var mockDocCatService = new Mock<IDocumentCategoryService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var fakeStream = new MemoryStream(fakeContentBytes);
        var documentResponse = new DocumentResponse
        {
            Stream = fakeStream
        };

        mockFilesServiceClient.Setup(c => c.FilesRecordOfProceedingsAsync(
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

        var mockLookupService = new Mock<LookupService>(
            MockBehavior.Strict,
            _mockConfig.Object,
            mockLookupCodeServicesClient.Object,
            _cachingService,
            mockDocCatService.Object);
        var locationService = SetupLocationService();

        _service = new FilesService(
            _mockConfig.Object,
            mockFilesServiceClient.Object,
            _mapper,
            mockLookupService.Object,
            locationService.Object,
            _cachingService,
            mockUser,
            mockLoggerFactory.Object);

        return _service;
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
        var strategy = new ROPStrategy(_service);
        var resultStream = await strategy.Invoke(documentRequest);

        Assert.NotNull(resultStream);
        resultStream.Position = 0;
        var resultBytes = resultStream.ToArray();
        Assert.Equal(fakeContentBytes, resultBytes);
    }

    [Fact]
    public void Type_ReturnsROP()
    {
        var strategy = new ROPStrategy(_service);

        var type = strategy.Type;

        Assert.Equal(Scv.Api.Documents.DocumentType.ROP, type);
    }
}