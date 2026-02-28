using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PCSSCommon.Clients.PersonServices;
using Scv.Api.Models.Location;
using Scv.Api.Services;
using tests.api.Fixtures;
using Xunit;
using PcssPersonSearchItem = PCSSCommon.Models.PersonSearchItem;
using ScvPerson = Scv.Api.Models.Person;

namespace tests.api.Services;

public class JudgeServiceTests : ServiceTestBase, IClassFixture<LocationServiceFixture>
{
    private readonly Mock<PersonServicesClient> _mockPersonClient;
    private readonly LocationServiceFixture _locationServiceFixture;
    private readonly IAppCache _cache;
    private readonly JudgeService _judgeService;
    private readonly Faker _faker;

    public JudgeServiceTests(LocationServiceFixture locationServiceFixture)
    {
        _faker = new Faker();

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
        _cache = cachingService;

        _locationServiceFixture = locationServiceFixture;
        // Reset the fixture to ensure clean state for each test
        _locationServiceFixture.Reset();

        _mockPersonClient = new Mock<PersonServicesClient>(
            MockBehavior.Default,
            this.HttpClient
        );

        _judgeService = new JudgeService(
            _cache,
            _locationServiceFixture.MockLocationService.Object,
            _mockPersonClient.Object
        );
    }

    #region GetJudges Tests

