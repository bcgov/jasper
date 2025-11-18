using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using DARSCommon.Clients.LogNotesServices;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class DarsServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<DarsService>> _mockLogger;
    private readonly Faker _faker;
    private readonly IMapper _mapper;

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
    }

    private (DarsService darsService,
        Mock<LogNotesServicesClient> mockLogNotesClient
    ) SetupDarsService(
        ICollection<Lognotes> darsResults)
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


        var darsService = new DarsService(
            _mockConfig.Object,
            _mockLogger.Object,
            mockLogNotesClient.Object,
            _mapper);

        return (darsService, mockLogNotesClient);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldReturnResults_WithAbsoluteUrls()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Key = _faker.Random.AlphaNumeric(10),
                Url = "path/to/logsheet.json",
                Region = _faker.Address.State(),
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName()
            }
        };

        var (darsService, mockLogNotesClient) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Equal("https://logsheet.example.com/path/to/logsheet.json", firstResult.Url);
        Assert.Equal("logsheet.json", firstResult.FileName);
        Assert.Equal(agencyIdentifierCd, firstResult.LocationId);
        Assert.Equal(courtRoomCd, firstResult.CourtRoomCd);

        // Verify that DARS API was called with the agencyIdentifierCd (which is the same as agencyIdentifierCd in our test)
        mockLogNotesClient.Verify(c => c.GetBaseAsync(
            It.IsAny<string>(),
            agencyIdentifierCd,  // This is the agencyIdentifierCd converted to int
            courtRoomCd,
            It.IsAny<DateTimeOffset?>(),
            null,
            It.IsAny<System.Threading.CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task DarsApiSearch_ShouldPreferCcdJsonOverOthers()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = agencyIdentifierCd,
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
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_fls_1.pdf",
                Url = "path/to/logsheet_fls_1.pdf",
                Location = agencyIdentifierCd,
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
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = agencyIdentifierCd,
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
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_ccd.html",
                Url = "path/to/logsheet_ccd.html",
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd1 = "ROOM1";
        var courtRoomCd2 = "ROOM2";

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet_room1.json",
                Url = "path/to/logsheet_room1.json",
                Location = agencyIdentifierCd,
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
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd2,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), null);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = null,
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>();

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task DarsApiSearch_ShouldTrimSlashesInUrl()
    {
        // Arrange
        var date = _faker.Date.Recent();
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = "/path/to/logsheet.json",
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = null,
                Url = "path/to/logsheet",
                Location = agencyIdentifierCd,
                LocationName = _faker.Address.City(),
                Room = courtRoomCd,
                DateTime = date.ToString("yyyy-MM-dd"),
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

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
        var agencyIdentifierCd = _faker.Random.Int(1, 100);
        var courtRoomCd = _faker.Random.AlphaNumeric(5);
        var expectedDateTime = date.ToString("yyyy-MM-dd");
        var expectedLocationName = _faker.Address.City();

        var mockLognotes = new List<Lognotes>
        {
            new Lognotes
            {
                FileName = "logsheet.json",
                Url = "path/to/logsheet.json",
                Location = agencyIdentifierCd,
                LocationName = expectedLocationName,
                Room = courtRoomCd,
                DateTime = expectedDateTime,
                Judge = _faker.Name.FullName(),
                Key = _faker.Random.AlphaNumeric(10),
                Region = _faker.Address.State()
            }
        };

        var (darsService, _) = SetupDarsService(mockLognotes);

        // Act
        var result = await darsService.DarsApiSearch(date, agencyIdentifierCd.ToString(), courtRoomCd);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var firstResult = result.First();
        Assert.Equal(expectedDateTime, firstResult.Date);
        Assert.Equal(agencyIdentifierCd, firstResult.LocationId);
        Assert.Equal(courtRoomCd, firstResult.CourtRoomCd);
        Assert.Equal("logsheet.json", firstResult.FileName);
        Assert.Contains("https://logsheet.example.com/path/to/logsheet.json", firstResult.Url);
        Assert.Equal(expectedLocationName, firstResult.LocationNm);
    }
}