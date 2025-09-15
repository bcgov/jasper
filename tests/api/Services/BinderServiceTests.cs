using System;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Processors;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace tests.api.Services;
public class BinderServiceTests
{
    private readonly Bogus.Faker _faker;
    private readonly Mock<IRepositoryBase<Binder>> _mockBinderRepo;
    private readonly Mock<IBinderFactory> _mockBinderFactory;
    private readonly BinderService _binderService;

    public BinderServiceTests()
    {
        _faker = new Bogus.Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        var mapper = new Mapper(config);

        // ILogger setup
        var logger = new Mock<ILogger<BinderService>>();

        _mockBinderRepo = new Mock<IRepositoryBase<Binder>>();
        _mockBinderFactory = new Mock<IBinderFactory>();

        _binderService = new BinderService(
            cachingService,
            mapper,
            logger.Object,
            _mockBinderRepo.Object,
            _mockBinderFactory.Object,
            new Mock<IDocumentMerger>().Object);
    }
}
