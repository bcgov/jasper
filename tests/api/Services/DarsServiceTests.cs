using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using DARSCommon.Clients.LogNotesServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Xunit;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Services;

public class DarsServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<DarsService>> _mockLogger;
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly CachingService _cachingService;

    public DarsServiceTests()
    {
        _faker = new Faker();
        _mockConfig = new Mock<IConfiguration>();
        
        var mockDarsSection = new Mock<IConfigurationSection>();
        mockDarsSection.Setup(s => s.Value).Returns("https://logsheet.example.com");
        _mockConfig.Setup(c => c.GetSection("DARS:LogsheetUrl")).Returns(mockDarsSection.Object);

        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockSection.Object);

        _mockLogger = new Mock<ILogger<DarsService>>();

        var config = new TypeAdapterConfig();
        config.Apply(new DarsMapping());
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);

        // Setup Cache
        _cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
    }

    private Mock<LocationService> SetupLocationService(Dictionary<string, string> locationIdToAgencyIdMap = null)
    {
        var mockJCLocationClient = new Mock<JCCommon.Clients.LocationServices.LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
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

        // Setup GetAgencyIdentifierCdByLocationId based on the provided map
        if (locationIdToAgencyIdMap != null && locationIdToAgencyIdMap.Any())
        {
            foreach (var kvp in locationIdToAgencyIdMap)
            {
                mockLocationService
                    .Setup(s => s.GetAgencyIdentifierCdByLocationId(kvp.Key))
                    .ReturnsAsync(kvp.Value);
            }
        }
        else
        {
            // Default setup returns null for any location ID
            mockLocationService
                .Setup(s => s.GetAgencyIdentifierCdByLocationId(It.IsAny<string>()))
                .ReturnsAsync((string)null);
        }

        return mockLocationService;
    }

    private (DarsService darsService,
        Mock<LogNotesServicesClient> mockLogNotesClient,
        Mock<LocationService> mockLocationService
    ) SetupDarsService(
        ICollection<Lognotes> darsResults,
        int? locationId = null,
        string agencyIdentifierCd = null)
    {
        var mockLogNotesClient = new Mock<LogNotesServicesClient>(MockBehavior.Strict, this.HttpClient);
        mockLogNotesClient
            .Setup(c => c.GetBaseAsync(
                It.IsAny<string>(),  // region
                It.IsAny<int?>(),    // location
                It.IsAny<string>(),  // room
                It.IsAny<DateTimeOffset?>(),  // datetime
                It.IsAny<string>(),  // judge
                It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(darsResults);

        Dictionary<string, string> locationIdToAgencyIdMap = null;
        if (locationId.HasValue)
        {
            var effectiveAgencyIdentifierCd = agencyIdentifierCd ?? locationId.Value.ToString();
            locationIdToAgencyIdMap = new Dictionary<string, string>
            {
                { locationId.Value.ToString(), effectiveAgencyIdentifierCd }
            };
        }

        var mockLocationService = SetupLocationService(locationIdToAgencyIdMap);

        var darsService = new DarsService(
            _mockConfig.Object,
            _mockLogger.Object,
            mockLogNotesClient.Object,
            mockLocationService.Object,
            _mapper);

        return (darsService, mockLogNotesClient, mockLocationService);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnResults_WithAbsoluteUrls()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Key = _faker.Random.AlphaNumeric(10),
                Url = "path/to/logsheet.json",
                Region = _faker.Address.State(),
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName()
            }
        };

        var (darsService, mockLogNotesClient, mockLocationService) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Equal("https://logsheet.example.com/path/to/logsheet.json", firstResult.Url);
        Assert.Equal("logsheet.json", firstResult.FileName);
        Assert.Equal(locationId, firstResult.LocationId);
        Assert.Equal(courtRoomCd, firstResult.CourtRoomCd);

        // Verify that LocationService was called with the correct locationId
        mockLocationService.Verify(s => s.GetAgencyIdentifierCdByLocationId(locationId.ToString()), Times.Once());

        // Verify that DARS API was called with the agencyIdentifierCd (which is the same as locationId in our test)
        mockLogNotesClient.Verify(c => c.GetBaseAsync(
            null,
            locationId,  // This is the agencyIdentifierCd converted to int
            courtRoomCd,
            It.IsAny<DateTimeOffset?>(),
            null,
            It.IsAny<System.Threading.CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnEmptyResults_WhenLocationNotFound()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>();

        var (darsService, mockLogNotesClient, mockLocationService) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // Verify that LocationService was called
        mockLocationService.Verify(s => s.GetAgencyIdentifierCdByLocationId(locationId.ToString()), Times.Once());

        // Verify that DARS API was NOT called
        mockLogNotesClient.Verify(c => c.GetBaseAsync(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<string>(),
            It.IsAny<System.Threading.CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnEmptyResults_WhenAgencyIdentifierNotNumeric()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);
        var nonNumericAgencyId = "ABC123";

        var mockLognotes = new List<Lognotes>();

        var (darsService, mockLogNotesClient, mockLocationService) = SetupDarsService(mockLognotes, locationId, nonNumericAgencyId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        // Verify that LocationService was called
        mockLocationService.Verify(s => s.GetAgencyIdentifierCdByLocationId(locationId.ToString()), Times.Once());

        // Verify that DARS API was NOT called
        mockLogNotesClient.Verify(c => c.GetBaseAsync(
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset?>(),
            It.IsAny<string>(),
            It.IsAny<System.Threading.CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldPreferCcdJsonOverOthers()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            },
            new Lognotes
            {
                FileName = "logsheet_ccd.json",
                Url = "path/to/logsheet_ccd.json",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Contains("json", firstResult.FileName.ToLowerInvariant());
        Assert.DoesNotContain("html", firstResult.FileName.ToLowerInvariant());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnMultipleFlsLogsheets()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_fls_1.pdf",
                Url = "path/to/logsheet_fls_1.pdf",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            },
            new Lognotes
            {
                FileName = "logsheet_fls_2.pdf",
                Url = "path/to/logsheet_fls_2.pdf",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Contains("fls", r.FileName.ToLowerInvariant()));
    }

    [Fact]
    public async Task DarsApiSearch_ShouldPreferFlsOverHtml()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            },
            new Lognotes
            {
                FileName = "logsheet_fls.pdf",
                Url = "path/to/logsheet_fls.pdf",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Contains("fls", firstResult.FileName.ToLowerInvariant());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnHtmlWhenNoJsonOrFls()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Contains("html", firstResult.FileName.ToLowerInvariant());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldGroupByCourtRoom()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd1 = "ROOM1";
        var courtRoomCd2 = "ROOM2";

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_room1.json",
                Url = "path/to/logsheet_room1.json",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd1,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            },
            new Lognotes
            {
                FileName = "logsheet_room2.json",
                Url = "path/to/logsheet_room2.json",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd2,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        var result = await darsService.DarsApiSearch(date, locationId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.CourtRoomCd == courtRoomCd1);
        Assert.Contains(result, r => r.CourtRoomCd == courtRoomCd2);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldHandleNullUrl()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = null,
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result.First().Url);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldHandleEmptyResults()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>();

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldTrimSlashesInUrl()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = "/path/to/logsheet.json",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Equal("https://logsheet.example.com/path/to/logsheet.json", firstResult.Url);
        Assert.DoesNotContain("//path", firstResult.Url);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldHandleNullFileName()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = null,
                Url = "path/to/logsheet",
                Location = locationId,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result.First().FileName);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldMapAllFields()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var locationId = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);
        var expectedDateTime = date.ToString("yyyy-MM-dd");
        var expectedLocationName = _faker.Address.City();

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = "path/to/logsheet.json",
                Location = locationId,
                LocationName = expectedLocationName,
                Room = courtRoomCd,
                DateTime = expectedDateTime,
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _, _) = SetupDarsService(mockLognotes, locationId);

        // Act
        var result = await darsService.DarsApiSearch(date, locationId, courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Equal(expectedDateTime, firstResult.Date);
        Assert.Equal(locationId, firstResult.LocationId);
        Assert.Equal(courtRoomCd, firstResult.CourtRoomCd);
        Assert.Equal("logsheet.json", firstResult.FileName);
        Assert.Contains("https://logsheet.example.com/path/to/logsheet.json", firstResult.Url);
        Assert.Equal(expectedLocationName, firstResult.LocationNm);
    }
}