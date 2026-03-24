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
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.Configuration;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<IRepositoryBase<Constant>> _mockConstantRepo;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        var mapper = new Mapper(config);

        var logger = new Mock<ILogger<ConfigurationService>>();

        _mockConstantRepo = new Mock<IRepositoryBase<Constant>>();
        _configurationService = new ConfigurationService(
            cachingService,
            mapper,
            logger.Object,
            _mockConstantRepo.Object);
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnMappedConstants()
    {
        var constants = new List<Constant>
        {
            new() { Id = "c1", Key = "ReleaseNotesUrl", Values = ["https://example.com"] },
            new() { Id = "c2", Key = "OtherKey", Values = ["value1", "value2"] }
        };

        _mockConstantRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(constants);

        var result = (await _configurationService.GetConfigurationAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("ReleaseNotesUrl", result[0].Key);
        Assert.Equal("https://example.com", result[0].Values.First());
        Assert.Equal("OtherKey", result[1].Key);
        Assert.Equal(2, result[1].Values.Count);
        _mockConstantRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess()
    {
        var dto = new ConstantDto
        {
            Id = "c1",
            Key = "ReleaseNotesUrl",
            Values = ["https://example.com"]
        };

        var result = await _configurationService.ValidateAsync(dto, false);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }
}
