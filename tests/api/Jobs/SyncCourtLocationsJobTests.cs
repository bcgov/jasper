using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents.Parsers;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Scv.Core.Infrastructure;
using Scv.Models.CourtLocation;
using Xunit;
using GraphModel = Microsoft.Graph.Models;
using ParserModel = Scv.Api.Documents.Parsers.Models;

namespace tests.api.Jobs;

public class SyncCourtLocationsJobTests
{
    private const string Mailbox = "service@example.com";
    private const string Subject = "Court Locations";
    private const string Filename = "court-locations.xlsx";
    private const string SupportAccount = "example@support.com";

    private const string CourtLocationsSheet = "Court Locations";
    private const string AdultProbationOfficesSheet = "Adult Probation Offices";
    private const string YouthProbationOfficesSheet = "Youth Probation Offices";

    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IExcelParser> _mockExcelParser;
    private readonly Mock<IExcelWorkbook> _mockWorkbook;
    private readonly Mock<ICourtLocationService> _mockClService;
    private readonly Mock<ILogger<SyncCourtLocationsJob>> _mockLogger;
    private readonly Mock<IAntiVirusService> _mockAvService;
    private readonly SyncCourtLocationsJob _job;

    public SyncCourtLocationsJobTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockExcelParser = new Mock<IExcelParser>();
        _mockWorkbook = new Mock<IExcelWorkbook>();
        _mockClService = new Mock<ICourtLocationService>();
        _mockLogger = new Mock<ILogger<SyncCourtLocationsJob>>();
        _mockAvService = new Mock<IAntiVirusService>();

