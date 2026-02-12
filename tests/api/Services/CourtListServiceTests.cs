using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.ReportServices;
using PCSSCommon.Clients.SearchDateServices;
using PCSSCommon.Models;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.CourtList;
using Scv.Api.Services;
using Xunit;
using static PCSSCommon.Models.ActivityClassUsage;
using JasperRole = Scv.Db.Models.Role;

namespace tests.api.Services;

public class CourtListServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly Mock<IPcssConfigService> _mockExternalConfigClient;

    public CourtListServiceTests()
    {
        _faker = new Faker();

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();

        var mockFileExpirySection = new Mock<IConfigurationSection>();
        mockFileExpirySection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());

        var mockRefreshHoursSection = new Mock<IConfigurationSection>();
        mockRefreshHoursSection.Setup(s => s.Value).Returns("12");

        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockFileExpirySection.Object);
        var mockSection = Mock.Of<IConfigurationSection>(s => s.Value == "TESTAPP");
        _mockConfig.Setup(c => c.GetSection("Request:ApplicationCd")).Returns(mockSection);

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        _mapper = new Mapper(config);

        // IExternalConfigService setup
        _mockExternalConfigClient = new Mock<IPcssConfigService>();
    }

    private (
        CourtListService courtListService,
        Mock<ReportServicesClient> mockReportClient,
        Mock<FileServicesClient> mockFileClient,
        Mock<SearchDateClient> mockSearchDateClient
        ) SetupCourtListService(string role = JasperRole.ADMIN)
    {
        // Setup Service Clients
        var mockFileClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, this.HttpClient);
        var mockReportClient = new Mock<ReportServicesClient>(MockBehavior.Strict, this.HttpClient);

        // Setup cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Setup ClaimsPrincipal
        var identity = new ClaimsIdentity(
            [
                new(CustomClaimTypes.ApplicationCode, "TESTAPP"),
                new(CustomClaimTypes.JcAgencyCode, "TESTAGENCY"),
                new(CustomClaimTypes.JcParticipantId, "TESTPART"),
                new(CustomClaimTypes.Role, role)
            ],
            "mock");

        var courtListService = new CourtListService(
            _mockConfig.Object,
            new Mock<ILogger<CourtListService>>().Object,
            mockFileClient.Object,
            _mapper,
            null,
            null,
            mockSearchDateClient.Object,
            mockReportClient.Object,
            cachingService,
            new ClaimsPrincipal(identity),
            _mockExternalConfigClient.Object);

        return (
            courtListService,
            mockReportClient,
            mockFileClient,
            mockSearchDateClient
        );
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnStream()
    {
        var (courtListService, mockReportClient, _, _) = SetupCourtListService();
        var fakeContentDisposition = $"inline; filename={Path.GetRandomFileName()}";

        mockReportClient
            .Setup(r => r.GetCourtListReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), fakeContentDisposition));

        var (stream, contentDisposition) = await courtListService.GenerateReportAsync(new CourtListReportRequest());

        Assert.NotNull(stream);
        Assert.Equal(fakeContentDisposition, contentDisposition);
        mockReportClient
            .Verify(r => r
                .GetCourtListReportAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());
    }

    [Fact]
    public async Task GetJudgeCourtListAppearances_ShouldReturnApiResponse_When_Items_IsNull()
    {
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = null
        };
        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        var result = await courtListService.GetJudgeCourtListAppearances(_faker.Random.Int(), DateTime.Now.ToClientTimezone());

        Assert.Equal(mockResult, result);
    }

    [Fact]
    public async Task GetJudgeCourtListAppearances_ShouldReturnDataRelatedToJudgeId()
    {
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();

        var mockJudgeId = _faker.Random.Int();
        var mockAmPm = _faker.Lorem.Word();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items =
            [
                new ActivityClassUsage.ActivityAppearanceResults
                {
                    Appearances =
                    [
                        new PcssCounsel.ActivityAppearanceDetail
                        {
                            AppearanceTm = mockAmPm
                        },
                        new PcssCounsel.ActivityAppearanceDetail
                        {
                            AppearanceTm = mockAmPm
                        },
                        new PcssCounsel.ActivityAppearanceDetail
                        {
                            AppearanceTm = _faker.Lorem.Word()
                        }
                    ],
                    CourtActivityDetails =
                    [
                        new ActivityClassUsage.CourtActivityDetail
                        {
                            CourtSittingCd = mockAmPm
                        },
                        new ActivityClassUsage.CourtActivityDetail
                        {
                            CourtSittingCd = _faker.Lorem.Word()
                        },
                        new ActivityClassUsage.CourtActivityDetail
                        {
                            CourtSittingCd = _faker.Lorem.Word()
                        },
                    ],
                    CourtRoomDetails =
                    [
                        new ActivityClassUsage.CourtRoomDetail
                        {
                            AdjudicatorDetails =
                            [
                                new ActivityClassUsage.AdjudicatorDetail
                                {
                                    AdjudicatorId = mockJudgeId,
                                    AmPm = mockAmPm
                                },
                                new ActivityClassUsage.AdjudicatorDetail
                                {
                                    AdjudicatorId = _faker.Random.Int()
                                },
                                 new ActivityClassUsage.AdjudicatorDetail
                                {
                                    AdjudicatorId = _faker.Random.Int()
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        var result = await courtListService.GetJudgeCourtListAppearances(mockJudgeId, DateTime.Now.ToClientTimezone());

        var first = result.Items.First();
        Assert.Equal(2, first.Appearances.Count);
        Assert.Single(first.CourtActivityDetails);
        Assert.Single(first.CourtRoomDetails.SelectMany(crd => crd.AdjudicatorDetails));
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldReturnFailure_WhenProceedingDateIsOutsideLookAheadWindow()
    {
        // Arrange
        var (courtListService, _, _, _) = SetupCourtListService(JasperRole.RAJ);
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(lookAheadWindow + 1);
        var judgeId = _faker.Random.Int();

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, null, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("The court list is not available at this time.", result.Errors[0]);
        _mockExternalConfigClient.Verify(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldReturnSuccess_WhenProceedingDateIsWithinLookAheadWindow()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(lookAheadWindow - 1);
        var judgeId = _faker.Random.Int();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = []
        };

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, null, null);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(mockResult, result.Payload);
        _mockExternalConfigClient.Verify(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()), Times.Once);
        mockSearchDateClient.Verify(s => s.GetJudgeCourtListAppearancesAsync(judgeId, proceedingDate.ToString("dd-MMM-yyyy")), Times.Once);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldCallGetJudgeCourtListAppearances_WhenAgencyIdAndRoomCodeAreNull()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(1);
        var judgeId = _faker.Random.Int();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = []
        };

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, null, null);

        // Assert
        Assert.True(result.Succeeded);
        mockSearchDateClient.Verify(s => s.GetJudgeCourtListAppearancesAsync(judgeId, proceedingDate.ToString("dd-MMM-yyyy")), Times.Once);
        mockSearchDateClient.Verify(s => s.GetCourtListAppearancesAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldCallGetCourtListAppearances_WhenAgencyIdAndRoomCodeAreProvided()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(1);
        var judgeId = _faker.Random.Int();
        var agencyId = _faker.Random.Number().ToString();
        var roomCode = _faker.Lorem.Word();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = []
        };

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool?>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, agencyId, roomCode);

        // Assert
        Assert.True(result.Succeeded);
        mockSearchDateClient.Verify(s => s.GetCourtListAppearancesAsync(
            int.Parse(agencyId),
            proceedingDate.ToString("dd-MMM-yyyy"),
            judgeId,
            roomCode,
            null), Times.Once);
        mockSearchDateClient.Verify(s => s.GetJudgeCourtListAppearancesAsync(
            It.IsAny<int>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(1);
        var judgeId = _faker.Random.Int();
        var agencyId = _faker.Random.Number().ToString();
        var roomCode = _faker.Lorem.Word();
        var exceptionMessage = "Test exception message";

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<bool?>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, agencyId, roomCode);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Something went wrong when retrieving court list appearances.", result.Errors[0]);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldReturnSuccess_WhenProceedingDateIsToday()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var judgeId = _faker.Random.Int();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = []
        };

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await courtListService.GetCourtListAsync(today, judgeId, null, null);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(mockResult, result.Payload);
    }

    [Fact]
    public async Task GetCourtListAsync_ShouldReturnSuccess_WhenProceedingDateIsAtExactLookAheadWindowBoundary()
    {
        // Arrange
        var (courtListService, _, _, mockSearchDateClient) = SetupCourtListService();
        var lookAheadWindow = 30;
        var today = DateTime.Now.ToClientTimezone().Date;
        var proceedingDate = today.AddDays(lookAheadWindow);
        var judgeId = _faker.Random.Int();
        var mockResult = new ActivityClassUsage.ActivityAppearanceResultsCollection
        {
            Items = []
        };

        _mockExternalConfigClient
            .Setup(e => e.GetLookAheadWindowAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
            .ReturnsAsync(lookAheadWindow);

        mockSearchDateClient
            .Setup(s => s.GetJudgeCourtListAppearancesAsync(
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await courtListService.GetCourtListAsync(proceedingDate, judgeId, null, null);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(mockResult, result.Payload);
    }
}