using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Db.Contants;
using Xunit;

namespace tests.api.Processors;

public class BinderFactoryTests
{
    private static BinderFactory CreateFactory(
        Mock<ILogger<BinderFactory>> loggerMock,
        out Mock<IValidator<BinderDto>> validatorMock)
    {
        var httpClient = new HttpClient();
        var filesClient = new FileServicesClient(httpClient);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")], "mock"));

        loggerMock ??= new Mock<ILogger<BinderFactory>>();

        validatorMock = new Mock<IValidator<BinderDto>>();
        validatorMock
            .Setup(v => v.Validate(It.IsAny<ValidationContext<BinderDto>>()))
            .Returns(new ValidationResult());

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new TypeAdapterConfig();
        var mapper = new Mapper(config);

        return new BinderFactory(
            filesClient,
            user,
            loggerMock.Object,
            validatorMock.Object,
            cachingService,
            new Mock<IConfiguration>().Object,
            new Mock<IDocumentConverter>().Object,
            mapper);
    }

    private static Dictionary<string, string> Labels(string courtClass) =>
        new()
        {
            { LabelConstants.COURT_CLASS_CD, courtClass }
        };

    [Theory]
    [InlineData("C")]
    [InlineData("F")]
    [InlineData("L")]
    [InlineData("M")]
    public void BinderFactory_ReturnsJudicialBinderProcessor_WhenCourtClassIsCivil(string courtClass)
    {
        var logger = new Mock<ILogger<BinderFactory>>();
        var factory = CreateFactory(logger, out _);

        var processor = factory.Create(Labels(courtClass));

        Assert.NotNull(processor);
        Assert.IsType<JudicialBinderProcessor>(processor);
        Assert.Equal(courtClass, processor.Binder.Labels[LabelConstants.COURT_CLASS_CD]);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Y")]
    [InlineData("T")]
    public void BinderFactory_ReturnsJudicialBinderProcessor_WhenCourtClassIsCriminal(string courtClass)
    {
        var logger = new Mock<ILogger<BinderFactory>>();
        var factory = CreateFactory(logger, out _);

        var processor = factory.Create(Labels(courtClass));

        Assert.NotNull(processor);
        Assert.IsType<KeyDocumentsBinderProcessor>(processor);
        Assert.Equal(courtClass, processor.Binder.Labels[LabelConstants.COURT_CLASS_CD]);
    }

    [Fact]
    public void BinderFactoryReturns_JudicialBinderProcessor_WhenCourtClassIsLowercase()
    {
        var logger = new Mock<ILogger<BinderFactory>>();
        var factory = CreateFactory(logger, out _);

        var processor = factory.Create(Labels("c"));

        Assert.IsType<JudicialBinderProcessor>(processor);
        Assert.Equal("c", processor.Binder.Labels[LabelConstants.COURT_CLASS_CD]);
    }

    [Fact]
    public void BinderFactoryReturns_ThrowsAndLogsError_WhenCourtClassIsInvalid()
    {
        var logger = new Mock<ILogger<BinderFactory>>();
        var factory = CreateFactory(logger, out _);

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(Labels("Z")));
        Assert.Contains("processor", ex.Message, StringComparison.OrdinalIgnoreCase);

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("invalid", StringComparison.OrdinalIgnoreCase)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void BinderFactory_ReturnsKeyDocumentProcessor_WhenBinderDtoIsPassed()
    {
        var logger = new Mock<ILogger<BinderFactory>>();
        var factory = CreateFactory(logger, out _);
        var dto = new BinderDto
        {
            Labels = Labels("A"),
            Documents = []
        };

        var processor = factory.Create(dto);

        Assert.IsType<KeyDocumentsBinderProcessor>(processor);
        Assert.Same(dto, processor.Binder);
    }
}
