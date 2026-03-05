using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.Security;
using Moq;
using Scv.Api.Infrastructure;
using Scv.Api.Jobs;
using Scv.Api.Models;
using Scv.Api.Models.Binder;
using Scv.Api.Models.Civil.Detail;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Db.Contants;
using Scv.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tests.api.Fixtures;
using Xunit;

namespace tests.api.Jobs;

[Collection("ServiceFixture")]

public class PopulateJudicialBinderDocumentFieldsJobTests
{
    private readonly Faker _faker;
    private readonly Mock<ILogger<PopulateJudicialBinderDocumentFieldsJob>> _logger;
    private readonly Mock<IBinderService> _mockBinderService;
    private readonly FilesServiceFixture _filesServiceFixture;
    private readonly IAppCache _appCache;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly IMapper _mapper;
    private readonly string _fileId;

    public PopulateJudicialBinderDocumentFieldsJobTests(FilesServiceFixture filesServiceFixture)
    {
        _faker = new Faker();
        _fileId = _faker.Random.Int(10000, 99999).ToString();

        _appCache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        _logger = new Mock<ILogger<PopulateJudicialBinderDocumentFieldsJob>>();
        _mockBinderService = new Mock<IBinderService>();
        _filesServiceFixture = filesServiceFixture;

        _mockConfig = new Mock<IConfiguration>();

        var config = new TypeAdapterConfig();
        _mapper = new Mapper(config);
    }

    #region JobName and CronSchedule Tests

    [Fact]
    public void JobName_Should_Return_PopulateJudicialBinderDocumentFieldsJob()
    {
        Assert.Equal(nameof(PopulateJudicialBinderDocumentFieldsJob), CreateJob().JobName);
    }

    [Fact]
    public void CronSchedule_Should_Return_Never()
    {
        Assert.Equal(Hangfire.Cron.Never(), CreateJob().CronSchedule);
    }

    #endregion

    #region Execute Method Tests

