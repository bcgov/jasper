using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.LocationServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Xunit;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Services;

public class LocationServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly IMapper _mapper;

    public LocationServiceTests()
    {
        _faker = new Faker();

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockSection.Object);

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);
    }

    private (LocationService locationService,
        Mock<LocationServicesClient> mockJCLocationClient,
        Mock<PCSSLocationServices.LocationServicesClient> mockPCSSLocationClient,
        Mock<PCSSLookupServices.LookupServicesClient> mockPCSSLookupClient
    ) SetupLocationService(
        List<CodeValue> jcLocations,
        List<Location> pcssLocations,
        List<CodeValue> jcCourtRooms = null)
    {
        // Setup Services Client
        var mockJCLocationClient = new Mock<LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockPCSSLocationClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockPCSSLookupClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, this.HttpClient);

        mockJCLocationClient
            .Setup(c => c.LocationsGetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(() => jcLocations);
        mockJCLocationClient
            .Setup(c => c.LocationsRoomsGetAsync())
            .ReturnsAsync(jcCourtRooms);

        mockPCSSLocationClient
            .Setup(c => c.GetLocationsAsync())
            .ReturnsAsync(() => pcssLocations);

        mockPCSSLookupClient
            .Setup(c => c.GetCourtRoomsAsync())
            .ReturnsAsync(() => pcssLocations);

        // Setup Cache
        var mockCache = new Mock<IAppCache>();
        mockCache
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<ICacheEntry, Task<string>>>(),
                It.IsAny<MemoryCacheEntryOptions>()))
            .Returns(
                (string key,
                Func<ICacheEntry,
                Task<string>> addItem,
                MemoryCacheEntryOptions opts) => addItem(null));

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Instantiate the LocationService using Mocks
        var locationService = new LocationService(
            _mockConfig.Object,
            mockJCLocationClient.Object,
            mockPCSSLocationClient.Object,
            mockPCSSLookupClient.Object,
            cachingService,
            _mapper);

        return (
            locationService,
            mockJCLocationClient,
            mockPCSSLocationClient,
            mockPCSSLookupClient
        );
    }

    [Fact]
    public async Task GetLocations_ShouldMergeWhenJustinAgenIdExists()
    {
        var mockCode = _faker.Random.Double(0, 100);
        var mockJCLocations = new List<CodeValue>(
            [
                new()
                {
                    Code = mockCode.ToString(),
                    ShortDesc = _faker.Random.AlphaNumeric(5),
                    LongDesc = string.Join(" ", _faker.Lorem.Words()),
                    Flex = "Y"
                }
            ]);
        var mockPCSSLocations = new List<Location>(
            [
                new()
                {
                    JustinAgenId = mockCode,
                    LocationSNm = _faker.Random.AlphaNumeric(5)
                }
            ]);

        var (locationService, mockJCLocationClient, mockPCSSLocationClient, _) = this.SetupLocationService(mockJCLocations, mockPCSSLocations);
        var result = await locationService.GetLocations();

        Assert.NotNull(result);
        Assert.Single(result);

        var location = result.Single();
        Assert.Equal(mockJCLocations[0].LongDesc, location.Name);
        Assert.Equal(mockJCLocations[0].Code, location.Code);
        Assert.True(location.Active.GetValueOrDefault());

        mockJCLocationClient.Verify(c => c.LocationsGetAsync(null, true, true), Times.Once());
        mockPCSSLocationClient.Verify(c => c.GetLocationsAsync(), Times.Once());
    }

    [Fact]
    public async Task GetLocations_ShouldMergeWhenLocationSNmExists()
    {
        var mockShortDesc = _faker.Random.AlphaNumeric(5);
        var mockJCLocations = new List<CodeValue>(
            [
                new()
                {
                    Code = _faker.Random.Double(0, 100).ToString(),
                    ShortDesc = mockShortDesc,
                    LongDesc = string.Join(" ", _faker.Lorem.Words()),
                    Flex = "Y"
                }
            ]);
        var mockPCSSLocations = new List<Location>(
            [
                new()
                {
                    LocationId = _faker.Random.Int(),
                    LocationSNm = mockShortDesc
                }
            ]);

        var (locationService, mockJCLocationClient, mockPCSSLocationClient, _) = this.SetupLocationService(mockJCLocations, mockPCSSLocations);
        var result = await locationService.GetLocations();

        Assert.NotNull(result);
        Assert.Single(result);

        var location = result.Single();
        Assert.Equal(mockJCLocations[0].LongDesc, location.Name);
        Assert.Equal(mockPCSSLocations[0].LocationId.ToString(), location.LocationId);
        Assert.True(location.Active.GetValueOrDefault());

        mockJCLocationClient.Verify(c => c.LocationsGetAsync(null, true, true), Times.Once());
        mockPCSSLocationClient.Verify(c => c.GetLocationsAsync(), Times.Once());
    }

    [Fact]
    public async Task GetLocations_ShouldIgnorePCSSLocationWhenNeitherJustinAgenOrLocationSNmExists()
    {
        var mockJCLocations = new List<CodeValue>(
            [
                new()
                {
                    Code = _faker.Random.Double(0, 100).ToString(),
                    ShortDesc = _faker.Lorem.Text(),
                    LongDesc = string.Join(" ", _faker.Lorem.Words()),
                    Flex = "Y"
                }
            ]);
        var mockPCSSLocations = new List<Location>(
            [
                new()
                {
                    LocationId = _faker.Random.Int(),
                    LocationSNm = _faker.Lorem.Text()
                }
            ]);
        var (locationService, _, _, _) = this.SetupLocationService(mockJCLocations, mockPCSSLocations);

        var result = await locationService.GetLocations();

        Assert.NotNull(result);
        Assert.Single(result);

        var location = result.Single();
        Assert.Null(location.LocationId);
        Assert.Empty(location.CourtRooms);
    }

    [Fact]
    public async Task GetLocations_ShouldIncludeCourtRooms()
    {
        var mockCode = _faker.Random.Double(0, 100);
        var mockShortDescCode = _faker.Random.AlphaNumeric(5);
        var mockLocationId = _faker.Random.Int();
        var mockJCLocations = new List<CodeValue>(
            [
                new()
                {
                    Code = mockCode.ToString(),
                    ShortDesc = mockShortDescCode,
                    LongDesc = string.Join(" ", _faker.Lorem.Words()),
                    Flex = "Y"
                }
            ]);
        var mockJCCourtRooms = new List<CodeValue>(
            [
                new()
                {
                    ShortDesc = _faker.PickRandom(new List<string>{ "HGR", "CRT" }),
                    Flex = mockShortDescCode
                }
            ]);

        var mockPCSSLocations = new List<Location>(
            [
                new()
                {
                    LocationId = mockLocationId,
                    JustinAgenId = mockCode,
                    LocationSNm = _faker.Random.AlphaNumeric(5),
                    CourtRooms = new List<CourtRoom>([ new() { LocationId = mockLocationId } ])
                }
            ]);

        var (
            locationService,
            mockJCLocationClient,
            mockPCSSLocationClient,
            mockPCSSLookupClient
        ) = this.SetupLocationService(mockJCLocations, mockPCSSLocations, mockJCCourtRooms);
        var result = await locationService.GetLocations(true);

        Assert.NotNull(result);
        Assert.Single(result);

        mockJCLocationClient.Verify(c => c.LocationsGetAsync(null, true, true), Times.Once());
        mockJCLocationClient.Verify(c => c.LocationsRoomsGetAsync(), Times.Once());

        mockPCSSLocationClient.Verify(c => c.GetLocationsAsync(), Times.Once());
        mockPCSSLookupClient.Verify(c => c.GetCourtRoomsAsync(), Times.Once());
    }

    [Fact]
    public async Task GetLocationShortName_ShouldSucceed()
    {
        var mockLocationId = _faker.Random.Int();
        var mockShortDesc = _faker.Random.AlphaNumeric(5);
        var mockJCLocations = new List<CodeValue>(
            [
                new()
                {
                    Code = _faker.Random.Double(0, 100).ToString(),
                    ShortDesc = mockShortDesc,
                    LongDesc = string.Join(" ", _faker.Lorem.Words()),
                    Flex = "Y"
                }
            ]);
        var mockPCSSLocations = new List<Location>(
            [
                new()
                {
                    LocationId = mockLocationId,
                    LocationSNm = mockShortDesc
                }
            ]);

        var (locationService, mockJCLocationClient, mockPCSSLocationClient, _) = this.SetupLocationService(mockJCLocations, mockPCSSLocations);
        var result = await locationService.GetLocationShortName(mockLocationId.ToString());

        Assert.Equal(mockShortDesc, result);

        mockJCLocationClient.Verify(c => c.LocationsGetAsync(null, true, true), Times.Once());
        mockPCSSLocationClient.Verify(c => c.GetLocationsAsync(), Times.Once());
    }
}
