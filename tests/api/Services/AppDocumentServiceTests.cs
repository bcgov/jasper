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
    public async Task UpdateAsync_WhenKeyExists_ShouldUpdateExistingEntityKeepingId()
    {
        var existing = new AppDocument { Key = "KEY", Name = "old.pdf" };
        var originalId = existing.Id;
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync(new List<AppDocument> { existing });
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<AppDocument>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync("KEY", CreateUpload());

        Assert.True(result.Succeeded);
        Assert.Equal(originalId, existing.Id);
        Assert.Equal("template.pdf", existing.Name);
        Assert.Null(result.Payload.Data);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeyIsNotFound_ShouldReturnFailureAndNotCallUpdate()
    {
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AppDocument, bool>>>()))
            .ReturnsAsync([]);

        var result = await _service.UpdateAsync("KEY", CreateUpload());

        Assert.False(result.Succeeded);
        Assert.Contains("App document not found.", result.Errors);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<AppDocument>()), Times.Never);
    }

    [Fact]
    public async Task GetAppDocumentByKeyAsync_ShouldCacheResultUntilUpdateAsync()
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

        await _service.UpdateAsync("KEY", CreateUpload());
        var afterUpdate = await _service.GetAppDocumentByKeyAsync("KEY");

        Assert.Equal("template.pdf", afterUpdate.Name);
    }
}
