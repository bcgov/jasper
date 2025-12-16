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
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Clients.PersonServices;
using PCSSCommon.Clients.SearchDateServices;
using PCSSCommon.Models;
using Scv.Api.Helpers.Extensions;
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
        Mock<SearchDateClient> mockSearchDateClient,
        Mock<LocationService> mockLocationService

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

        var mockPersonServiceClient = new Mock<PersonServicesClient>(MockBehavior.Strict, this.HttpClient);

        var dashboardService = new DashboardService(
            _cachingService,
            mockJudicialCalendarClient.Object,
            mockSearchDateClient.Object,
            mockLocationService.Object,
            _mapper,
            new Mock<ILogger<DashboardService>>().Object,
            mockPersonServiceClient.Object);

        return (
            dashboardService,
            mockJudicialCalendarClient,
            mockSearchDateClient,
            mockLocationService
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

        mockLocationService
            .Setup(l => l.GetLocationShortName(It.IsAny<string>()))
            .ReturnsAsync(string.Empty);

        return mockLocationService;
    }

    [Fact]
    public async Task GetMySchedule_Returns_Failure_When_Dates_Are_Invalid()
    {
        var (dashboardService, _, _, _) = this.SetupDashboardService(null, null);

        var result = await dashboardService.GetMyScheduleAsync(
            _faker.Random.Int(),
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

        var (dashboardService, mockJudicialCalendarClient, _, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload);

        var activity = result.Payload.First().Activities.First();

        Assert.Single(result.Payload[0].Activities);
        Assert.Equal(mockLocationId, activity.LocationId);
        Assert.Equal(mockLocationName, activity.LocationName);
        Assert.Equal(mockActivityCode, activity.ActivityCode);
        Assert.Equal(mockActivityDisplayCode, activity.ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, activity.ActivityClassDescription);
        Assert.Equal(mockIsRemote, activity.IsRemote);
        Assert.Null(activity.Period);

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

        var (dashboardService, mockJudicialCalendarClient, _, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload);
        Assert.Equal(2, result.Payload[0].Activities.Count());

        var firstActivity = result.Payload.First().Activities.First();
        var secondActivity = result.Payload.First().Activities.Skip(1).First();

        Assert.Equal(mockLocationId1, firstActivity.LocationId);
        Assert.Equal(mockLocationName, firstActivity.LocationName);
        Assert.Equal(mockActivityCode, firstActivity.ActivityCode);
        Assert.Equal(mockActivityDisplayCode, firstActivity.ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, firstActivity.ActivityClassDescription);
        Assert.Equal(mockIsRemote, firstActivity.IsRemote);

        Assert.Equal(Period.AM, firstActivity.Period);
        Assert.Equal(Period.PM, secondActivity.Period);
        Assert.Equal(mockLocationId2, secondActivity.LocationId);


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
            _,
            _,
            _) = this.SetupDashboardService(mockJudicialCalendar, mockCourtList);

        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        var activities = result.Payload[0].Activities.ToList();

        Assert.Single(result.Payload);
        Assert.Equal(2, activities.Count);
        Assert.Equal(mockLocationId1, activities[0].LocationId);
        Assert.Equal(mockLocationName, activities[0].LocationName);
        Assert.Equal(mockActivityCode, activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, activities[0].IsRemote);

        Assert.Equal(Period.AM, activities[0].Period);
        Assert.Equal(Period.PM, activities[1].Period);
        Assert.Equal(mockLocationId2, activities[1].LocationId);
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

        var (dashboardService, mockJudicialCalendarClient, _, _) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddMonths(-5);
        var endDate = currentDate.AddMonths(-4);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Single(result.Payload);

        var activities = result.Payload[0].Activities.ToList();

        Assert.Single(activities);
        Assert.Equal(mockLocationId, activities[0].LocationId);
        Assert.Equal(mockLocationName, activities[0].LocationName);
        Assert.Equal(mockActivityCode, activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, activities[0].IsRemote);
        Assert.Null(activities[0].Period);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task GetMySchedule_Returns_Success_For_WeekendAndShowCourtListDays()
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
                    Date = GetRandomWeekend().ToString(DashboardService.DATE_FORMAT),
                    Assignment = new JudicialCalendarAssignment
                    {
                        LocationId = mockLocationId,
                        LocationName = mockLocationName,
                        ActivityCode = mockActivityCode,
                        ActivityDisplayCode = mockActivityDisplayCode,
                        ActivityClassCode = "CrimRem",
                        ActivityClassDescription = mockActivityClassDescription,
                        IsVideo = mockIsRemote
                    }
                },
                new JudicialCalendarDay
                {
                    Date = GetRandomWeekend().ToString(DashboardService.DATE_FORMAT),
                    Assignment = new JudicialCalendarAssignment
                    {
                        LocationId = mockLocationId,
                        LocationName = mockLocationName,
                        ActivityCode = mockActivityCode,
                        ActivityDisplayCode = mockActivityDisplayCode,
                        ActivityClassCode = DashboardService.SITTING_ACTIVITY_CODE,
                        ActivityClassDescription = mockActivityClassDescription,
                        IsVideo = mockIsRemote
                    }
                },
                new JudicialCalendarDay
                {
                    Date = GetRandomWeekend().ToString(DashboardService.DATE_FORMAT),
                    Assignment = new JudicialCalendarAssignment
                    {
                        LocationId = mockLocationId,
                        LocationName = mockLocationName,
                        ActivityCode = mockActivityCode,
                        ActivityDisplayCode = mockActivityDisplayCode,
                        ActivityClassCode = DashboardService.NON_SITTING_ACTIVITY_CODE,
                        ActivityClassDescription = mockActivityClassDescription,
                        IsVideo = mockIsRemote
                    }
                }
            ]
        };

        var (dashboardService,
            mockJudicialCalendarClient,
            _,
            mockLocationService) = this.SetupDashboardService(mockJudicialCalendar, null);

        var currentDate = DateTime.Now;
        var startDate = currentDate.AddMonths(-5);
        var endDate = currentDate.AddMonths(-4);

        var result = await dashboardService.GetMyScheduleAsync(
            mockJudgeId,
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT)
        );

        Assert.Equal(3, result.Payload.Count);

        var activities = result.Payload[0].Activities.ToList();

        Assert.Single(activities);
        Assert.Equal(mockLocationId, activities[0].LocationId);
        Assert.Equal(mockLocationName, activities[0].LocationName);
        Assert.Equal(mockActivityCode, activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, activities[0].IsRemote);
        Assert.True(result.Payload[0].IsWeekend);
        Assert.True(result.Payload[0].ShowCourtList);
        Assert.False(result.Payload[1].ShowCourtList);
        Assert.False(result.Payload[2].ShowCourtList);
        Assert.Null(activities[0].Period);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once());
        mockLocationService
            .Verify(l => l
                .GetLocationShortName(It.IsAny<string>()), Times.Exactly(3));
    }


    [Fact]
    public async Task GetTodaysSchedule_Returns_Success_When_There_Are_No_Schedule_Found()
    {
        var (dashboardService, mockJudicialCalendarClient, _, _) =
            this.SetupDashboardService(new JudicialCalendar(), new ActivityAppearanceResultsCollection());

        var result = await dashboardService.GetTodaysScheduleAsync(_faker.Random.Int());

        Assert.True(result.Succeeded);
        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task GetTodaysSchedule_Returns_Success_With_Scheduled_Files()
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
        var currentDate = DateTime.Now.ToClientTimezone();

        var mockJudicialCalendar = new JudicialCalendar
        {
            Days =
            [
                new JudicialCalendarDay
                {
                    Date = currentDate.ToClientTimezone().ToString(DashboardService.DATE_FORMAT),
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
        };

        var mockCourtList = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items =
            [
                new() {
                    ActivityCd = mockActivityCode,
                    CourtRoomDetails =
                    [
                        new CourtRoomDetail
                        {
                            CourtRoomCd = mockCourtRoom,
                            CasesTarget = mockFileCount,
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
            mockSearchDateClient,
            _) = this.SetupDashboardService(mockJudicialCalendar, mockCourtList);

        var result = await dashboardService.GetTodaysScheduleAsync(mockJudgeId);

        var activities = result.Payload.Activities.ToList();

        Assert.True(result.Succeeded);
        Assert.Equal(2, activities.Count);
        Assert.Equal(mockLocationId1, activities[0].LocationId);
        Assert.Equal(mockLocationName, activities[0].LocationName);
        Assert.Equal(mockActivityCode, activities[0].ActivityCode);
        Assert.Equal(mockActivityDisplayCode, activities[0].ActivityDisplayCode);
        Assert.Equal(mockActivityClassDescription, activities[0].ActivityClassDescription);
        Assert.Equal(mockIsRemote, activities[0].IsRemote);

        Assert.Equal(Period.AM, activities[0].Period);
        Assert.Equal(Period.PM, activities[1].Period);
        Assert.Equal(mockLocationId2, activities[1].LocationId);

        Assert.Equal(mockFileCount, activities[0].FilesCount);
        Assert.Equal(1, activities[0].ContinuationsCount);

        mockJudicialCalendarClient
            .Verify(jcc => jcc
                .ReadCalendarV2Async(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Once());

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
    public async Task GetTodaysSchedule_Returns_Failure_When_Exception_Occured()
    {
        var (dashboardService, _, _, _) = this.SetupDashboardService(null, new ActivityAppearanceResultsCollection());

        var result = await dashboardService.GetTodaysScheduleAsync(_faker.Random.Int());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetCourtCalendarSchedule_Returns_Failure_When_Dates_Are_Invalid()
    {
        var (dashboardService, _, _, _) = this.SetupDashboardService(null, new ActivityAppearanceResultsCollection());

        var result = await dashboardService.GetCourtCalendarScheduleAsync(0, "", "", "");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetCourtCalendarSchedule_Returns_Success_When_Dates_Are_Valid()
    {
        var mockLocationId = _faker.Random.Int();
        var currentDate = DateTime.Now;
        var startDate = currentDate.AddDays(-5);
        var endDate = currentDate.AddDays(5);
        var mockLocationId1 = _faker.Random.Int();
        var mockLocationId2 = _faker.Random.Int();
        var mockLocationName = _faker.Address.City();
        var mockActivityCode = _faker.Lorem.Word();
        var mockActivityDisplayCode = _faker.Lorem.Word();
        var mockActivityClassDescription = _faker.Lorem.Word();
        var mockIsRemote = _faker.Random.Bool();
        var mockCourtRoom = _faker.Address.BuildingNumber();
        var mockFileCount = _faker.Random.Int();

        var presider1 = new
        {
            Id = _faker.Random.Int(),
            Name = _faker.Person.FullName,
            Initials = _faker.Person.FirstName,
            HomeLocationId = _faker.Random.Number(),
            HomeLocationName = _faker.Address.City()
        };

        var presider2 = new
        {
            Id = _faker.Random.Int(),
            Name = _faker.Person.FullName,
            Initials = _faker.Person.FirstName,
            HomeLocationId = _faker.Random.Number(),
            HomeLocationName = _faker.Address.City()
        };

        var (dashboardService, mockJudicialCalendarClient, _, _) =
            this.SetupDashboardService(null, new ActivityAppearanceResultsCollection());

        var mockJCData = new List<JudicialCalendar>
        {
            new()
            {
                Id = presider1.Id,
                Name = presider1.Name,
                RotaInitials = presider1.Initials,
                HomeLocationId = presider1.HomeLocationId,
                HomeLocationName = presider1.HomeLocationName,
                IsPresider = true,
                Days = [
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
            },
            new()
            {
                Id = presider2.Id,
                Name = presider2.Name,
                RotaInitials = presider2.Initials,
                HomeLocationId = presider2.HomeLocationId,
                HomeLocationName = presider2.HomeLocationName,
                IsPresider = true,
                Days = [
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
            }
        };

        mockJudicialCalendarClient
            .Setup(c => c.ReadCalendarsV2Async(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ReadJudicialCalendarsResponse { Calendars = mockJCData });

        var result = await dashboardService.GetCourtCalendarScheduleAsync(
            _faker.Random.Number(),
            mockLocationId.ToString(),
            startDate.ToString(DashboardService.DATE_FORMAT),
            endDate.ToString(DashboardService.DATE_FORMAT));

        Assert.True(result.Succeeded);
        Assert.Single(result.Payload.Days);
        Assert.Equal(2, result.Payload.Presiders.Count);
        Assert.Single(result.Payload.Activities);
    }


    private static DateTime GetRandomWeekend()
    {
        var baseDate = new Faker().Date.Past(2).Date;
        var offset = DayOfWeek.Saturday - baseDate.DayOfWeek;
        var saturday = baseDate.AddDays(offset);
        return saturday.AddDays(new Faker().Random.Int(0, 1));
    }
}
