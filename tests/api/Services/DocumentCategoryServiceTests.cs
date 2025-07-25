using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PCSSCommon.Clients.ConfigurationServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Services;
public class DocumentCategoryServiceTests : ServiceTestBase
{
    private readonly Faker _faker;
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
        config.Apply(new DocumentCategoryMapping());
        var mapper = new Mapper(config);

        _configClient = new Mock<ConfigurationServicesClient>(MockBehavior.Strict, this.HttpClient);

        _dcService = new DocumentCategoryService(
            cachingService,
            mapper,
            _configClient.Object);

    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDocumentCategories()
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

        var result = await _dcService.GetAllAsync();

        Assert.Equal(result.Count(), configData.Count);
        _configClient.Verify(c => c.GetAllAsync(), Times.Once());
    }
}
