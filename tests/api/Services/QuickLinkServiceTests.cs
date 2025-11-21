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
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class QuickLinkServiceTest
{
    private readonly Mock<IRepositoryBase<QuickLink>> _mockQuickLinkRepo;
    private readonly QuickLinkService _quickLinkService;
    private readonly List<QuickLink> _quickLinks;

    public QuickLinkServiceTest()
    {
        _quickLinks = [ 
            new() { Id = "ql1", Name = "Quick Link 1", ParentName = "", Order = 1, JudgeId = null },
            new() { Id = "ql2", Name = "Quick Link 2", ParentName = "", Order = 2, JudgeId = null } 
            ];
        
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        var mapper = new Mapper(config);

        var logger = new Mock<ILogger<QuickLinkService>>();

        _mockQuickLinkRepo = new Mock<IRepositoryBase<QuickLink>>();
        _quickLinkService = new QuickLinkService(cachingService, mapper, logger.Object, _mockQuickLinkRepo.Object);
    }

    [Fact]
    public async Task GetJudgeQuickLinks_ShouldReturnMappedQuickLinks()
    {
        _mockQuickLinkRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<QuickLink, bool>>>())).ReturnsAsync(_quickLinks);

        var result = await _quickLinkService.GetJudgeQuickLinks();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, dto =>
        {
            var expected = _quickLinks.First(q => q.Id == dto.Id);
            Assert.Equal(expected.Name, dto.Name);
            Assert.Equal(expected.ParentName, dto.ParentName);
            Assert.Equal(expected.Order, dto.Order);
        });
        _mockQuickLinkRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<QuickLink, bool>>>()), Times.Once);
    }
}