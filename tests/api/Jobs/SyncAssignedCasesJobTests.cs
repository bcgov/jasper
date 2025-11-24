using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.JudicialCalendarServices;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Jobs;
using Scv.Api.Models;
using Scv.Api.Services;
using tests.api.Fixtures;
using Xunit;
using GraphModel = Microsoft.Graph.Models;

namespace tests.api.Jobs;

[Collection("ServiceFixture")]
public class SyncAssignedCasesJobTests
{
    private readonly CourtListServiceFixture _courtListServiceFixture;
    private readonly FilesServiceFixture _filesServiceFixture;
    private readonly LocationServiceFixture _locationServiceFixture;

    private readonly Faker _faker;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ICsvParser> _mockCsvParser;
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<ILambdaInvokerService> _mockLambdaInvokerService;

    private readonly Mock<JudicialCalendarServicesClient> _mockJcServiceClient;
    private readonly Mock<ILogger<SyncAssignedCasesJob>> _mockLogger;
    private readonly IAppCache _cache;
    private readonly IMapper _mapper;
    private readonly SyncAssignedCasesJob _job;

    public SyncAssignedCasesJobTests(
        CourtListServiceFixture courtListServiceFixture,
        FilesServiceFixture filesServiceFixture,
        LocationServiceFixture locationServiceFixture)
    {
        _courtListServiceFixture = courtListServiceFixture;
        _filesServiceFixture = filesServiceFixture;
        _locationServiceFixture = locationServiceFixture;

        _faker = new Faker();

        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockCsvParser = new Mock<ICsvParser>();
        _mockDashboardService = new Mock<IDashboardService>();
        _mockCaseService = new Mock<ICaseService>();
        _mockLambdaInvokerService = new Mock<ILambdaInvokerService>();

        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri(_faker.Internet.Url())
        };

        _mockJcServiceClient = new Mock<JudicialCalendarServicesClient>(MockBehavior.Strict, httpClient);
        _mockLogger = new Mock<ILogger<SyncAssignedCasesJob>>();

