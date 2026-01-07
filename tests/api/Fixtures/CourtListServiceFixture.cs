using System;
using System.Net.Http;
using System.Security.Claims;
using Bogus;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Core.Helpers;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;
using PCSSReportServices = PCSSCommon.Clients.ReportServices;

namespace tests.api.Fixtures;

/// <summary>
/// Reusable fixture for creating Mock<CourtListService> with all dependencies configured.
/// Use this across multiple test classes to avoid repetitive setup code.
/// </summary>
public class CourtListServiceFixture : IDisposable
{
    private readonly Faker _faker;
    private readonly HttpClient _httpClient;

    public Mock<CourtListService> MockCourtListService { get; private set; }
    public Mock<IConfiguration> MockConfiguration { get; }
    public Mock<ILogger<CourtListService>> MockLogger { get; }
    public Mock<FileServicesClient> MockFileServicesClient { get; }
    public Mock<SearchDateClient> MockSearchDateClient { get; }
    public Mock<PCSSReportServices.ReportServicesClient> MockReportServicesClient { get; }
    public Mock<LookupService> MockLookupService { get; }
    public Mock<LocationService> MockLocationService { get; }
    public IAppCache CachingService { get; }
    public IMapper Mapper { get; }
    public ClaimsPrincipal Principal { get; private set; }

    public CourtListServiceFixture()
    {
        _faker = new Faker();

        // HttpClient setup
        var mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_faker.Internet.Url())
        };

        // Configuration setup
        MockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        MockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);

        // Logger setup
        MockLogger = new Mock<ILogger<CourtListService>>();

        // Client mocks
        MockFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, _httpClient);
        MockSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, _httpClient);
        var mockJCLocationServicesClient = new Mock<LocationServicesClient>(MockBehavior.Strict, _httpClient);
        var mockPCSSLocationServicesClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, _httpClient);
        var mockLookupCodeServicesClient = new Mock<LookupCodeServicesClient>(MockBehavior.Strict, _httpClient);
        var mockPCSSLookupServicesClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, _httpClient);
        MockReportServicesClient = new Mock<PCSSReportServices.ReportServicesClient>(MockBehavior.Strict, _httpClient);

        // Cache setup
        CachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Mapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new CalendarMapping());
        config.Apply(new LocationMapping());
        Mapper = new Mapper(config);

        var mockDocCatService = new Mock<IDocumentCategoryService>();

        // Service mocks
        MockLookupService = new Mock<LookupService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            mockLookupCodeServicesClient.Object,
            CachingService,
            mockDocCatService.Object);

        MockLocationService = new Mock<LocationService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            mockJCLocationServicesClient.Object,
            mockPCSSLocationServicesClient.Object,
            mockPCSSLookupServicesClient.Object,
            CachingService,
            Mapper);

        // Default ClaimsPrincipal
        Principal = CreateDefaultPrincipal();

        // Create the mock service
        CreateMockService();
    }

    private void CreateMockService()
    {
        MockCourtListService = new Mock<CourtListService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            MockLogger.Object,
            MockFileServicesClient.Object,
            Mapper,
            MockLookupService.Object,
            MockLocationService.Object,
            MockSearchDateClient.Object,
            MockReportServicesClient.Object,
            CachingService,
            Principal);
    }

    public CourtListServiceFixture WithPrincipal(ClaimsPrincipal principal)
    {
        Principal = principal;
        CreateMockService(); // Recreate with new principal
        return this;
    }

    public CourtListServiceFixture WithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Cookies");
        Principal = new ClaimsPrincipal(identity);
        CreateMockService(); // Recreate with new principal
        return this;
    }

    private ClaimsPrincipal CreateDefaultPrincipal()
    {
        var claims = new[]
        {
            new Claim(CustomClaimTypes.ApplicationCode, "SCV"),
            new Claim(CustomClaimTypes.JcParticipantId, _faker.Random.AlphaNumeric(10)),
            new Claim(CustomClaimTypes.JcAgencyCode, _faker.Random.AlphaNumeric(10)),
            new Claim(CustomClaimTypes.IsSupremeUser, "True"),
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Resets all mocks to their initial state. Useful when sharing fixture across tests.
    /// </summary>
    public void Reset()
    {
        MockCourtListService.Reset();
        MockFileServicesClient.Reset();
        MockSearchDateClient.Reset();
        MockReportServicesClient.Reset();
        MockLookupService.Reset();
        MockLocationService.Reset();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}