using System;
using System.Net.Http;
using Bogus;
using Hangfire;
using LazyCache;
using LazyCache.Providers;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Jobs;
using Xunit;

namespace tests.api.Jobs;

public class PrimePcssUserCacheJobTests
{
    private readonly Faker _faker;
    private readonly Mock<IAuthorizationServicesClient> _mockPcssAuthorizationServiceClient;
    private readonly Mock<ILogger<PrimePcssUserCacheJob>> _mockLogger;
    private readonly IAppCache _cache;
    private readonly Mock<IMapper> _mapper;

    public PrimePcssUserCacheJobTests()
    {
        _faker = new Faker();

        _mockPcssAuthorizationServiceClient = new Mock<IAuthorizationServicesClient>();
        _mockLogger = new Mock<ILogger<PrimePcssUserCacheJob>>();
        _mapper = new Mock<IMapper>();

        _cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
    }

    private Mock<IConfiguration> SetupMockConfiguration(string expiryMinutes = "60")
    {
        var mockConfig = new Mock<IConfiguration>();
        
        var mockExpirySection = new Mock<IConfigurationSection>();
        mockExpirySection.Setup(s => s.Value).Returns(expiryMinutes);
        mockConfig.Setup(c => c.GetSection("Caching:UserExpiryMinutes")).Returns(mockExpirySection.Object);

        return mockConfig;
    }

    [Fact]
    public void JobName_ReturnsCorrectName()
    {
        var mockConfig = SetupMockConfiguration();
        var job = new PrimePcssUserCacheJob(
            mockConfig.Object,
            _cache,
            _mapper.Object,
            _mockLogger.Object,
            _mockPcssAuthorizationServiceClient.Object);

        Assert.Equal(nameof(PrimePcssUserCacheJob), job.JobName);
    }

    [Theory]
    [InlineData("15", 15)]
    [InlineData("30", 30)]
    [InlineData("45", 45)]
    public void CronSchedule_ReturnsCorrectExpression_ForMinutes(string expiryMinutes, int minuteInterval)
    {
        var mockConfig = SetupMockConfiguration(expiryMinutes);
        var job = new PrimePcssUserCacheJob(
            mockConfig.Object,
            _cache,
            _mapper.Object,
            _mockLogger.Object,
            _mockPcssAuthorizationServiceClient.Object);

        var schedule = job.CronSchedule;

        Assert.Equal(Cron.MinuteInterval(minuteInterval), schedule);
    }

    [Theory]
    [InlineData("60", 1)]
    [InlineData("120", 2)]
    [InlineData("180", 3)]
    [InlineData("240", 4)]
    [InlineData("360", 6)]
    [InlineData("480", 8)]
    [InlineData("720", 12)]
    public void CronSchedule_ReturnsCorrectExpression_ForHours(string expiryMinutes, int hourInterval)
    {
        var mockConfig = SetupMockConfiguration(expiryMinutes);
        var job = new PrimePcssUserCacheJob(
            mockConfig.Object,
            _cache,
            _mapper.Object,
            _mockLogger.Object,
            _mockPcssAuthorizationServiceClient.Object);

        var schedule = job.CronSchedule;

        Assert.Equal(Cron.HourInterval(hourInterval), schedule);
    }

    [Theory]
    [InlineData("1440", 1)]
    [InlineData("2880", 2)]
    public void CronSchedule_ReturnsCorrectExpression_ForDays(string expiryMinutes, int dayInterval)
    {
        var mockConfig = SetupMockConfiguration(expiryMinutes);
        var job = new PrimePcssUserCacheJob(
            mockConfig.Object,
            _cache,
            _mapper.Object,
            _mockLogger.Object,
            _mockPcssAuthorizationServiceClient.Object);

        var schedule = job.CronSchedule;

        Assert.Equal(Cron.DayInterval(dayInterval), schedule);
    }

    [Theory]
    [InlineData("90")]
    [InlineData("150")]
    public void CronSchedule_Throws_ForUnsupportedIntervals(string expiryMinutes)
    {
        var mockConfig = SetupMockConfiguration(expiryMinutes);
        var job = new PrimePcssUserCacheJob(
            mockConfig.Object,
            _cache,
            _mapper.Object,
            _mockLogger.Object,
            _mockPcssAuthorizationServiceClient.Object);

        Assert.Throws<ArgumentException>(() => _ = job.CronSchedule);
    }
}
