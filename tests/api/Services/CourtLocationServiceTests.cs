using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class CourtLocationServiceTests
{
    private readonly Mock<IRepositoryBase<CourtLocation>> _mockRepo;
    private readonly CourtLocationService _service;

    public CourtLocationServiceTests()
    {
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var mapper = new Mapper(new TypeAdapterConfig());
        var logger = new Mock<ILogger<CourtLocationService>>();

        _mockRepo = new Mock<IRepositoryBase<CourtLocation>>();
        _service = new CourtLocationService(cachingService, mapper, logger.Object, _mockRepo.Object);
    }

    [Fact]
    public async Task GetCourtLocationByCodeAsync_ReturnsSuccessWithPayload_WhenFound()
    {
        var entity = new CourtLocation { Code = "4801", Name = "Vancouver" };
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CourtLocation, bool>>>()))
            .ReturnsAsync([entity]);

        var result = await _service.GetCourtLocationByCodeAsync("4801");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        Assert.Equal("4801", result.Payload.Code);
        Assert.Equal("Vancouver", result.Payload.Name);
        _mockRepo.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<CourtLocation, bool>>>()),
            Times.Once());
    }

    [Fact]
    public async Task GetCourtLocationByCodeAsync_ReturnsSuccessWithNullPayload_WhenNotFound()
    {
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CourtLocation, bool>>>()))
            .ReturnsAsync([]);

        var result = await _service.GetCourtLocationByCodeAsync("9999");

        Assert.True(result.Succeeded);
        Assert.Null(result.Payload);
        _mockRepo.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<CourtLocation, bool>>>()),
            Times.Once());
    }

    [Fact]
    public async Task GetCourtLocationByCodeAsync_ReturnsFailure_WhenRepositoryThrows()
    {
        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CourtLocation, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await _service.GetCourtLocationByCodeAsync("4801");

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.Contains("Something went wrong when retrieving the court location.", result.Errors);
    }
}