    [Fact]
    public async Task Execute_Should_Log_And_Return_When_NoJudicialBindersFound()
    {
        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([]);

        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Starting PopulateJudicialBinderDocumentFieldsJob")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("No judicial binders found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Search_With_Correct_Criteria()
    {
        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([]);

        var job = CreateJob();
        await job.Execute();

        _mockBinderService.Verify(
            s => s.SearchBinders(It.Is<SearchBindersCriteria>(c =>
                c.LabelKeysExist.Contains(LabelConstants.JUDGE_ID) &&
                c.UpdatedBefore.HasValue &&
                c.Limit == null)),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_ProcessBinder_Successfully()
    {
        var documentId = _faker.Random.AlphaNumeric(10);

        var binder = CreateTestBinder(_fileId, [documentId]);
        var civilDocuments = CreateTestCivilDocuments([documentId]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        _mockBinderService
            .Setup(s => s.InternalUpdateAsync(It.IsAny<BinderDto>()))
            .ReturnsAsync(OperationResult<BinderDto>.Success(binder));

        this.SetupFileServiceMocks();
        var job = CreateJob();
        await job.Execute();

        _mockBinderService.Verify(
            s => s.InternalUpdateAsync(It.Is<BinderDto>(b => b.Id == binder.Id)),
            Times.Once);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Processed binder") && o.ToString()!.Contains("successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Skip_Binder_Without_PhysicalFileId()
    {
        var binder = new BinderDto
        {
            Id = _faker.Random.AlphaNumeric(10),
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, _faker.Random.AlphaNumeric(10) }
            },
            Documents = [new BinderDocumentDto { DocumentId = _faker.Random.AlphaNumeric(10) }]
        };

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("has no physical file ID")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockBinderService.Verify(
            s => s.InternalUpdateAsync(It.IsAny<BinderDto>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_Should_Skip_Binder_Without_Documents()
    {
        var fileId = _faker.Random.AlphaNumeric(10);
        var binder = CreateTestBinder(fileId, []);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("has no documents")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Handle_Update_Failure()
    {
        var documentId = _faker.Random.AlphaNumeric(10);

        var binder = CreateTestBinder(_fileId, [documentId]);
        var civilDocuments = CreateTestCivilDocuments([documentId]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        _mockBinderService
            .Setup(s => s.InternalUpdateAsync(It.IsAny<BinderDto>()))
            .ReturnsAsync(OperationResult<BinderDto>.Failure("Update failed"));

        this.SetupFileServiceMocks();
        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Failed to update binder")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Handle_Processing_Exception()
    {
        var documentId = _faker.Random.AlphaNumeric(10);
        var binder = CreateTestBinder(_fileId, [documentId]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        this.SetupFileServiceMocks();
        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Error processing binder")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Process_Multiple_Binders_With_Mixed_Results()
    {
        var fileId2 = _faker.Random.AlphaNumeric(10);
        var documentId1 = _faker.Random.AlphaNumeric(10);
        var documentId2 = _faker.Random.AlphaNumeric(10);

        var successBinder = CreateTestBinder(_fileId, [documentId1]);
        var skipBinder = CreateTestBinder(fileId2, []);
        var errorBinder = CreateTestBinder(_faker.Random.AlphaNumeric(10), [documentId2]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([successBinder, skipBinder, errorBinder]);

        _mockBinderService
            .Setup(s => s.InternalUpdateAsync(It.Is<BinderDto>(b => b.Id == successBinder.Id)))
            .ReturnsAsync(OperationResult<BinderDto>.Success(successBinder));

        this.SetupFileServiceMocks();
        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) =>
                    o.ToString()!.Contains("PopulateJudicialBinderDocumentFieldsJob completed") &&
                    o.ToString()!.Contains("Total: 3") &&
                    o.ToString()!.Contains("Processed: 1") &&
                    o.ToString()!.Contains("Skipped: 1") &&
                    o.ToString()!.Contains("Errors: 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Log_Fatal_Error_And_Rethrow()
    {
        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ThrowsAsync(new Exception("Fatal error"));

        this.SetupFileServiceMocks();
        var job = CreateJob();
        var exception = await Assert.ThrowsAsync<Exception>(() => job.Execute());

        Assert.Equal("Fatal error", exception.Message);

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Fatal error in PopulateJudicialBinderDocumentFieldsJob")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Log_Progress_For_Each_Binder()
    {
        var documentId = _faker.Random.AlphaNumeric(10);
        var binder = CreateTestBinder(_fileId, [documentId]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        _mockBinderService
            .Setup(s => s.InternalUpdateAsync(It.IsAny<BinderDto>()))
            .ReturnsAsync(OperationResult<BinderDto>.Success(binder));

        this.SetupFileServiceMocks();
        var job = CreateJob();
        await job.Execute();

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Progress: 1/1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_Map_CivilDocuments_To_BinderDocuments()
    {
        var documentId = _faker.Random.AlphaNumeric(10);
        var binder = CreateTestBinder(_fileId, [documentId]);
        var civilDocuments = CreateTestCivilDocuments([documentId]);

        _mockBinderService
            .Setup(s => s.SearchBinders(It.IsAny<SearchBindersCriteria>()))
            .ReturnsAsync([binder]);

        BinderDto capturedBinder = null;
        _mockBinderService
            .Setup(s => s.InternalUpdateAsync(It.IsAny<BinderDto>()))
            .Callback<BinderDto>(b => capturedBinder = b)
            .ReturnsAsync(OperationResult<BinderDto>.Success(binder));

        var fileDetail = new CivilFileDetailResponse
        {
            CourtClassCd = CivilFileDetailResponseCourtClassCd.F,
            Appearance = [],
            Document =
            [
                new CvfcDocument3
                {
                    CivilDocumentId = documentId,
                    DocumentTypeCd = DocumentCategory.BAIL,
                    Issue = []
                }
            ],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent
        {
            CivilFile =
            [
                new CvfcCivilFile
                {
                    PhysicalFileID = _fileId
                }
            ]
        };

        this.SetupFileServiceMocks(fileDetail, fileContent);
        var job = CreateJob();
        
        await job.Execute();

        Assert.NotNull(capturedBinder);
        Assert.NotNull(capturedBinder.Documents);
        Assert.NotEmpty(capturedBinder.Documents);
    }

    #endregion

    #region Private Helper Methods

    private PopulateJudicialBinderDocumentFieldsJob CreateJob() =>
        new(
            _mockConfig.Object,
            _appCache,
            _mapper,
            _logger.Object,
            _mockBinderService.Object,
            _filesServiceFixture.MockFilesService.Object
        );

    private void SetupFileServiceMocks(
        CivilFileDetailResponse fileDetail = null,
        CivilFileContent fileContent = null,
        string documentCategory = null)
    {
        fileDetail ??= new CivilFileDetailResponse
        {
            CourtClassCd = CivilFileDetailResponseCourtClassCd.F,
            Appearance = [],
            Document =
            [
                new CvfcDocument3
                {
                    CivilDocumentId = "doc-1",
                    DocumentTypeCd = DocumentCategory.BAIL,
                    Issue = []
                }
            ],
            ReferenceDocument = []
        };

        fileContent ??= new CivilFileContent
        {
            CivilFile =
            [
                new CvfcCivilFile
                {
                    PhysicalFileID = _fileId
                }
            ]
        };
        
        documentCategory ??= DocumentCategory.BAIL;

        _filesServiceFixture.MockFileServicesClient
            .Setup(s => s.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileDetail);

        _filesServiceFixture.MockFileServicesClient
            .Setup(s => s.FilesCivilFilecontentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileContent);

        _filesServiceFixture.MockLookupService
            .Setup(s => s.GetDocumentCategory(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(documentCategory);

        _filesServiceFixture.MockLookupService
            .Setup(s => s.GetDocumentDescriptionAsync(It.IsAny<string>()))
            .ReturnsAsync("Test Document Description");
    }

    private BinderDto CreateTestBinder(string fileId, List<string> documentIds)
    {
        return new BinderDto
        {
            Id = _faker.Random.AlphaNumeric(10),
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, _faker.Random.AlphaNumeric(10) },
                { LabelConstants.PHYSICAL_FILE_ID, fileId }
            },
            Documents = documentIds.Select(id => new BinderDocumentDto
            {
                DocumentId = id,
                Order = 0
            }).ToList()
        };
    }

    private List<CivilDocument> CreateTestCivilDocuments(List<string> documentIds)
    {
        return documentIds.Select(id => new CivilDocument
        {
            CivilDocumentId = id,
            ImageId = _faker.Random.AlphaNumeric(10),
            DocumentTypeDescription = _faker.Lorem.Sentence(),
            DocumentTypeCd = "LITIGANT"
        }).ToList();
    }

    #endregion
}
