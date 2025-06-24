using System;
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
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Clients.SearchDateServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.Calendar;
using Scv.Api.Services;
using Xunit;
using static PCSSCommon.Models.ActivityClassUsage;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace tests.api.Services;
public class DashboardServiceTests : ServiceTestBase
{
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly CachingService _cachingService;
    private readonly Mock<IConfiguration> _mockConfig;

    public DashboardServiceTests()
    {
        _faker = new Faker();

        // Setup Cache
        _cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockSection.Object);

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new CalendarMapping());
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);

    }

    private (
        DashboardService dashboardService,
        Mock<JudicialCalendarServicesClient> mockJudicialCalenderClient,
        Mock<SearchDateClient> mockSearchDateClient

    ) SetupDashboardService(
        JudicialCalendar schedule,
        ActivityClassUsage.ActivityAppearanceResultsCollection courtList)
    {
        var mockJudicialCalendarClient = new Mock<JudicialCalendarServicesClient>(MockBehavior.Strict, this.HttpClient);
        mockJudicialCalendarClient
            .Setup(c => c.ReadCalendarV2Async(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(schedule);

        var mockSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, this.HttpClient);
        mockSearchDateClient
            .Setup(c => c.GetCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                null))
            .ReturnsAsync(courtList);

        var mockLocationService = this.SetupLocationService();

        var dashboardService = new DashboardService(
            _cachingService,
            mockJudicialCalendarClient.Object,
            mockSearchDateClient.Object,
            mockLocationService.Object);

        return (
            dashboardService,
            mockJudicialCalendarClient,
            mockSearchDateClient
        );
    }

    private Mock<LocationService> SetupLocationService()
    {
        var mockJCLocationClient = new Mock<LocationServicesClient>(MockBehavior.Strict, this.HttpClient);
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

        return mockLocationService;
    }

    [Fact]
    public async Task GetMySchedule_Returns_Failure_When_Dates_Are_Invalid()
    {
        var (dashboardService, _, _) = this.SetupDashboardService(null, null);

        var result = await dashboardService.GetMyScheduleAsync(
            _faker.Random.Int(),
            _faker.Lorem.Word(),
            _faker.Lorem.Word(),
            _faker.Lorem.Word());

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("currentDate, startDate and/or endDate is invalid.", result.Errors[0]);
    }

    [Fact]
    public async Task GetMySchedule_Returns_Success_When_Schedule_Is_Whole_Day()
    {
        var mockJudgeId = _faker.Random.Int();
        var mockLocationId = _faker.Random.Int();
        var mockLocationName = _faker.Address.City();
        var mockActivityCode = _faker.Lorem.Word();
        var mockActivityDisplayCode = _faker.Lorem.Word();
        var mockActivityClassDescription = _faker.Lorem.Word();
        var mockIsRemote = _faker.Random.Bool();

        var mockJudicialCalendar = new JudicialCalendar
        {
            Days =
            [
                new JudicialCalendarDay
                {
                    Assignment = new JudicialCalendarAssignment
                    {
                        LocationId = mockLocationId,
                        LocationName = mockLocationName,
                        ActivityCode = mockActivityCode,
                        ActivityDisplayCode = mockActivityDisplayCode,
                        ActivityClassDescription = mockActivityClassDescription,
                        IsVideo = mockIsRemote
                    }
                }
            ]
        };

        var (dashboardService, mockJudicialCalendarClient, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            currentDate.ToString(DashboardService.DATE_FORMAT),
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload.Days);
        Assert.Single(result.Payload.Days[0].Activities);
        Assert.Equal(mockLocationId, result.Payload.Days[0].Activities[0].LocationId);
        Assert.Equal(mockLocationName, result.Payload.Days[0].Activities[0].LocationName);
        Assert.Equal(mockActivityCode, result.Payload.Days[0].Activities[0].ActivityCode);
        Assert.True(string.IsNullOrEmpty(result.Payload.Days[0].Activities[0].ActivityDisplayCode));
        Assert.Equal(mockActivityClassDescription, result.Payload.Days[0].Activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, result.Payload.Days[0].Activities[0].IsRemote);
        Assert.Null(result.Payload.Days[0].Activities[0].Period);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedule_Returns_Success_When_Schedule_Is_Whole_But_Different_Location()
    {
        var mockJudgeId = _faker.Random.Int();
        var mockLocationId1 = _faker.Random.Int();
        var mockLocationId2 = _faker.Random.Int();
        var mockLocationName = _faker.Address.City();
        var mockActivityCode = _faker.Lorem.Word();
        var mockActivityDisplayCode = _faker.Lorem.Word();
        var mockActivityClassDescription = _faker.Lorem.Word();
        var mockIsRemote = _faker.Random.Bool();

        var mockJudicialCalendar = new JudicialCalendar
        {
            Days =
            [
                new JudicialCalendarDay
                {
                    Assignment = new JudicialCalendarAssignment
                    {

                        ActivityAm = new JudicialCalendarActivity
                        {
                            LocationId = mockLocationId1,
                            LocationName = mockLocationName,
                            ActivityCode = mockActivityCode,
                            ActivityDisplayCode = mockActivityDisplayCode,
                            ActivityClassDescription = mockActivityClassDescription,
                            IsVideo = mockIsRemote,
                        },
                        ActivityPm = new JudicialCalendarActivity
                        {
                            LocationId = mockLocationId2,
                            LocationName = mockLocationName,
                            ActivityCode = mockActivityCode,
                            ActivityDisplayCode = mockActivityDisplayCode,
                            ActivityClassDescription = mockActivityClassDescription,
                            IsVideo = mockIsRemote,
                        },
                    }
                }
            ]
        };

        var (dashboardService, mockJudicialCalendarClient, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            currentDate.ToString(DashboardService.DATE_FORMAT),
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload.Days);
        Assert.Equal(2, result.Payload.Days[0].Activities.Count);
        Assert.Equal(mockLocationId1, result.Payload.Days[0].Activities[0].LocationId);
        Assert.Equal(mockLocationName, result.Payload.Days[0].Activities[0].LocationName);
        Assert.Equal(mockActivityCode, result.Payload.Days[0].Activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, result.Payload.Days[0].Activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, result.Payload.Days[0].Activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, result.Payload.Days[0].Activities[0].IsRemote);

        Assert.Equal(Period.AM, result.Payload.Days[0].Activities[0].Period);
        Assert.Equal(Period.PM, result.Payload.Days[0].Activities[1].Period);
        Assert.Equal(mockLocationId2, result.Payload.Days[0].Activities[1].LocationId);


        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedule_Returns_Success_With_Scheduled_Files()
    {
        var mockJudgeId = _faker.Random.Int();
        var mockLocationId1 = _faker.Random.Int();
        var mockLocationId2 = _faker.Random.Int();
        var mockLocationName = _faker.Address.City();
        var mockActivityCode = _faker.Lorem.Word();
        var mockActivityDisplayCode = _faker.Lorem.Word();
        var mockActivityClassDescription = _faker.Lorem.Word();
        var mockIsRemote = _faker.Random.Bool();
        var mockCourtRoom = _faker.Address.BuildingNumber();
        var mockFileCount = _faker.Random.Int();
        var currentDate = DateTime.Now;


        var mockJudicialCalendar = new JudicialCalendar
        {
            Days =
            [
                new JudicialCalendarDay
                {
                    Date = currentDate.ToString(DashboardService.DATE_FORMAT),
                    Assignment = new JudicialCalendarAssignment
                    {

                        ActivityAm = new JudicialCalendarActivity
                        {
                            LocationId = mockLocationId1,
                            LocationName = mockLocationName,
                            ActivityCode = mockActivityCode,
                            ActivityDisplayCode = mockActivityDisplayCode,
                            ActivityClassDescription = mockActivityClassDescription,
                            IsVideo = mockIsRemote,
                            CourtRoomCode = mockCourtRoom,
                        },
                        ActivityPm = new JudicialCalendarActivity
                        {
                            LocationId = mockLocationId2,
                            LocationName = mockLocationName,
                            ActivityCode = mockActivityCode,
                            ActivityDisplayCode = mockActivityDisplayCode,
                            ActivityClassDescription = mockActivityClassDescription,
                            IsVideo = mockIsRemote,
                        },
                    }
                }
            ]
        }; ;

        var mockCourtList = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items =
            [
                new() {
                    ActivityCd = mockActivityCode,
                    CasesTarget = mockFileCount,
                    CourtRoomDetails =
                    [
                        new CourtRoomDetail
                        {
                            CourtRoomCd = mockCourtRoom,
                            AdjudicatorDetails =
                            [
                                new() {
                                    AdjudicatorId = mockJudgeId
                                }
                            ]
                        }
                    ],
                    Appearances =
                    [
                        new() {
                            ContinuationYn = "Y"
                        }
                    ]
                }
            ]
        };

        var (dashboardService,
            mockJudicialCalendarClient,
            mockSearchDateClient) = this.SetupDashboardService(mockJudicialCalendar, mockCourtList);

        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            currentDate.ToString(DashboardService.DATE_FORMAT),
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload.Days);
        Assert.Equal(2, result.Payload.Days[0].Activities.Count);
        Assert.Equal(mockLocationId1, result.Payload.Days[0].Activities[0].LocationId);
        Assert.Equal(mockLocationName, result.Payload.Days[0].Activities[0].LocationName);
        Assert.Equal(mockActivityCode, result.Payload.Days[0].Activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, result.Payload.Days[0].Activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, result.Payload.Days[0].Activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, result.Payload.Days[0].Activities[0].IsRemote);

        Assert.Equal(Period.AM, result.Payload.Days[0].Activities[0].Period);
        Assert.Equal(Period.PM, result.Payload.Days[0].Activities[1].Period);
        Assert.Equal(mockLocationId2, result.Payload.Days[0].Activities[1].LocationId);

        Assert.Equal(mockFileCount, result.Payload.Days[0].Activities[0].FilesCount);
        Assert.Equal(1, result.Payload.Days[0].Activities[0].ContinuationsCount);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once);

        mockSearchDateClient
            .Verify(sdc => sdc
                .GetCourtListAppearancesAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    null), Times.AtMost(2));
    }

    [Fact]
    public async Task GetMySchedule_Returns_Success_WhenCurrentDateIsNotInBetweenStartAndEndDate()
    {
        var mockJudgeId = _faker.Random.Int();
        var mockLocationId = _faker.Random.Int();
        var mockLocationName = _faker.Address.City();
        var mockActivityCode = _faker.Lorem.Word();
        var mockActivityDisplayCode = _faker.Lorem.Word();
        var mockActivityClassDescription = _faker.Lorem.Word();
        var mockIsRemote = _faker.Random.Bool();

        var mockJudicialCalendar = new JudicialCalendar
        {
            Days =
            [
                new JudicialCalendarDay
                {
                    Assignment = new JudicialCalendarAssignment
                    {
                        LocationId = mockLocationId,
                        LocationName = mockLocationName,
                        ActivityCode = mockActivityCode,
                        ActivityDisplayCode = mockActivityDisplayCode,
                        ActivityClassDescription = mockActivityClassDescription,
                        IsVideo = mockIsRemote
                    }
                }
            ]
        };

        var (dashboardService, mockJudicialCalendarClient, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddMonths(-5);
        var endDate = currentDate.AddMonths(-4);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            currentDate.ToString(DashboardService.DATE_FORMAT),
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload.Days);
        Assert.Single(result.Payload.Days[0].Activities);
        Assert.Equal(mockLocationId, result.Payload.Days[0].Activities[0].LocationId);
        Assert.Equal(mockLocationName, result.Payload.Days[0].Activities[0].LocationName);
        Assert.Equal(mockActivityCode, result.Payload.Days[0].Activities[0].ActivityCode);
        Assert.True(string.IsNullOrEmpty(result.Payload.Days[0].Activities[0].ActivityDisplayCode));
        Assert.Equal(mockActivityClassDescription, result.Payload.Days[0].Activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, result.Payload.Days[0].Activities[0].IsRemote);
        Assert.Null(result.Payload.Days[0].Activities[0].Period);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Exactly(2));
    }
}