        var cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new CourtLocationMapping());
        var mapper = new Mapper(config);

        SetupConfig();

        _mockExcelParser.Setup(p => p.Open(It.IsAny<Stream>())).Returns(_mockWorkbook.Object);

        _job = new SyncCourtLocationsJob(
            _mockConfig.Object,
            cache,
            mapper,
            _mockLogger.Object,
            _mockEmailService.Object,
            _mockExcelParser.Object,
            _mockClService.Object,
            _mockAvService.Object);
    }

    private void SetupConfig()
    {
        SetupConfigValue("AZURE:SERVICE_ACCOUNT", Mailbox);
        SetupConfigValue("COURT_LOCATIONS:SUBJECT", Subject);
        SetupConfigValue("COURT_LOCATIONS:ATTACHMENT_NAME", Filename);
        SetupConfigValue("SUPPORT_ACCOUNT", SupportAccount);
    }

    private void SetupConfigValue(string key, string value)
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns(value);
        _mockConfig.Setup(c => c.GetSection(key)).Returns(section.Object);
    }

    private void SetupValidEmailWithAttachment()
    {
        _mockEmailService
            .Setup(s => s.GetFilteredEmailsAsync(Mailbox, Subject, It.IsAny<string>(), true))
            .ReturnsAsync([new GraphModel.Message { Id = "msg-1" }]);

        _mockEmailService
            .Setup(s => s.GetAttachmentsAsStreamsAsync(Mailbox, "msg-1", Filename))
            .ReturnsAsync(new Dictionary<string, MemoryStream>
            {
                [Filename] = new MemoryStream([1, 2, 3])
            });
    }

    private void SetupWorkbookSheets(
        List<ParserModel.CourtLocation> courtLocations,
        List<ParserModel.AdultProbationOffice> adultOffices,
        List<ParserModel.YouthProbationOffice> youthOffices)
    {
        _mockWorkbook.Setup(w => w.GetSheet<ParserModel.CourtLocation>(CourtLocationsSheet))
            .Returns(courtLocations);
        _mockWorkbook.Setup(w => w.GetSheet<ParserModel.AdultProbationOffice>(AdultProbationOfficesSheet))
            .Returns(adultOffices);
        _mockWorkbook.Setup(w => w.GetSheet<ParserModel.YouthProbationOffice>(YouthProbationOfficesSheet))
            .Returns(youthOffices);
    }

    private void SetupAntiVirusScanPass()
    {
        _mockAvService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((true, null));
    }

    private static List<ParserModel.CourtLocation> DefaultCourtLocations() =>
    [
        new()
        {
            Name = "Vancouver",
            Code = "4801",
            Staffed = "Staffed",
            AdultProbationOffice = "Vancouver APO",
            YouthProbationOffice = "Vancouver YPO"
        }
    ];

    private static List<ParserModel.AdultProbationOffice> DefaultAdultOffices() =>
        [new() { Name = "Vancouver APO", Address1 = "123 Main St" }];

    private static List<ParserModel.YouthProbationOffice> DefaultYouthOffices() =>
        [new() { Name = "Vancouver YPO", Address1 = "456 Youth Ave" }];

    [Fact]
    public void JobName_ReturnsExpectedName()
    {
        Assert.Equal(nameof(SyncCourtLocationsJob), _job.JobName);
    }

    [Fact]
    public async Task Execute_CompletesSuccessfully_WhenAllDataValid()
    {
        SetupValidEmailWithAttachment();
        SetupWorkbookSheets(DefaultCourtLocations(), DefaultAdultOffices(), DefaultYouthOffices());
        SetupAntiVirusScanPass();
        _mockClService
            .Setup(s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()))
            .ReturnsAsync(OperationResult.Success());

        await _job.Execute();

        _mockClService.Verify(
            s => s.ReplaceCourtLocationsAsync(It.Is<CourtLocationDto[]>(dtos => dtos.Length == 1)),
            Times.Once());
        _mockWorkbook.Verify(w => w.Dispose(), Times.Once());
    }

    [Fact]
    public async Task Execute_LinksProbationOffices_ByName()
    {
        SetupValidEmailWithAttachment();
        SetupWorkbookSheets(DefaultCourtLocations(), DefaultAdultOffices(), DefaultYouthOffices());
        SetupAntiVirusScanPass();

        CourtLocationDto[] captured = null;
        _mockClService
            .Setup(s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()))
            .Callback<CourtLocationDto[]>(dtos => captured = dtos)
            .ReturnsAsync(OperationResult.Success());

        await _job.Execute();

        Assert.NotNull(captured);
        var dto = Assert.Single(captured);
        Assert.Equal("4801", dto.Code);
        Assert.True(dto.IsStaffed);
        Assert.NotNull(dto.AdultProbationOffice);
        Assert.Equal("Vancouver APO", dto.AdultProbationOffice.Name);
        Assert.NotNull(dto.YouthProbationOffice);
        Assert.Equal("Vancouver YPO", dto.YouthProbationOffice.Name);
    }

    [Fact]
    public async Task Execute_ReturnsSuccessfully_WhenNoEmailFound()
    {
        _mockEmailService
            .Setup(s => s.GetFilteredEmailsAsync(Mailbox, Subject, null, true))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockEmailService.Verify(
            s => s.GetAttachmentsAsStreamsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never());
        _mockClService.Verify(
            s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()),
            Times.Never());
    }

    [Fact]
    public async Task Execute_Throws_WhenAttachmentIsNotClean()
    {
        SetupValidEmailWithAttachment();
        SetupWorkbookSheets(DefaultCourtLocations(), [], DefaultYouthOffices());
        _mockAvService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((false, "Infected"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute());

        _mockClService.Verify(
            s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()),
            Times.Never());
    }

    [Fact]
    public async Task Execute_Throws_WhenASheetIsEmpty()
    {
        SetupValidEmailWithAttachment();
        SetupWorkbookSheets(DefaultCourtLocations(), [], DefaultYouthOffices());
        SetupAntiVirusScanPass();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute());

        _mockClService.Verify(
            s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()),
            Times.Never());
        _mockWorkbook.Verify(w => w.Dispose(), Times.Once());
    }

    [Fact]
    public async Task Execute_Throws_WhenReplaceFails()
    {
        SetupValidEmailWithAttachment();
        SetupWorkbookSheets(DefaultCourtLocations(), DefaultAdultOffices(), DefaultYouthOffices());
        _mockClService
            .Setup(s => s.ReplaceCourtLocationsAsync(It.IsAny<CourtLocationDto[]>()))
            .ReturnsAsync(OperationResult.Failure("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute());
    }
}
