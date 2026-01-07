using System;
using System.Net.Http;
using System.Security.Claims;
using Bogus;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using LazyCache.Providers;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Helpers;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Core.Helpers;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Fixtures;

/// <summary>
/// Reusable fixture for creating Mock<FilesService> with all dependencies configured.
/// Provides access to both Civil and Criminal file services.
/// </summary>
public class FilesServiceFixture : IDisposable
{
    private readonly Faker _faker;
    private readonly HttpClient _httpClient;

    public Mock<FilesService> MockFilesService { get; private set; }
    public Mock<CivilFilesService> MockCivilFilesService { get; }
    public Mock<CriminalFilesService> MockCriminalFilesService { get; }
    public Mock<IConfiguration> MockConfiguration { get; }
    public Mock<FileServicesClient> MockFileServicesClient { get; }
    public Mock<LookupService> MockLookupService { get; }
    public Mock<LocationService> MockLocationService { get; }
    public Mock<ILoggerFactory> MockLoggerFactory { get; }
    public Mock<ILogger<CivilFilesService>> MockCivilLogger { get; }
    public Mock<IDocumentConverter> MockDocumentConverter { get; }
    public Mock<IBinderService> MockBinderService { get; }
    public IAppCache Cache { get; }
    public IMapper Mapper { get; }
    public ClaimsPrincipal Principal { get; private set; }

    public FilesServiceFixture()
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
        SetupConfiguration();

        // Cache setup
        Cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Mapper setup
        Mapper = new Mapper();

        // LookupService setup
        var dcService = new Mock<IDocumentCategoryService>();
        dcService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var mockLookupCodeServicesClient = new Mock<LookupCodeServicesClient>(MockBehavior.Strict, _httpClient);
        MockLookupService = new Mock<LookupService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            mockLookupCodeServicesClient.Object,
            Cache,
            dcService.Object);

        // LocationService setup
        var mockJCLocationServicesClient = new Mock<LocationServicesClient>(MockBehavior.Strict, _httpClient);
        var mockPCSSLocationServicesClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, _httpClient);
        var mockPCSSLookupServicesClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, _httpClient);

        MockLocationService = new Mock<LocationService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            mockJCLocationServicesClient.Object,
            mockPCSSLocationServicesClient.Object,
            mockPCSSLookupServicesClient.Object,
            Cache,
            Mapper);

        // Client and service mocks
        MockFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, _httpClient);
        MockDocumentConverter = new Mock<IDocumentConverter>();
        MockBinderService = new Mock<IBinderService>();

        // Logger setup
        MockLoggerFactory = new Mock<ILoggerFactory>();
        MockCivilLogger = new Mock<ILogger<CivilFilesService>>();
        MockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(MockCivilLogger.Object);

        // Service mocks
        MockCivilFilesService = new Mock<CivilFilesService>();
        MockCriminalFilesService = new Mock<CriminalFilesService>();

        // Default ClaimsPrincipal
        Principal = CreateDefaultPrincipal();

        // Create the mock service
        CreateMockService();
    }

    private void SetupConfiguration()
    {
        var mockCachingSection = new Mock<IConfigurationSection>();
        mockCachingSection.Setup(s => s.Value).Returns("60");
        MockConfiguration.Setup(c => c.GetSection("Caching:FileExpiryMinutes"))
            .Returns(mockCachingSection.Object);

        var mockLookupCachingSection = new Mock<IConfigurationSection>();
        mockLookupCachingSection.Setup(s => s.Value).Returns("60");
        MockConfiguration.Setup(c => c.GetSection("Caching:LookupExpiryMinutes"))
            .Returns(mockLookupCachingSection.Object);

        var mockLocationCachingSection = new Mock<IConfigurationSection>();
        mockLocationCachingSection.Setup(s => s.Value).Returns("60");
        MockConfiguration.Setup(c => c.GetSection("Caching:LocationExpiryMinutes"))
            .Returns(mockLocationCachingSection.Object);

        var mockAppCodeSection = new Mock<IConfigurationSection>();
        mockAppCodeSection.Setup(s => s.Value).Returns("SCV");
        MockConfiguration.Setup(c => c.GetSection("Request:ApplicationCd"))
            .Returns(mockAppCodeSection.Object);

        var mockAgencySection = new Mock<IConfigurationSection>();
        mockAgencySection.Setup(s => s.Value).Returns(_faker.Random.AlphaNumeric(10));
        MockConfiguration.Setup(c => c.GetSection("Request:AgencyIdentifierId"))
            .Returns(mockAgencySection.Object);

        var mockPartIdSection = new Mock<IConfigurationSection>();
        mockPartIdSection.Setup(s => s.Value).Returns(_faker.Random.AlphaNumeric(10));
        MockConfiguration.Setup(c => c.GetSection("Request:PartId"))
            .Returns(mockPartIdSection.Object);

        var mockFilterOutDocTypesSeciont = new Mock<IConfigurationSection>();
        mockFilterOutDocTypesSeciont.Setup(s => s.Value).Returns(_faker.Lorem.Word());
        MockConfiguration.Setup(c => c.GetSection("ExcludeDocumentTypeCodesForCounsel"))
            .Returns(mockFilterOutDocTypesSeciont.Object);
    }

    private void CreateMockService()
    {
        MockFilesService = new Mock<FilesService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            MockFileServicesClient.Object,
            Mapper,
            MockLookupService.Object,
            MockLocationService.Object,
            Cache,
            Principal,
            MockLoggerFactory.Object,
            MockDocumentConverter.Object,
            MockBinderService.Object);
    }

    public FilesServiceFixture WithPrincipal(ClaimsPrincipal principal)
    {
        Principal = principal;
        CreateMockService();
        return this;
    }

    public FilesServiceFixture WithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Cookies");
        Principal = new ClaimsPrincipal(identity);
        CreateMockService();
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
    /// Resets all mocks to their initial state.
    /// </summary>
    public void Reset()
    {
        MockFilesService.Reset();
        MockFileServicesClient.Reset();
        MockLookupService.Reset();
        MockLocationService.Reset();
        MockDocumentConverter.Reset();
        MockBinderService.Reset();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}