    [Fact]
    public async Task GetJudges_ReturnsAllJudges_WhenNoPositionCodesProvided()
    {
        var locations = CreateLocationsList(2);
        var judges = CreateJudgesList(5);

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var result = await _judgeService.GetJudges();

        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        _mockPersonClient.Verify(c => c.GetJudicialListingAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            false,
            ""), Times.Once);
    }

    [Fact]
    public async Task GetJudges_ReturnsEmptyList_WhenNoJudgesFound()
    {
        var locations = CreateLocationsList(1);

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync([]);

        var result = await _judgeService.GetJudges();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJudges_FiltersJudgesByPositionCode_WhenPositionCodesProvided()
    {
        var locations = CreateLocationsList(2);
        var judges = new List<PcssPersonSearchItem>
        {
            CreateJudge("Smith", "John", JudgeService.CHIEF_JUDGE),
            CreateJudge("Jones", "Jane", JudgeService.PUISNE_JUDGE),
            CreateJudge("Brown", "Bob", JudgeService.SENIOR_JUDGE),
            CreateJudge("Davis", "Diana", JudgeService.ASSOC_CHIEF_JUDGE)
        };

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var positionCodes = new List<string> { JudgeService.CHIEF_JUDGE, JudgeService.PUISNE_JUDGE };
        var result = await _judgeService.GetJudges(positionCodes);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, judge => Assert.Contains(judge.PositionCode, positionCodes));
    }

    [Fact]
    public async Task GetJudges_ReturnsJudgesOrderedByFullName()
    {
        var locations = CreateLocationsList(1);
        var judges = new List<PcssPersonSearchItem>
        {
            CreateJudge("Zulu", "Alpha", JudgeService.CHIEF_JUDGE),
            CreateJudge("Alpha", "Zulu", JudgeService.PUISNE_JUDGE),
            CreateJudge("Mike", "Bravo", JudgeService.SENIOR_JUDGE)
        };

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var result = await _judgeService.GetJudges();

        var judgesList = result.ToList();
        Assert.Equal("Alpha Zulu", judgesList[0].FullName);
        Assert.Equal("Bravo Mike", judgesList[1].FullName);
        Assert.Equal("Zulu Alpha", judgesList[2].FullName);
    }

    [Fact]
    public async Task GetJudges_ReturnsEmptyList_WhenPositionCodeNotMatched()
    {
        var locations = CreateLocationsList(1);
        var judges = new List<PcssPersonSearchItem>
        {
            CreateJudge("Smith", "John", JudgeService.CHIEF_JUDGE),
            CreateJudge("Jones", "Jane", JudgeService.PUISNE_JUDGE)
        };

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var positionCodes = new List<string> { "UNKNOWN" };
        var result = await _judgeService.GetJudges(positionCodes);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJudges_FiltersOutLocationsWithNullLocationId()
    {
        var locations = new List<Location>
        {
            Location.Create(
                _faker.Address.City(),
                _faker.Address.CityPrefix(),
                "1",
                true,
                []),
            Location.Create(_faker.Address.City(),
                _faker.Address.CityPrefix(),
                null,
                true,
                []),
            Location.Create(_faker.Address.City(),
                _faker.Address.CityPrefix(),
                "2",
                true,
                []),
        };

        var judges = CreateJudgesList(2);

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                "1,2",
                false,
                ""))
            .ReturnsAsync(judges);

        var result = await _judgeService.GetJudges();

        Assert.NotNull(result);
        _mockPersonClient.Verify(c => c.GetJudicialListingAsync(
            It.IsAny<string>(),
            "1,2",
            false,
            ""), Times.Once);
    }

    [Fact]
    public async Task GetJudges_UsesProvidedLocationIds_WhenLocationIdsProvided()
    {
        var judges = CreateJudgesList(2);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                "1,2",
                false,
                ""))
            .ReturnsAsync(judges);

        var locationIds = new List<string> { "1", "2", " ", "1" };
        var result = await _judgeService.GetJudges(locationIds: locationIds);

        Assert.NotNull(result);
        _mockPersonClient.Verify(c => c.GetJudicialListingAsync(
            It.IsAny<string>(),
            "1,2",
            false,
            ""), Times.Once);
        _locationServiceFixture.VerifyGetLocationsCalled(Times.Never());
    }

    [Fact]
    public async Task GetJudges_CallsLocationServiceOnce()
    {
        var locations = CreateLocationsList(1);
        var judges = CreateJudgesList(1);

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        await _judgeService.GetJudges();

        _locationServiceFixture.VerifyGetLocationsCalled(Times.Once());
    }

    [Fact]
    public async Task GetJudges_HandlesEmptyPositionCodesList()
    {
        var locations = CreateLocationsList(1);
        var judges = CreateJudgesList(3);

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var result = await _judgeService.GetJudges(new List<string>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Theory]
    [InlineData(JudgeService.CHIEF_JUDGE)]
    [InlineData(JudgeService.ASSOC_CHIEF_JUDGE)]
    [InlineData(JudgeService.REGIONAL_ADMIN_JUDGE)]
    [InlineData(JudgeService.PUISNE_JUDGE)]
    [InlineData(JudgeService.SENIOR_JUDGE)]
    public async Task GetJudges_FiltersBySpecificPositionCode(string positionCode)
    {
        var locations = CreateLocationsList(1);
        var judges = new List<PcssPersonSearchItem>
        {
            CreateJudge("Smith", "John", JudgeService.CHIEF_JUDGE),
            CreateJudge("Jones", "Jane", JudgeService.ASSOC_CHIEF_JUDGE),
            CreateJudge("Brown", "Bob", JudgeService.REGIONAL_ADMIN_JUDGE),
            CreateJudge("Davis", "Diana", JudgeService.PUISNE_JUDGE),
            CreateJudge("Wilson", "Wendy", JudgeService.SENIOR_JUDGE)
        };

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var result = await _judgeService.GetJudges(new List<string> { positionCode });

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(positionCode, result.First().PositionCode);
    }

    [Fact]
    public async Task GetJudges_HandlesMultiplePositionCodes()
    {
        var locations = CreateLocationsList(1);
        var judges = new List<PcssPersonSearchItem>
        {
            CreateJudge("Smith", "John", JudgeService.CHIEF_JUDGE),
            CreateJudge("Jones", "Jane", JudgeService.ASSOC_CHIEF_JUDGE),
            CreateJudge("Brown", "Bob", JudgeService.PUISNE_JUDGE),
            CreateJudge("Davis", "Diana", "OTHER")
        };

        _locationServiceFixture.SetupGetLocations(locations);

        _mockPersonClient
            .Setup(c => c.GetJudicialListingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                false,
                ""))
            .ReturnsAsync(judges);

        var positionCodes = new List<string>
        {
            JudgeService.CHIEF_JUDGE,
            JudgeService.ASSOC_CHIEF_JUDGE,
            JudgeService.PUISNE_JUDGE
        };
        var result = await _judgeService.GetJudges(positionCodes);

        Assert.Equal(3, result.Count());
        Assert.DoesNotContain(result, j => j.PositionCode == "OTHER");
    }

    #endregion

    #region GetJudge Tests

    [Fact]
    public async Task GetJudge_ReturnsJudge_WhenJudgeExists()
    {
        var judgeId = _faker.Random.Int(1000, 9999);
        var person = CreatePerson(judgeId);

        _mockPersonClient
            .Setup(c => c.ReadPersonAsync(judgeId))
            .ReturnsAsync(person);

        var result = await _judgeService.GetJudge(judgeId);

        Assert.NotNull(result);
        Assert.Equal(judgeId, result.Id);
    }

    [Fact]
    public async Task GetJudge_CallsPersonClientWithCorrectId()
    {
        var judgeId = _faker.Random.Int(1000, 9999);
        var person = CreatePerson(judgeId);

        _mockPersonClient
            .Setup(c => c.ReadPersonAsync(judgeId))
            .ReturnsAsync(person);

        await _judgeService.GetJudge(judgeId);

        _mockPersonClient.Verify(c => c.ReadPersonAsync(judgeId), Times.Once);
    }

    [Fact]
    public async Task GetJudge_ReturnsNull_WhenJudgeNotFound()
    {
        var judgeId = _faker.Random.Int(1000, 9999);

        _mockPersonClient
            .Setup(c => c.ReadPersonAsync(judgeId))
            .ReturnsAsync((ScvPerson)null);

        var result = await _judgeService.GetJudge(judgeId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetJudge_DeserializesPersonCorrectly()
    {
        var judgeId = _faker.Random.Int(1000, 9999);
        var person = CreatePerson(judgeId);

        _mockPersonClient
            .Setup(c => c.ReadPersonAsync(judgeId))
            .ReturnsAsync(person);

        var result = await _judgeService.GetJudge(judgeId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetJudge_PreservesAllPersonProperties()
    {
        var judgeId = _faker.Random.Int(1000, 9999);
        var person = CreatePerson(judgeId);
        person.HomeLocationId = 123;

        _mockPersonClient
            .Setup(c => c.ReadPersonAsync(judgeId))
            .ReturnsAsync(person);

        var result = await _judgeService.GetJudge(judgeId);

        Assert.NotNull(result);
        Assert.Equal(123, result.HomeLocationId);
    }

    #endregion

    #region Helper Methods

    private ICollection<Location> CreateLocationsList(int count)
    {
        var locations = new List<Location>();
        for (int i = 1; i <= count; i++)
        {
            locations.Add(Location.Create(
                _faker.Address.City(),
                _faker.Address.CityPrefix(),
                _faker.Random.Int(1, 100).ToString(),
                true,
                []));
        }
        return locations;
    }

    private ICollection<PcssPersonSearchItem> CreateJudgesList(int count)
    {
        var judges = new List<PcssPersonSearchItem>();
        var positionCodes = new[]
        {
            JudgeService.CHIEF_JUDGE,
            JudgeService.ASSOC_CHIEF_JUDGE,
            JudgeService.REGIONAL_ADMIN_JUDGE,
            JudgeService.PUISNE_JUDGE,
            JudgeService.SENIOR_JUDGE
        };

        for (int i = 0; i < count; i++)
        {
            judges.Add(CreateJudge(
                _faker.Name.LastName(),
                _faker.Name.FirstName(),
                _faker.PickRandom(positionCodes)
            ));
        }

        return judges;
    }

    private PcssPersonSearchItem CreateJudge(string lastName, string firstName, string positionCode)
    {
        return new PcssPersonSearchItem
        {
            PersonId = _faker.Random.Int(1000, 9999),
            UserId = _faker.Random.Int(1000, 9999),
            LastName = lastName,
            FirstName = firstName,
            FullName = $"{firstName} {lastName}",
            PositionCode = positionCode,
            PositionDescription = GetPositionDescription(positionCode),
            HomeLocationId = _faker.Random.Int(1, 100),
            HomeLocationName = _faker.Address.City(),
            StatusCode = "A",
            StatusDescription = "Active",
            RotaInitials = _faker.Random.String2(2, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        };
    }

    private ScvPerson CreatePerson(int personId)
    {
        return new ScvPerson
        {
            Id = personId,
            UserId = _faker.Random.Int(1000, 9999),
            HomeLocationId = _faker.Random.Int(1, 100),
        };
    }

    private string GetPositionDescription(string positionCode)
    {
        return positionCode switch
        {
            JudgeService.CHIEF_JUDGE => "Chief Judge",
            JudgeService.ASSOC_CHIEF_JUDGE => "Associate Chief Judge",
            JudgeService.REGIONAL_ADMIN_JUDGE => "Regional Administrative Judge",
            JudgeService.PUISNE_JUDGE => "Puisne Judge",
            JudgeService.SENIOR_JUDGE => "Senior Judge",
            _ => "Unknown"
        };
    }

    #endregion
}