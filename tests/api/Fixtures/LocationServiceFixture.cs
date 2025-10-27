using System;
using System.Net.Http;
using Bogus;
using JCCommon.Clients.LocationServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Fixtures;

/// <summary>
/// Reusable fixture for creating Mock<LocationService> with all dependencies configured.
/// </summary>
public class LocationServiceFixture : IDisposable
{
    private readonly Faker _faker;
    private readonly HttpClient _httpClient;

    public Mock<LocationService> MockLocationService { get; private set; }
    public Mock<IConfiguration> MockConfiguration { get; }
    public Mock<LocationServicesClient> MockJCLocationServicesClient { get; }
    public Mock<PCSSLocationServices.LocationServicesClient> MockPCSSLocationServicesClient { get; }
    public Mock<PCSSLookupServices.LookupServicesClient> MockPCSSLookupServicesClient { get; }
    public IAppCache Cache { get; }
    public IMapper Mapper { get; }

    public LocationServiceFixture()
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
        var config = new TypeAdapterConfig();
        config.Apply(new LocationMapping());
        Mapper = new Mapper(config);

        // Client mocks
        MockJCLocationServicesClient = new Mock<LocationServicesClient>(MockBehavior.Strict, _httpClient);
        MockPCSSLocationServicesClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, _httpClient);
        MockPCSSLookupServicesClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, _httpClient);

        // Create the mock service
        CreateMockService();
    }

    private void SetupConfiguration()
    {
        var mockCachingSection = new Mock<IConfigurationSection>();
        mockCachingSection.Setup(s => s.Value).Returns("60");
        MockConfiguration.Setup(c => c.GetSection("Caching:LocationExpiryMinutes"))
            .Returns(mockCachingSection.Object);
    }

    private void CreateMockService()
    {
        MockLocationService = new Mock<LocationService>(
            MockBehavior.Strict,
            MockConfiguration.Object,
            MockJCLocationServicesClient.Object,
            MockPCSSLocationServicesClient.Object,
            MockPCSSLookupServicesClient.Object,
            Cache,
            Mapper);
    }

    /// <summary>
    /// Resets all mocks to their initial state.
    /// </summary>
    public void Reset()
    {
        MockLocationService.Reset();
        MockJCLocationServicesClient.Reset();
        MockPCSSLocationServicesClient.Reset();
        MockPCSSLookupServicesClient.Reset();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}