        _cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new CaseMapping());
        _mapper = new Mapper(config);

        SetupMockConfiguration();

        _job = new SyncAssignedCasesJob(
            _mockConfig.Object,
            _cache,
            _mapper,
            _mockLogger.Object,
            _mockEmailService.Object,
            _mockCsvParser.Object,
            _mockDashboardService.Object,
            _mockCaseService.Object,
            _courtListServiceFixture.MockCourtListService.Object,
            _mockJcServiceClient.Object,
            _mockLambdaInvokerService.Object);
    }

    private void SetupMockConfiguration()
    {
        var mockScheduleSection = new Mock<IConfigurationSection>();
        mockScheduleSection.Setup(s => s.Value).Returns("0 7 * * *");
        _mockConfig.Setup(c => c.GetSection("JOBS:SYNC_ASSIGNED_CASES_SCHEDULE"))
            .Returns(mockScheduleSection.Object);

        var mockMailboxSection = new Mock<IConfigurationSection>();
        mockMailboxSection.Setup(s => s.Value).Returns("test@example.com");
        _mockConfig.Setup(c => c.GetSection("AZURE:SERVICE_ACCOUNT"))
            .Returns(mockMailboxSection.Object);

        var mockSubjectSection = new Mock<IConfigurationSection>();
        mockSubjectSection.Setup(s => s.Value).Returns("Reserved Judgements");
        _mockConfig.Setup(c => c.GetSection("RESERVED_JUDGEMENTS:SUBJECT"))
            .Returns(mockSubjectSection.Object);

        var mockFilenameSection = new Mock<IConfigurationSection>();
        mockFilenameSection.Setup(s => s.Value).Returns("judgements.csv");
        _mockConfig.Setup(c => c.GetSection("RESERVED_JUDGEMENTS:ATTACHMENT_NAME"))
            .Returns(mockFilenameSection.Object);

        var mockFromEmailSection = new Mock<IConfigurationSection>();
        mockFromEmailSection.Setup(s => s.Value).Returns("sender@example.com");
        _mockConfig.Setup(c => c.GetSection("RESERVED_JUDGEMENTS:SENDER"))
            .Returns(mockFromEmailSection.Object);

        var mockLambdaNameSection = new Mock<IConfigurationSection>();
        mockLambdaNameSection.Setup(s => s.Value).Returns((string)null);
        _mockConfig.Setup(c => c.GetSection("AWS_GET_ASSIGNED_CASES_LAMBDA_NAME"))
            .Returns(mockLambdaNameSection.Object);

        var mockLambdaTimeoutSection = new Mock<IConfigurationSection>();
        mockLambdaTimeoutSection.Setup(s => s.Value).Returns("10");
        _mockConfig.Setup(c => c.GetSection("AWS_GET_ASSIGNED_CASES_LAMBDA_TIMEOUT_MINUTES"))
            .Returns(mockLambdaTimeoutSection.Object);
    }

    [Fact]
    public async Task Execute_DeletesExistingCases_BeforeProcessing()
    {
        var existingCases = new List<CaseDto>
        {
            new() { Id = _faker.Random.AlphaNumeric(24) },
            new() { Id = _faker.Random.AlphaNumeric(24) }
        };

        _mockCaseService.Setup(s => s.GetAllAsync()).ReturnsAsync(existingCases);
        _mockCaseService.Setup(s => s.DeleteRangeAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult.Success);

        _mockEmailService
            .Setup(s => s.GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockCaseService.Verify(s => s.DeleteRangeAsync(It.Is<List<string>>(ids =>
            ids.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task Execute_ProcessesReservedJudgements_WhenEmailFound()
    {
        SetupBasicMocks();

        var csvContent = "Header1,Header2\nValue1,Value2";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var attachments = new Dictionary<string, MemoryStream> { { "judgements.csv", stream } };

        var messages = new List<GraphModel.Message>
        {
            new() { Id = _faker.Random.AlphaNumeric(10) }
        };

        var csvData = new List<CsvReservedJudgement>
        {
            new()
            {
                CourtFileNumber = _faker.Random.AlphaNumeric(10),
                AdjudicatorLastNameFirstName = $"{_faker.Name.LastName()}, {_faker.Name.FirstName()}",
                AppearanceDate = _faker.Date.Future(),
                FacilityCode = _faker.Address.CountryCode()
            }
        };

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(messages);

        _mockEmailService.Setup(s => s.GetAttachmentsAsStreamsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(attachments);

        _mockCsvParser.Setup(p => p.Parse<CsvReservedJudgement>(It.IsAny<MemoryStream>(), "\t"))
            .Returns(csvData);

        _mockDashboardService.Setup(s => s.GetJudges())
            .ReturnsAsync([]);

        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        _mockJcServiceClient.Setup(c => c
            .GetUpcomingSeizedAssignedCasesAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockCsvParser.Verify(p => p.Parse<CsvReservedJudgement>(It.IsAny<MemoryStream>(), "\t"), Times.Once);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenNoEmailFound()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        _mockJcServiceClient.Setup(c => c
            .GetUpcomingSeizedAssignedCasesAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("No email found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ProcessesScheduledCases_WhenDataAvailable()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        var scheduledCases = new List<PCSSCommon.Models.Case>
        {
            new()
            {
                NextApprId = _faker.Random.Double(1, 9999),
                LastApprDt = "24-Oct-2025",
                NextApprDt = "25-Oct-2025",
                CourtClassCd = "S",
                FileNumberTxt = _faker.Random.AlphaNumeric(10),
                NextApprReason = "DEC",
                StyleOfCause = $"{_faker.Name.LastName()} vs {_faker.Name.LastName()}",
                Participants = []
            }
        };

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(scheduledCases);

        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        await _job.Execute();

        _mockCaseService.Verify(s => s.AddRangeAsync(It.Is<List<CaseDto>>(list =>
            list.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task Execute_LogsInfo_WhenNoScheduledCasesFound()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("No Scheduled Cases found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_GeneratesStyleOfCause_WhenMissingAndParticipantsExist()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        var participant1 = new PCSSCommon.Models.Participant { FullName = _faker.Name.FullName() };
        var participant2 = new PCSSCommon.Models.Participant { FullName = _faker.Name.FullName() };

        var scheduledCases = new List<PCSSCommon.Models.Case>
        {
            new()
            {
                NextApprId = _faker.Random.Double(1, 9999),
                LastApprDt = "24-Oct-2025",
                NextApprDt = "25-Oct-2025",
                CourtClassCd = "S",
                FileNumberTxt = _faker.Random.AlphaNumeric(10),
                NextApprReason = "CNT",
                StyleOfCause = null,
                Participants = [participant1, participant2]
            }
        };

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(scheduledCases);

        List<CaseDto> capturedCases = null;
        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .Callback<List<CaseDto>>(cases => capturedCases = cases)
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        await _job.Execute();

        Assert.NotNull(capturedCases);
        Assert.Single(capturedCases);
        Assert.Contains("and 1 other(s)", capturedCases[0].StyleOfCause);
    }

    [Fact]
    public async Task Execute_UsesSingleParticipantName_WhenOnlyOneParticipant()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        var participantName = _faker.Name.FullName();
        var participant = new PCSSCommon.Models.Participant { FullName = participantName };

        var scheduledCases = new List<PCSSCommon.Models.Case>
        {
            new()
            {
                NextApprId = _faker.Random.Double(1, 9999),
                LastApprDt = "24-Oct-2025",
                NextApprDt = "25-Oct-2025",
                CourtClassCd = "S",
                FileNumberTxt = _faker.Random.AlphaNumeric(10),
                NextApprReason = CaseService.ADDTL_CNT_TIME_APPR_REASON_CD,
                StyleOfCause = null,
                Participants = [participant]
            }
        };

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(scheduledCases);

        List<CaseDto> capturedCases = null;
        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .Callback<List<CaseDto>>(cases => capturedCases = cases)
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        await _job.Execute();

        Assert.NotNull(capturedCases);
        Assert.Single(capturedCases);
        Assert.Equal(participantName, capturedCases[0].StyleOfCause);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenNoParticipantsAndNoStyleOfCause()
    {
        SetupBasicMocks();

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        var scheduledCases = new List<PCSSCommon.Models.Case>
        {
            new()
            {
                NextApprId = _faker.Random.Double(1, 9999),
                LastApprDt = "24-Oct-2025",
                NextApprDt = "25-Oct-2025",
                CourtClassCd = "S",
                FileNumberTxt = "TEST123",
                NextApprReason = CaseService.DECISION_APPR_REASON_CD,
                StyleOfCause = null,
                Participants = []
            }
        };

        _mockJcServiceClient.Setup(c => c.GetUpcomingSeizedAssignedCasesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(scheduledCases);

        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        await _job.Execute();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("No participants found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_ThrowsException_WhenErrorOccurs()
    {
        SetupBasicMocks();

        _mockCaseService.Setup(s => s.DeleteRangeAsync(It.IsAny<List<string>>()))
            .ThrowsAsync(new Exception("Database error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute());
    }

    [Fact]
    public void JobName_ReturnsCorrectName()
    {
        Assert.Equal(nameof(SyncAssignedCasesJob), _job.JobName);
    }

    [Fact]
    public void CronSchedule_ReturnsConfiguredSchedule()
    {
        var schedule = _job.CronSchedule;

        Assert.Equal("0 7 * * *", schedule);
    }

    [Fact]
    public async Task Execute_UsesLambdaInvoker_WhenLambdaNameConfigured()
    {
        SetupBasicMocks();

        var mockLambdaNameSection = new Mock<IConfigurationSection>();
        mockLambdaNameSection.Setup(s => s.Value).Returns("test-lambda-function");
        _mockConfig.Setup(c => c.GetSection("AWS_GET_ASSIGNED_CASES_LAMBDA_NAME"))
            .Returns(mockLambdaNameSection.Object);

        _mockEmailService.Setup(s => s
            .GetFilteredEmailsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync([]);

        var scheduledCases = new List<PCSSCommon.Models.Case>
        {
            new()
            {
                NextApprId = _faker.Random.Double(1, 9999),
                LastApprDt = "24-Oct-2025",
                NextApprDt = "25-Oct-2025",
                CourtClassCd = "S",
                FileNumberTxt = _faker.Random.AlphaNumeric(10),
                NextApprReason = "DEC",
                StyleOfCause = $"{_faker.Name.LastName()} vs {_faker.Name.LastName()}",
                Participants = []
            }
        };

        var lambdaResponse = new AssignedCaseResponse
        {
            Success = true,
            Data = scheduledCases
        };

        _mockLambdaInvokerService.Setup(s => s
            .InvokeAsync<object, AssignedCaseResponse>(
                It.IsAny<object>(),
                "test-lambda-function",
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(lambdaResponse);

        _mockCaseService.Setup(s => s.AddRangeAsync(It.IsAny<List<CaseDto>>()))
            .ReturnsAsync(OperationResult<CaseDto>.Success(new()));

        await _job.Execute();

        _mockLambdaInvokerService.Verify(s => s
            .InvokeAsync<object, AssignedCaseResponse>(
                It.IsAny<object>(),
                "test-lambda-function",
                It.IsAny<TimeSpan?>()),
            Times.Once);

        _mockJcServiceClient.Verify(c => c
            .GetUpcomingSeizedAssignedCasesAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private void SetupBasicMocks()
    {
        _mockCaseService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);
        _mockCaseService.Setup(s => s.DeleteRangeAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(OperationResult.Success());
    }
}