using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GdPicture14;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Models.Document;
using Scv.Api.Services.Files;
using Xunit;
using JCCommon.Clients.FileServices;
using tests.api.Services;
using System.Linq;
using System.Security.Claims;
using FluentValidation;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Exceptions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.archive;
using Scv.Api.Models.Search;
using Scv.Api.Services;
using Scv.Db.Models;
using tests.api.Helpers;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;
using PCSSCommon.Clients.FileDetailServices;
using Microsoft.Extensions.Configuration;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;

namespace tests.api.Documents;

public class DocumentMergerTest : ServiceTestBase
{
    private FilesService _service;
    private Mock<IConfiguration> _mockConfig;
    private CachingService _cachingService;
    private IMapper _mapper;
    public DocumentMergerTest()
    {
        _mockConfig = new Mock<IConfiguration>();
        var mockLocationSection = new Mock<IConfigurationSection>();
        mockLocationSection.Setup(s => s.Value).Returns(1.ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockLocationSection.Object);
        var mockLookupSection = new Mock<IConfigurationSection>();
        mockLookupSection.Setup(s => s.Value).Returns(1.ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LookupExpiryMinutes")).Returns(mockLookupSection.Object);
        var mockFileExpirySection = new Mock<IConfigurationSection>();
        mockFileExpirySection.Setup(s => s.Value).Returns("60");
        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockFileExpirySection.Object);
        _mockConfig.Setup(c => c.GetSection("ExcludeDocumentTypeCodesForCounsel")).Returns(mockFileExpirySection.Object);

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

        mockFilesServiceClient.Setup(c => c.FilesDocumentAsync(
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
                new MemoryStream(),
                null,
                null
            ));

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
            mockFileDetailClient.Object,
            _cachingService,
            mockUser,
            mockLoggerFactory.Object);


        return _service;
    }

    [Fact]
    public async Task MergeDocuments_ThrowsInvalidOperationException_WhenStreamUnreadable()
    {
        var merger = new DocumentMerger(_service);
        var docId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("doc1"));
        var request = new PdfDocumentRequest
        {
            Data = new PdfDocumentRequestDetails
            {
                DocumentId = docId,
                FileId = "file1",
                CorrelationId = "corr1"
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await merger.MergeDocuments([request]);
        });
    }
}