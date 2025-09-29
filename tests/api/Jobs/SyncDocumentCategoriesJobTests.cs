using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
using PCSSCommon.Clients.ConfigurationServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Jobs;
using Scv.Db.Models;
using Scv.Db.Repositories;
using tests.api.Services;
using Xunit;

namespace tests.api.Jobs;
public class SyncDocumentCategoriesJobTests : ServiceTestBase
{
    private const string CONFIG_KEY = "JOBS:SYNC_DOCUMENT_CATEGORIES_SCHEDULE";

    private readonly Faker _faker;
    private readonly Mock<ILogger<SyncDocumentCategoriesJob>> _logger;
    private readonly Mock<ConfigurationServicesClient> _configClient;
    private readonly Mock<IRepositoryBase<DocumentCategory>> _mockRepo;
    private readonly IAppCache _appCache;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IConfigurationSection> _mockSection;
    private readonly SyncDocumentCategoriesJob _job;

    private List<PcssConfiguration> GetSampleData() =>
        [
            new()
            {
                Key = DocumentCategory.PSR,
                Value = _faker.Lorem.Paragraph(),
                PcssConfigurationId = _faker.Random.Int()
            },
            new()
            {
                Key = DocumentCategory.PLEADINGS,
                Value = _faker.Lorem.Paragraph(),
                PcssConfigurationId = _faker.Random.Int()
            }
        ];


    public SyncDocumentCategoriesJobTests()
    {
        _faker = new Faker();

        // Setup Cache
        _appCache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        _logger = new Mock<ILogger<SyncDocumentCategoriesJob>>();
        _configClient = new Mock<ConfigurationServicesClient>(MockBehavior.Strict, this.HttpClient);
        _mockRepo = new Mock<IRepositoryBase<DocumentCategory>>();
        _mockConfig = new Mock<IConfiguration>();
        _mockSection = new Mock<IConfigurationSection>();


        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new DocumentCategoryMapping());
        var mapper = new Mapper(config);

        _job = new SyncDocumentCategoriesJob(
            _mockConfig.Object,
            _appCache,
            mapper,
            _logger.Object,
            _configClient.Object,
            _mockRepo.Object);
    }

    [Fact]
    public async Task Execute_Should_AddUnsyncedCategories_And_UseDefaultSchedule()
    {
        _mockSection.Setup(s => s.Value).Returns((string)null);
        _mockConfig.Setup(c => c.GetSection(CONFIG_KEY)).Returns(_mockSection.Object);

        var data = this.GetSampleData();

        _configClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(data);
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()))
            .ReturnsAsync([]);
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<DocumentCategory>()));

        await _job.Execute();

        Assert.Equal(SyncDocumentCategoriesJob.DEFAULT_SCHEDULE, _job.CronSchedule);

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(data.Count));
        _mockRepo
            .Verify(r => r.AddAsync(It.IsAny<DocumentCategory>()), Times.Exactly(data.Count));
    }

    [Fact]
    public async Task Execute_Should_AddUnsyncedCategories_And_UseScheduleFromConfig()
    {
        var schedule = "0 0 * * *";
        _mockSection.Setup(s => s.Value).Returns(schedule);
        _mockConfig.Setup(c => c.GetSection(CONFIG_KEY)).Returns(_mockSection.Object);

        var data = this.GetSampleData();

        _configClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(data);
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()))
            .ReturnsAsync([]);
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<DocumentCategory>()));

        await _job.Execute();

        Assert.Equal(schedule, _job.CronSchedule);

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(data.Count));
        _mockRepo
            .Verify(r => r.AddAsync(It.IsAny<DocumentCategory>()), Times.Exactly(data.Count));
    }

    [Fact]
    public async Task Execute_ShouldHandleException_WhenExecutedFails()
    {
        _mockSection.Setup(s => s.Value).Returns((string)null);
        _mockConfig.Setup(c => c.GetSection(CONFIG_KEY)).Returns(_mockSection.Object);

        _configClient
            .Setup(c => c.GetAllAsync())
            .Throws(new InvalidOperationException());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _job.Execute());

        _configClient.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshDocumentCategoriesAsync_ShouldUpdateCategories_WhenCategoryHasChanged()
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string)null);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection(CONFIG_KEY)).Returns(mockSection.Object);

        var key = DocumentCategory.PSR;
        var value = _faker.Lorem.Paragraph();
        var configId = _faker.Random.Int();

        var configData = new List<PcssConfiguration>
        {
            new()
            {
                Key = key,
                Value = value,
                PcssConfigurationId = configId
            }
        };

        var dcData = new List<DocumentCategory>
        {
            new()
            {
                Name = key,
                Value = _faker.Lorem.Paragraph()
            }
        };

        _configClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(configData);
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()))
            .ReturnsAsync(dcData);
        _mockRepo
            .Setup(r => r.UpdateAsync(It.IsAny<DocumentCategory>()));

        await _job.Execute();

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(configData.Count));
        _mockRepo
            .Verify(r => r.UpdateAsync(It.IsAny<DocumentCategory>()), Times.Exactly(configData.Count));
    }

    [Fact]
    public async Task RefreshDocumentCategoriesAsync_ShouldNotUpdateCategories_WhenCategoryHasNotChanged()
    {
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string)null);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection(CONFIG_KEY)).Returns(mockSection.Object);

        var key = DocumentCategory.PSR;
        var value = _faker.Lorem.Paragraph();
        var configId = _faker.Random.Int();

        var configData = new List<PcssConfiguration>
        {
            new()
            {
                Key = key,
                Value = value,
                PcssConfigurationId = configId
            }
        };

        var dcData = new List<DocumentCategory>
        {
            new()
            {
                Name = key,
                Value = value
            }
        };

        _configClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(configData);
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()))
            .ReturnsAsync(dcData);
        _mockRepo
            .Setup(r => r.UpdateAsync(It.IsAny<DocumentCategory>()));

        await _job.Execute();

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(configData.Count));
        _mockRepo
            .Verify(r => r.UpdateAsync(It.IsAny<DocumentCategory>()), Times.Exactly(0));
    }
}
