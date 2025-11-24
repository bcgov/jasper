using System;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.TimebankServices;
using Scv.Api.Models.Timebank;
using Scv.Api.Services;
using Xunit;
using TimebankApiException = PCSSCommon.Clients.TimebankServices.ApiException;

namespace tests.api.Services;

public class TimebankServiceTests : ServiceTestBase
{
    private readonly Faker _faker;

    public TimebankServiceTests()
    {
        _faker = new Faker();
    }

    private (
        TimebankService timebankService,
        Mock<TimebankServicesClient> mockTimebankClient,
        Mock<IMapper> mockMapper
    ) SetupTimebankService()
    {
        var mockTimebankClient = new Mock<TimebankServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockMapper = new Mock<IMapper>();

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var timebankService = new TimebankService(
            cachingService,
            mockTimebankClient.Object,
            new Mock<ILogger<TimebankService>>().Object,
            mockMapper.Object);

        return (timebankService, mockTimebankClient, mockMapper);
    }

    #region GetTimebankSummaryForJudgeAsync Tests

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnSuccess_WhenDataExists()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var expectedSummary = new PCSSCommon.Clients.TimebankServices.TimebankSummary
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            FirstNm = _faker.Name.FirstName(),
            SurnameNm = _faker.Name.LastName(),
            LocationId = _faker.Random.Int(1, 100)
        };

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        Assert.Equal(judgeId, result.Payload.JudiciaryPersonId);
        Assert.Equal(period, result.Payload.Period);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnSuccess_WithIncludeLineItems()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var includeLineItems = true;
        var expectedSummary = new PCSSCommon.Clients.TimebankServices.TimebankSummary
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            LocationId = _faker.Random.Int(1, 100)
        };

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                includeLineItems,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSummary);

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId, includeLineItems);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            includeLineItems,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnFailure_WhenDataIsNull()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PCSSCommon.Clients.TimebankServices.TimebankSummary)null);

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("No timebank summary data available", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnSuccess_When204NoContent()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimebankApiException("No Content", 204, null, null, null));

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.True(result.Succeeded);
        Assert.Null(result.Payload);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnFailure_WhenApiExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var statusCode = 500;

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimebankApiException("Internal Server Error", statusCode, null, null, null));

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains($"Status: {statusCode}", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnFailure_WhenArgumentNullExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);

#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("judgeId", "Parameter cannot be null"));
#pragma warning restore S3928

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid parameters", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankSummaryForJudgeAsync_ShouldReturnFailure_WhenUnexpectedExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);

        mockTimebankClient
            .Setup(c => c.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await timebankService.GetTimebankSummaryForJudgeAsync(period, judgeId);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("unexpected error", result.Errors[0], StringComparison.OrdinalIgnoreCase);

        mockTimebankClient.Verify(c => c.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    #endregion

    #region GetTimebankPayoutsForJudgesAsync Tests

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnSuccess_WhenDataExists()
    {
        var (timebankService, mockTimebankClient, mockMapper) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var expiryDate = _faker.Date.Future();
        var expectedPayout = new PCSSCommon.Clients.TimebankServices.VacationPayout
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = _faker.Random.Double(1000, 5000),
            EffectiveDate = DateTimeOffset.Now
        };

        var expectedDto = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = expectedPayout.TotalPayout,
            EffectiveDate = expectedPayout.EffectiveDate
        };

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<string>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayout);

        mockMapper.Setup(m => m.Map<VacationPayoutDto>(expectedPayout))
            .Returns(expectedDto);

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, expiryDate, rate);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        Assert.Equal(judgeId, result.Payload.JudiciaryPersonId);
        Assert.Equal(period, result.Payload.Period);
        Assert.Equal(rate, result.Payload.Rate);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            It.IsAny<string>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnSuccess_WithoutExpiryDate()
    {
        var (timebankService, mockTimebankClient, mockMapper) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var expectedPayout = new PCSSCommon.Clients.TimebankServices.VacationPayout
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = _faker.Random.Double(1000, 5000),
            EffectiveDate = DateTimeOffset.Now
        };

        var expectedDto = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = expectedPayout.TotalPayout,
            EffectiveDate = expectedPayout.EffectiveDate
        };

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                null,
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayout);

        mockMapper.Setup(m => m.Map<VacationPayoutDto>(It.IsAny<PCSSCommon.Clients.TimebankServices.VacationPayout>()))
            .Returns(expectedDto);

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, null, rate);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            null,
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldFormatExpiryDateCorrectly()
    {
        var (timebankService, mockTimebankClient, mockMapper) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var expiryDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
        var expectedFormattedDate = "31-Dec-2024";
        var expectedPayout = new PCSSCommon.Clients.TimebankServices.VacationPayout
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = _faker.Random.Double(1000, 5000),
            EffectiveDate = DateTimeOffset.Now
        };

        var expectedDto = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = expectedPayout.TotalPayout,
            EffectiveDate = expectedPayout.EffectiveDate
        };

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                expectedFormattedDate,
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayout);

        mockMapper.Setup(m => m.Map<VacationPayoutDto>(It.IsAny<PCSSCommon.Clients.TimebankServices.VacationPayout>()))
            .Returns(expectedDto);

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, expiryDate, rate);

        Assert.True(result.Succeeded);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            expectedFormattedDate,
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnFailure_WhenDataIsNull()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<string>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PCSSCommon.Clients.TimebankServices.VacationPayout)null);

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, null, rate);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("No timebank payout data available", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            It.IsAny<string>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnFailure_WhenApiExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var statusCode = 400;

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<string>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimebankApiException("Bad Request", statusCode, null, null, null));

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, null, rate);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains($"Status: {statusCode}", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            It.IsAny<string>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnFailure_WhenArgumentNullExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);

#pragma warning disable S3928 // Parameter names used into ArgumentException constructors should match an existing one 
        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<string>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("period", "Parameter cannot be null"));
#pragma warning restore S3928

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, null, rate);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid parameters", result.Errors[0]);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            It.IsAny<string>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudgesAsync_ShouldReturnFailure_WhenUnexpectedExceptionThrown()
    {
        var (timebankService, mockTimebankClient, _) = SetupTimebankService();
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);

        mockTimebankClient
            .Setup(c => c.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<string>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        var result = await timebankService.GetTimebankPayoutsForJudgesAsync(period, judgeId, null, rate);

        Assert.False(result.Succeeded);
        Assert.Null(result.Payload);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("unexpected error", result.Errors[0], StringComparison.OrdinalIgnoreCase);

        mockTimebankClient.Verify(c => c.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            It.IsAny<string>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once());
    }

    #endregion
}
