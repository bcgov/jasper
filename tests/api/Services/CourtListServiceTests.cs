using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.ReportServices;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Models.CourtList;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;
public class CourtListServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly Mock<IMapper> _mapper;

    public CourtListServiceTests()
    {
        _faker = new Faker();

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockSection.Object);

        // IMapper setup
        _mapper = new Mock<IMapper>();
    }

    private (
        CourtListService courtListService,
        Mock<ReportServicesClient> mockReportClient
        ) SetupCourtListService()
    {
        // Setup Service Clients
        var mockFileClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, this.HttpClient);
        var mockReportClient = new Mock<ReportServicesClient>(MockBehavior.Strict, this.HttpClient);

        // Setup Services
        var mockLocationService = new Mock<LocationService>(MockBehavior.Strict, this.HttpClient);
        var mockLookupService = new Mock<LookupService>(MockBehavior.Strict, this.HttpClient);

        // Setup cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Setup ClaimsPrincipal
        var identity = new ClaimsIdentity([], "mock");

        var courtListService = new CourtListService(
            _mockConfig.Object,
            new Mock<ILogger<CourtListService>>().Object,
            mockFileClient.Object,
            _mapper.Object,
            null,
            null,
            mockSearchDateClient.Object,
            mockReportClient.Object,
            cachingService,
            new ClaimsPrincipal(identity));

        return (
            courtListService,
            mockReportClient
        );
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnStream()
    {
        var (courtListService, mockReportClient) = SetupCourtListService();
        mockReportClient
            .Setup(r => r.GetCourtListReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new MemoryStream());

        var result = await courtListService.GenerateReportAsync(new CourtListReportRequest());

        Assert.NotNull(result);
        mockReportClient
            .Verify(r => r
                .GetCourtListReportAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());
    }
}
