using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Scv.Api.Services;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.CourtLocation;
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

    [Fact]
    public async Task ReplaceCourtLocationsAsync_ReturnsSuccess_AndReplacesMappedEntities()
    {
        var dtos = new[]
        {
            new CourtLocationDto { Code = "4801", Name = "Vancouver" },
            new CourtLocationDto { Code = "4811", Name = "Victoria" },
        };

        IEnumerable<CourtLocation> captured = null;
        _mockRepo
            .Setup(r => r.ReplaceAllAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<CourtLocation>>(),
                It.IsAny<FilterDefinition<CourtLocation>>()))
            .Callback<string, IEnumerable<CourtLocation>, FilterDefinition<CourtLocation>>(
                (_, entities, _) => captured = [.. entities])
            .Returns(Task.CompletedTask);

        var result = await _service.ReplaceCourtLocationsAsync(dtos);

        Assert.True(result.Succeeded);

        _mockRepo.Verify(
            r => r.ReplaceAllAsync(
                CollectionNameConstants.COURT_LOCATIONS,
                It.IsAny<IEnumerable<CourtLocation>>(),
                It.IsAny<FilterDefinition<CourtLocation>>()),
            Times.Once());

        Assert.NotNull(captured);
        var mapped = captured.ToList();
        Assert.Equal(2, mapped.Count);
        Assert.Equal("4801", mapped[0].Code);
        Assert.Equal("Vancouver", mapped[0].Name);
        Assert.Equal("4811", mapped[1].Code);
        Assert.Equal("Victoria", mapped[1].Name);
    }

    [Fact]
    public async Task ReplaceCourtLocationsAsync_ReturnsSuccess_WhenGivenEmptyArray()
    {
        _mockRepo
            .Setup(r => r.ReplaceAllAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<CourtLocation>>(),
                It.IsAny<FilterDefinition<CourtLocation>>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ReplaceCourtLocationsAsync([]);

        Assert.True(result.Succeeded);
        _mockRepo.Verify(
            r => r.ReplaceAllAsync(
                CollectionNameConstants.COURT_LOCATIONS,
                It.IsAny<IEnumerable<CourtLocation>>(),
                It.IsAny<FilterDefinition<CourtLocation>>()),
            Times.Once());
    }

    [Fact]
    public async Task ReplaceCourtLocationsAsync_ReturnsFailure_WhenRepositoryThrows()
    {
        _mockRepo
            .Setup(r => r.ReplaceAllAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<CourtLocation>>(),
                It.IsAny<FilterDefinition<CourtLocation>>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await _service.ReplaceCourtLocationsAsync(
            [new CourtLocationDto { Code = "4801", Name = "Vancouver" }]);

        Assert.False(result.Succeeded);
        Assert.Contains("db down", result.Errors);
    }
}
