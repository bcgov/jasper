using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Api.Services;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Binder;
using Xunit;

namespace tests.api.Services;
public class BinderServiceTests
{
    private readonly Bogus.Faker _faker;
    private readonly Mock<IRepositoryBase<Binder>> _mockBinderRepo;
    private readonly Mock<IBinderFactory> _mockBinderFactory;
    private readonly Mock<ILogger<BinderService>> _mockLogger;
    private readonly IMapper _mapper;
    private readonly BinderService _binderService;

    public BinderServiceTests()
    {
        _faker = new Bogus.Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        _mapper = new Mapper(config);

        // ILogger setup
        _mockLogger = new Mock<ILogger<BinderService>>();

        _mockBinderRepo = new Mock<IRepositoryBase<Binder>>();
        _mockBinderFactory = new Mock<IBinderFactory>();

        _binderService = new BinderService(
            cachingService,
            _mapper,
            _mockLogger.Object,
            _mockBinderRepo.Object,
            _mockBinderFactory.Object,
            new Mock<IDocumentMerger>().Object);
    }

    #region SearchBinders Tests

    [Fact]
    public async Task SearchBinders_WithNullCriteria_ShouldUseDefaultCriteria()
    {
        var expectedBinders = CreateSampleBinders(3);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(null);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        _mockBinderRepo.Verify(r => r.FindAsync(
            CollectionNameConstants.BINDERS,
            It.IsAny<FilterDefinition<Binder>>(),
            null,
            100,
            null), Times.Once);
    }

    [Fact]
    public async Task SearchBinders_WithLabelKeysExist_ShouldFilterCorrectly()
    {
        var criteria = new SearchBindersCriteria
        {
            LabelKeysExist = [LabelConstants.JUDGE_ID]
        };

        var expectedBinders = CreateSampleBinders(2, includeJudgeId: true);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.Labels.ContainsKey(LabelConstants.JUDGE_ID)));
    }

    [Fact]
    public async Task SearchBinders_WithLabelMatches_ShouldFilterCorrectly()
    {
        var criteria = new SearchBindersCriteria
        {
            LabelMatches = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "FILE-123" }
            }
        };

        var expectedBinders = CreateSampleBinders(1);
        expectedBinders[0].Labels[LabelConstants.PHYSICAL_FILE_ID] = "FILE-123";

        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("FILE-123", result[0].Labels[LabelConstants.PHYSICAL_FILE_ID]);
    }

    [Fact]
    public async Task SearchBinders_WithUpdatedBefore_ShouldFilterCorrectly()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var criteria = new SearchBindersCriteria
        {
            UpdatedBefore = cutoffDate
        };

        var expectedBinders = CreateSampleBinders(2);
        expectedBinders[0].Upd_Dtm = DateTime.UtcNow.AddDays(-2);
        expectedBinders[1].Upd_Dtm = DateTime.UtcNow.AddDays(-3);

        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.True(b.UpdatedDate < cutoffDate));
    }

    [Fact]
    public async Task SearchBinders_WithMultipleFilters_ShouldCombineCorrectly()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-1);
        var criteria = new SearchBindersCriteria
        {
            LabelKeysExist = [LabelConstants.JUDGE_ID],
            LabelMatches = new Dictionary<string, string>
            {
                { LabelConstants.COURT_CLASS_CD, "C" }
            },
            UpdatedBefore = cutoffDate
        };

        var expectedBinders = CreateSampleBinders(1, includeJudgeId: true);
        expectedBinders[0].Labels[LabelConstants.COURT_CLASS_CD] = "C";
        expectedBinders[0].Upd_Dtm = DateTime.UtcNow.AddDays(-2);

        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result[0].Labels.ContainsKey(LabelConstants.JUDGE_ID));
        Assert.Equal("C", result[0].Labels[LabelConstants.COURT_CLASS_CD]);
        Assert.True(result[0].UpdatedDate < cutoffDate);
    }

    [Fact]
    public async Task SearchBinders_WithLimitAndSkip_ShouldPaginateCorrectly()
    {
        var criteria = new SearchBindersCriteria
        {
            Limit = 10,
            Skip = 5
        };

        var expectedBinders = CreateSampleBinders(10);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                10,
                5))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        _mockBinderRepo.Verify(r => r.FindAsync(
            CollectionNameConstants.BINDERS,
            It.IsAny<FilterDefinition<Binder>>(),
            null,
            10,
            5), Times.Once);
    }

    [Fact]
    public async Task SearchBinders_WithNoLimit_ShouldPassNullLimit()
    {
        var criteria = new SearchBindersCriteria
        {
            Limit = null
        };

        var expectedBinders = CreateSampleBinders(5);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                null,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        _mockBinderRepo.Verify(r => r.FindAsync(
            CollectionNameConstants.BINDERS,
            It.IsAny<FilterDefinition<Binder>>(),
            null,
            null,
            null), Times.Once);
    }

    [Fact]
    public async Task SearchBinders_WhenNoResults_ShouldReturnEmptyList()
    {
        var criteria = new SearchBindersCriteria
        {
            LabelKeysExist = ["NON_EXISTENT_KEY"]
        };

        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(new List<Binder>());

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchBinders_ShouldLogInformationWithCriteria()
    {
        var criteria = new SearchBindersCriteria
        {
            LabelKeysExist = [LabelConstants.JUDGE_ID],
            LabelMatches = new Dictionary<string, string>
            {
                { LabelConstants.COURT_CLASS_CD, "C" }
            },
            UpdatedBefore = DateTime.UtcNow.AddDays(-1)
        };

        var expectedBinders = CreateSampleBinders(2);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SearchBinders returned")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchBinders_WithEmptyCriteria_ShouldReturnAllBinders()
    {
        var criteria = new SearchBindersCriteria();

        var expectedBinders = CreateSampleBinders(5);
        _mockBinderRepo
            .Setup(r => r.FindAsync(
                CollectionNameConstants.BINDERS,
                It.IsAny<FilterDefinition<Binder>>(),
                null,
                100,
                null))
            .ReturnsAsync(expectedBinders);

        var result = await _binderService.SearchBinders(criteria);

        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    #endregion

    #region Helper Methods

    private List<Binder> CreateSampleBinders(int count, bool includeJudgeId = false)
    {
        var binders = new List<Binder>();
        for (int i = 0; i < count; i++)
        {
            var binder = new Binder
            {
                Id = _faker.Random.AlphaNumeric(24),
                Labels = new Dictionary<string, string>
                {
                    { LabelConstants.PHYSICAL_FILE_ID, _faker.Random.AlphaNumeric(10) },
                    { LabelConstants.COURT_CLASS_CD, _faker.PickRandom("C", "F", "L", "M", "A", "Y", "T") }
                },
                Documents = new List<BinderDocument>
                {
                    new() { DocumentId = _faker.Random.AlphaNumeric(10), Order = 0 }
                },
                Upd_Dtm = DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 30))
            };

            if (includeJudgeId)
            {
                binder.Labels[LabelConstants.JUDGE_ID] = _faker.Random.AlphaNumeric(8);
            }

            binders.Add(binder);
        }
        return binders;
    }

    #endregion
}
