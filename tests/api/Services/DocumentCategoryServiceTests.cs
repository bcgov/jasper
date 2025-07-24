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
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.ConfigurationServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;
public class DocumentCategoryServiceTests : ServiceTestBase
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<DocumentCategory>> _mockRepo;
    private readonly DocumentCategoryService _dcService;
    private readonly Mock<ConfigurationServicesClient> _configClient;

    public DocumentCategoryServiceTests()
    {
        _faker = new Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        var mapper = new Mapper(config);

        // ILogger setup
        var logger = new Mock<ILogger<DocumentCategoryService>>();

        _mockRepo = new Mock<IRepositoryBase<DocumentCategory>>();

        _configClient = new Mock<ConfigurationServicesClient>(MockBehavior.Strict, this.HttpClient);


        _dcService = new DocumentCategoryService(
            cachingService,
            logger.Object,
            _mockRepo.Object,
            _configClient.Object);

    }

    [Fact]
    public async Task RefreshDocumentCategoriesAsync_ShouldAddUnsyncedCategories()
    {
        var configData = new List<PcssConfiguration>
        {
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
        };

        _configClient
            .Setup(c => c.GetAllAsync())
            .ReturnsAsync(configData);
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()))
            .ReturnsAsync([]);
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<DocumentCategory>()));

        await _dcService.RefreshDocumentCategoriesAsync();

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(configData.Count));
        _mockRepo
            .Verify(r => r.AddAsync(It.IsAny<DocumentCategory>()), Times.Exactly(configData.Count));
    }

    [Fact]
    public async Task RefreshDocumentCategoriesAsync_ShouldUpdateCategories_WhenCategoryHasChanged()
    {
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

        await _dcService.RefreshDocumentCategoriesAsync();

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(configData.Count));
        _mockRepo
            .Verify(r => r.UpdateAsync(It.IsAny<DocumentCategory>()), Times.Exactly(configData.Count));
    }

    [Fact]
    public async Task RefreshDocumentCategoriesAsync_ShouldNotUpdateCategories_WhenCategoryHasNotChanged()
    {
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

        await _dcService.RefreshDocumentCategoriesAsync();

        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<DocumentCategory, bool>>>()), Times.Exactly(configData.Count));
        _mockRepo
            .Verify(r => r.UpdateAsync(It.IsAny<DocumentCategory>()), Times.Exactly(0));
    }
}
