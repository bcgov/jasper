using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Processors;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class AppDocumentServiceTests
{
    private readonly Mock<IRepositoryBase<AppDocument>> _mockRepo = new();
    private readonly AppDocumentService _service;

    public AppDocumentServiceTests()
    {
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new AppDocumentMapping());

        _service = new AppDocumentService(
            cachingService,
            new Mapper(config),
            new Mock<ILogger<AppDocumentService>>().Object,
            _mockRepo.Object);
    }

    private static UploadedFile CreateUpload() => new([1, 2, 3], "application/pdf", "template.pdf");

    [Fact]
    public async Task UpsertAsync_WhenKeyDoesNotExist_ShouldAddEntityWithGeneratedId()
    {
        AppDocument added = null;
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync(new List<AppDocument>());
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AppDocument>()))
            .Callback<AppDocument>(e => added = e)
            .Returns(Task.CompletedTask);

        var result = await _service.UpsertAsync("KEY", CreateUpload());

        Assert.True(result.Succeeded);
        Assert.NotNull(added);
        Assert.False(string.IsNullOrEmpty(added.Id));
        Assert.Equal("KEY", added.Key);
        Assert.Equal(3, added.Size);
        Assert.Null(result.Payload.Data);
    }

    [Fact]
    public async Task UpsertAsync_WhenKeyExists_ShouldUpdateExistingEntityKeepingId()
    {
        var existing = new AppDocument { Key = "KEY", Name = "old.pdf" };
        var originalId = existing.Id;
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync(new List<AppDocument> { existing });
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<AppDocument>())).Returns(Task.CompletedTask);

        var result = await _service.UpsertAsync("KEY", CreateUpload());

        Assert.True(result.Succeeded);
        Assert.Equal(originalId, existing.Id);
        Assert.Equal("template.pdf", existing.Name);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_WhenRepositoryThrows_ShouldReturnFailure()
    {
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync(new List<AppDocument>());
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AppDocument>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.UpsertAsync("KEY", CreateUpload());

        Assert.False(result.Succeeded);
        Assert.Contains("An error occurred while upserting the app document.", result.Errors);
    }

    [Fact]
    public async Task GetAppDocumentByKeyAsync_ShouldCacheResultUntilUpsert()
    {
        var existing = new AppDocument { Key = "KEY", Name = "old.pdf" };
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync(new List<AppDocument> { existing });
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<AppDocument>())).Returns(Task.CompletedTask);

        var first = await _service.GetAppDocumentByKeyAsync("KEY");
        var second = await _service.GetAppDocumentByKeyAsync("KEY");

        Assert.Equal("old.pdf", first.Name);
        Assert.Same(first, second);
        _mockRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()), Times.Once);

        await _service.UpsertAsync("KEY", CreateUpload());
        var afterUpsert = await _service.GetAppDocumentByKeyAsync("KEY");

        Assert.Equal("template.pdf", afterUpsert.Name);
    }
}
