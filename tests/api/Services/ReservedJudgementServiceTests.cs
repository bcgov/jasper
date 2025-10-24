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
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class ReservedJudgementServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<Case>> _mockJudgementRepo;
    private readonly CaseService _judgementService;

    public ReservedJudgementServiceTests()
    {
        _faker = new Faker();

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() => new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        var mapper = new Mapper(config);
        var logger = new Mock<ILogger<CaseService>>();

        _mockJudgementRepo = new Mock<IRepositoryBase<Case>>();
        _judgementService = new CaseService(cachingService, mapper, logger.Object, _mockJudgementRepo.Object);
    }

    [Fact]
    public async Task GetReservedJudgementsAsync_ShouldReturnMappedJudgements()
    {
        var fileNumber = _faker.Random.AlphaNumeric(10);
        var mockJudgement = new Case
        {
            FileNumber = fileNumber,
        };

        _mockJudgementRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([mockJudgement]);

        var result = await _judgementService.GetAllAsync();

        Assert.Single(result);
        Assert.Equal(fileNumber, result.FirstOrDefault().FileNumber);
        _mockJudgementRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReservedJudgementAsync_ShouldReturnSuccess()
    {
        var mockJudgement = new Scv.Api.Models.CaseDto();
        _mockJudgementRepo.Setup(r => r.AddAsync(It.IsAny<Case>())).Returns(Task.CompletedTask);

        var result = await _judgementService.AddAsync(mockJudgement);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockJudgementRepo.Verify(r => r.AddAsync(It.IsAny<Case>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeReservedJudgementAsync_ShouldReturnSuccess()
    {
        var mockJudgements = new List<Scv.Api.Models.CaseDto>();
        _mockJudgementRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);

        var result = await _judgementService.AddRangeAsync(mockJudgements);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockJudgementRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<Case>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeReservedJudgementAsync_ShouldInvokeWhenEntitiesFound()
    {
        var mockJudgementDto = new Scv.Api.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Api.Models.CaseDto>() { mockJudgementDto };
        _mockJudgementRepo.Setup(r => r.DeleteRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);
        var mockJudgementEntity = new Case();
        _mockJudgementRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(mockJudgementEntity));

        var result = await _judgementService.DeleteRangeAsync(["test-id"]);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockJudgementRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<List<Case>>()), Times.Once);
    }

        [Fact]
    public async Task DeleteRangeReservedJudgementAsync_ShouldNotInvokeWhenEntitiesNotFound()
    {
        var mockJudgementDto = new Scv.Api.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Api.Models.CaseDto>() { mockJudgementDto };
        _mockJudgementRepo.Setup(r => r.DeleteRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);
        var mockJudgementEntity = new Case();

        var result = await _judgementService.DeleteRangeAsync(["test-id"]);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        _mockJudgementRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<List<Case>>()), Times.Never);
    }
}