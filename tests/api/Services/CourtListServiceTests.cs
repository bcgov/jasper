using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.ReportServices;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.CourtList;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;
public class CourtListServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly IMapper _mapper;

    public CourtListServiceTests()
    {
        _faker = new Faker();

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();

        var mockFileExpirySection = new Mock<IConfigurationSection>();
        mockFileExpirySection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());

        var mockRefreshHoursSection = new Mock<IConfigurationSection>();
        mockRefreshHoursSection.Setup(s => s.Value).Returns("12");

        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockFileExpirySection.Object);

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        _mapper = new Mapper(config);
    }

    private (
        CourtListService courtListService,
        Mock<ReportServicesClient> mockReportClient,
        Mock<FileServicesClient> mockFileClient
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
            _mapper,
            null,
            null,
            mockSearchDateClient.Object,
            mockReportClient.Object,
            cachingService,
            new ClaimsPrincipal(identity));

        return (
            courtListService,
            mockReportClient,
            mockFileClient
        );
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnStream()
    {
        var (courtListService, mockReportClient, _) = SetupCourtListService();
        var fakeContentDisposition = $"inline; filename={Path.GetRandomFileName()}";

        mockReportClient
            .Setup(r => r.GetCourtListReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), fakeContentDisposition));

        var (stream, contentDisposition) = await courtListService.GenerateReportAsync(new CourtListReportRequest());

        Assert.NotNull(stream);
        Assert.Equal(fakeContentDisposition, contentDisposition);
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