using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PCSSCommon.Clients.ConfigurationServices;
using PCSSCommon.Clients.GlobalNonSittingDaysServicesClient;
using PCSSCommon.Models;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class PcssConfigServiceTests : ServiceTestBase
{
    private readonly Faker _faker;

    public PcssConfigServiceTests()
    {
        _faker = new Faker();
    }

    private (PcssConfigService service,
        Mock<ConfigurationServicesClient> mockConfigClient,
        Mock<GlobalNonSittingDaysServicesClient> mockNonSittingDaysClient)
        SetupPcssConfigService(
            List<PcssConfiguration> configurations = null,
            List<NonSittingDay> nonSittingDays = null)
    {
        var mockConfigClient = new Mock<ConfigurationServicesClient>(MockBehavior.Strict, HttpClient);
        var mockNonSittingDaysClient = new Mock<GlobalNonSittingDaysServicesClient>(MockBehavior.Strict, HttpClient);

        mockConfigClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(configurations ?? []);

        if (nonSittingDays != null)
        {
            mockNonSittingDaysClient
                .Setup(c => c.GetAllAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(nonSittingDays);
        }

        var cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var service = new PcssConfigService(
            cache,
            mockConfigClient.Object,
            mockNonSittingDaysClient.Object);

        return (service, mockConfigClient, mockNonSittingDaysClient);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnConfigurationData()
    {
        var key1 = _faker.Lorem.Word().ToUpper();
        var key2 = _faker.Lorem.Word().ToUpper();
        var value1 = _faker.Random.Int(1, 1000).ToString();
        var value2 = _faker.Random.Int(1, 1000).ToString();

        var expectedConfigurations = new List<PcssConfiguration>
        {
            new() { Key = key1, Value = value1 },
            new() { Key = key2, Value = value2 }
        };

        var (service, mockConfigClient, _) = SetupPcssConfigService(expectedConfigurations);

        var result = await service.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Key == key1 && c.Value == value1);
        Assert.Contains(result, c => c.Key == key2 && c.Value == value2);

        mockConfigClient.Verify(c => c.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyCollection_WhenNoConfigurationsExist()
    {
        var (service, mockConfigClient, _) = SetupPcssConfigService([]);

        var result = await service.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);

        mockConfigClient.Verify(c => c.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldReturnStandardWindow_WhenLocationNotInCircuitCourt()
    {
        var startDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = _faker.Random.Int(5, 15);
        var specialWindow = _faker.Random.Int(16, 30);
        var circuitLocation1 = _faker.Random.Int(100, 200);
        var circuitLocation2 = _faker.Random.Int(201, 300);
        var circuitLocation3 = _faker.Random.Int(301, 400);
        var nonCircuitLocation = _faker.Random.Int(401, 500).ToString();

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = specialWindow.ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = $"{circuitLocation1},{circuitLocation2},{circuitLocation3}" }
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, nonCircuitLocation);

        // Result should be greater than the original window due to weekends
        Assert.True(result > lookAheadWindow);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(
            It.Is<DateTimeOffset?>(d => d == startDate),
            It.Is<DateTimeOffset?>(d => d == startDate.AddDays(5 * lookAheadWindow))), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldReturnSpecialWindow_WhenLocationIsInCircuitCourt()
    {
        var startDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified);
        var standardWindow = _faker.Random.Int(10, 20);
        var specialLookAheadWindow = _faker.Random.Int(5, 9);
        var circuitLocation1 = _faker.Random.Int(100, 200).ToString();
        var circuitLocation2 = _faker.Random.Int(201, 300).ToString();
        var circuitLocation3 = _faker.Random.Int(301, 400).ToString();

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = standardWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = specialLookAheadWindow.ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = $"{circuitLocation1},{circuitLocation2},{circuitLocation3}" }
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: circuitLocation2);

        // Result should be greater than the special window due to weekends
        Assert.True(result > specialLookAheadWindow);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(
            It.Is<DateTimeOffset?>(d => d == startDate),
            It.Is<DateTimeOffset?>(d => d == startDate.AddDays(5 * specialLookAheadWindow))), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldReturnStandardWindow_WhenLocationIdIsNull()
    {
        var startDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = _faker.Random.Int(5, 15);
        var specialWindow = _faker.Random.Int(16, 30);
        var circuitLocations = string.Join(",", _faker.Random.Int(100, 200), _faker.Random.Int(201, 300), _faker.Random.Int(301, 400));

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = specialWindow.ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = circuitLocations }
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: null);

        // Result should be greater than the original window due to weekends
        Assert.True(result > lookAheadWindow);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldSkipHolidays()
    {
        var startDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = 5;

        var holidayDate = new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Unspecified); // Monday (Family Day)
        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = _faker.Random.Int(20, 30).ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = "" }
        };
        var nonSittingDays = new List<NonSittingDay>
        {
            new()
            {
                NonSittingDt = holidayDate,
                ActivityType = new ActivityType { ActivityCd = "HOL" }
            }
        };

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: null);

        // 5 days + 1 weekend (2 days) + 1 holiday = 8 days total
        Assert.Equal(8, result);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldSkipWeekendsAndHolidays()
    {
        var startDate = new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = 10;

        var holiday1 = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified);
        var holiday2 = new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Unspecified);
        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = _faker.Random.Int(20, 30).ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = "" }
        };
        var nonSittingDays = new List<NonSittingDay>
        {
            new()
            {
                NonSittingDt = holiday1,
                ActivityType = new ActivityType { ActivityCd = "HOL" }
            },
            new()
            {
                NonSittingDt = holiday2,
                ActivityType = new ActivityType { ActivityCd = "HOL" }
            }
        };

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: null);

        // 10 days + 2 weekends (4 days) + 2 holidays = 16 days total
        Assert.Equal(16, result);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldIgnoreNonHolidayActivities()
    {
        var startDate = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = 5;

        var nonHolidayDate = new DateTime(2026, 2, 12, 0, 0, 0, DateTimeKind.Unspecified);
        var nonHolidayActivityCode = _faker.Random.AlphaNumeric(3).ToUpper();
        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = _faker.Random.Int(20, 30).ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = "" }
        };
        var nonSittingDays = new List<NonSittingDay>
        {
            new()
            {
                NonSittingDt = nonHolidayDate,
                ActivityType = new ActivityType { ActivityCd = nonHolidayActivityCode }
            }
        };

        var (service, _, mockNonSittingDaysClient) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: null);

        // 5 days + 1 weekend (2 days) = 7 days total (non-holiday is ignored)
        Assert.Equal(7, result);

        mockNonSittingDaysClient.Verify(c => c.GetAllAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>()), Times.Once);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldHandleStartDateOnWeekend()
    {
        var startDate = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Unspecified);
        var lookAheadWindow = 3;

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = lookAheadWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = _faker.Random.Int(20, 30).ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = "" }
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, _) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, locationId: null);

        // Starting on Saturday, next 3 days includes Sunday, Monday, Tuesday
        // Sunday is skipped, so we end up with 3 + 1 = 4 days total
        Assert.Equal(4, result);
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldParseCircuitCourtLocationsCorrectly()
    {
        var startDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified); // Tuesday
        var standardWindow = _faker.Random.Int(10, 20);
        var specialLookAheadWindow = _faker.Random.Int(5, 9);
        var circuitLocation1 = _faker.Random.Int(100, 200).ToString();
        var circuitLocation2 = _faker.Random.Int(201, 300).ToString();
        var circuitLocation3 = _faker.Random.Int(301, 400).ToString();

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = standardWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = specialLookAheadWindow.ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = $" {circuitLocation1} , {circuitLocation2} , {circuitLocation3} " } // With spaces
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, _) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, circuitLocation1);

        // Should use special window since location is in the list (even with spaces)
        Assert.True(result > specialLookAheadWindow); // Will be more due to weekends
    }

    [Fact]
    public async Task GetLookAheadWindowAsync_ShouldHandleEmptyCircuitCourtLocations()
    {
        var startDate = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Unspecified); // Tuesday
        var standardWindow = 5;
        var specialWindow = _faker.Random.Int(20, 30);
        var testLocationId = _faker.Random.Int(100, 500).ToString();

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = PcssConfigService.JUDGE_COURT_LIST_LOOKAHEAD_WINDOW, Value = standardWindow.ToString() },
            new() { Key = PcssConfigService.JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW, Value = specialWindow.ToString() },
            new() { Key = PcssConfigService.CIRCUIT_COURT_LOCATIONS, Value = "" }
        };
        var nonSittingDays = new List<NonSittingDay>();

        var (service, _, _) = SetupPcssConfigService(configurations, nonSittingDays);

        var result = await service.GetLookAheadWindowAsync(startDate, testLocationId);

        // Should use standard window since circuit court locations is empty
        Assert.Equal(7, result); // 5 days + 1 weekend = 7
    }

    [Fact]
    public async Task GetAllAsync_ShouldCacheResults()
    {
        var testKey = _faker.Lorem.Word().ToUpper();
        var testValue = _faker.Random.Int(1, 1000).ToString();

        var configurations = new List<PcssConfiguration>
        {
            new() { Key = testKey, Value = testValue }
        };

        var (service, mockConfigClient, _) = SetupPcssConfigService(configurations);

        // Call twice
        var result1 = await service.GetAllAsync();
        var result2 = await service.GetAllAsync();

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Count, result2.Count);

        // Should only call the client once due to caching
        mockConfigClient.Verify(c => c.GetAllAsync(), Times.Once);
    }

    [Fact]
    public void CacheName_ShouldReturnCorrectValue()
    {
        var (service, _, _) = SetupPcssConfigService();

        Assert.Equal(nameof(PcssConfigService), service.CacheName);
    }
}