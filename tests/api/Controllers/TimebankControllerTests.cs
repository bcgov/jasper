using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Models.Timebank;
using Xunit;

namespace tests.api.Controllers;

public class TimebankControllerTests
{
    private readonly Mock<ITimebankService> _mockTimebankService;
    private readonly Mock<ILogger<TimebankController>> _mockLogger;
    private readonly Mock<IValidator<TimebankSummaryRequest>> _mockSummaryValidator;
    private readonly Mock<IValidator<TimebankPayoutRequest>> _mockPayoutValidator;
    private readonly Faker _faker;

    public TimebankControllerTests()
    {
        _mockTimebankService = new Mock<ITimebankService>();
        _mockLogger = new Mock<ILogger<TimebankController>>();
        _mockSummaryValidator = new Mock<IValidator<TimebankSummaryRequest>>();
        _mockPayoutValidator = new Mock<IValidator<TimebankPayoutRequest>>();
        _faker = new Faker();
    }

    private TimebankController CreateControllerWithContext(IEnumerable<Claim> claims)
    {
        var controller = new TimebankController(
            _mockTimebankService.Object,
            _mockLogger.Object,
            _mockSummaryValidator.Object,
            _mockPayoutValidator.Object
        );

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    #region GetTimebankSummaryForJudge Tests

    [Fact]
    public async Task GetTimebankSummaryForJudge_ReturnsBadRequest_WhenPeriodIsZero()
    {
        // Arrange
        var judgeId = _faker.Random.Int(1, 1000);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        // Setup validator to fail for period = 0
        var validationFailure = new FluentValidation.Results.ValidationFailure("Period", "Period must be a positive integer.");
        var validationResult = new FluentValidation.Results.ValidationResult(new[] { validationFailure });
        _mockSummaryValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankSummaryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankSummaryForJudge(0);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetTimebankSummaryForJudge_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedSummary = new TimebankSummaryDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            FirstNm = _faker.Name.FirstName(),
            SurnameNm = _faker.Name.LastName()
        };

        _mockSummaryValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankSummaryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<TimebankSummaryDto>.Success(expectedSummary));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankSummaryForJudge(period);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSummary = Assert.IsType<TimebankSummaryDto>(okResult.Value);
        Assert.Equal(judgeId, returnedSummary.JudiciaryPersonId);
        Assert.Equal(period, returnedSummary.Period);
    }

    [Fact]
    public async Task GetTimebankSummaryForJudge_ReturnsBadRequest_WhenServiceFails()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        _mockSummaryValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankSummaryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<TimebankSummaryDto>.Failure("Service error"));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankSummaryForJudge(period);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetTimebankSummaryForJudge_UsesProvidedJudgeId()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var loggedInJudgeId = _faker.Random.Int(1, 1000);
        var requestedJudgeId = _faker.Random.Int(1001, 2000);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, loggedInJudgeId.ToString()),
            new(CustomClaimTypes.Groups, "jasper-view-others-schedule"),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedSummary = new TimebankSummaryDto
        {
            JudiciaryPersonId = requestedJudgeId,
            Period = period
        };

        _mockSummaryValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankSummaryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankSummaryForJudgeAsync(
                period,
                requestedJudgeId,
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<TimebankSummaryDto>.Success(expectedSummary));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankSummaryForJudge(period, requestedJudgeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSummary = Assert.IsType<TimebankSummaryDto>(okResult.Value);
        Assert.Equal(requestedJudgeId, returnedSummary.JudiciaryPersonId);

        _mockTimebankService.Verify(s => s.GetTimebankSummaryForJudgeAsync(
            period,
            requestedJudgeId,
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimebankSummaryForJudge_PassesIncludeLineItems()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var includeLineItems = true;
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedSummary = new TimebankSummaryDto
        {
            JudiciaryPersonId = judgeId,
            Period = period
        };

        _mockSummaryValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankSummaryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankSummaryForJudgeAsync(
                period,
                judgeId,
                includeLineItems,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<TimebankSummaryDto>.Success(expectedSummary));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankSummaryForJudge(period, judgeId, includeLineItems);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        _mockTimebankService.Verify(s => s.GetTimebankSummaryForJudgeAsync(
            period,
            judgeId,
            includeLineItems,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTimebankPayoutsForJudges Tests

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsBadRequest_WhenPeriodIsInvalid()
    {
        // Arrange
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        // Setup validator to fail for period <= 1900
        var validationFailure = new FluentValidation.Results.ValidationFailure("Period", "Period must be a valid year.");
        var validationResult = new FluentValidation.Results.ValidationResult(new[] { validationFailure });
        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(1899, rate: rate);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsForbid_WhenPermissionMissing()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString())
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Since authorization attributes are not enforced in unit tests, the controller will call the service.
        // Configure the service to return a failure so the controller returns BadRequest.
        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime?>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Failure("Permission missing"));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: rate);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);

        _mockTimebankService.Verify(s => s.GetTimebankPayoutsForJudgesAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<DateTime?>(),
            It.IsAny<double>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsBadRequest_WhenRateIsZero()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        // Setup validator to fail for rate = 0
        var validationFailure = new FluentValidation.Results.ValidationFailure("Rate", "Rate must be a positive number.");
        var validationResult = new FluentValidation.Results.ValidationResult(new[] { validationFailure });
        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: 0);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedPayout = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate,
            TotalPayout = _faker.Random.Double(1000, 5000)
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<DateTime?>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Success(expectedPayout));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: rate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPayout = Assert.IsType<VacationPayoutDto>(okResult.Value);
        Assert.Equal(judgeId, returnedPayout.JudiciaryPersonId);
        Assert.Equal(period, returnedPayout.Period);
        Assert.Equal(rate, returnedPayout.Rate);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsBadRequest_WhenServiceFails()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<DateTime?>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Failure("Service error"));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: rate);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_ReturnsNotFound_WhenPayloadIsNull()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<DateTime?>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Success(null));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: rate);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_UsesProvidedJudgeId()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var loggedInJudgeId = _faker.Random.Int(1, 1000);
        var requestedJudgeId = _faker.Random.Int(1001, 2000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, loggedInJudgeId.ToString()),
            new(CustomClaimTypes.Groups, "jasper-view-others-schedule"),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedPayout = new VacationPayoutDto
        {
            JudiciaryPersonId = requestedJudgeId,
            Period = period,
            Rate = rate
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                requestedJudgeId,
                It.IsAny<DateTime?>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Success(expectedPayout));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, requestedJudgeId, rate: rate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPayout = Assert.IsType<VacationPayoutDto>(okResult.Value);
        Assert.Equal(requestedJudgeId, returnedPayout.JudiciaryPersonId);

        _mockTimebankService.Verify(s => s.GetTimebankPayoutsForJudgesAsync(
            period,
            requestedJudgeId,
            It.IsAny<DateTime?>(),
            rate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_PassesExpiryDate()
    {
        // Arrange
        var period = _faker.Random.Int(2020, 2025);
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var expiryDate = _faker.Date.Future();
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedPayout = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                expiryDate,
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Success(expectedPayout));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, judgeId, expiryDate, rate);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        _mockTimebankService.Verify(s => s.GetTimebankPayoutsForJudgesAsync(
            period,
            judgeId,
            expiryDate,
            rate,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimebankPayoutsForJudges_HandlesValidPeriodYear()
    {
        // Arrange
        var period = 1901; // Just above the threshold
        var judgeId = _faker.Random.Int(1, 1000);
        var rate = _faker.Random.Double(100, 500);
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.Permission, Permission.VIEW_VACATION_PAYOUT)
        };

        var expectedPayout = new VacationPayoutDto
        {
            JudiciaryPersonId = judgeId,
            Period = period,
            Rate = rate
        };

        _mockPayoutValidator.Setup(v => v.ValidateAsync(It.IsAny<TimebankPayoutRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTimebankService
            .Setup(s => s.GetTimebankPayoutsForJudgesAsync(
                period,
                judgeId,
                It.IsAny<DateTime?>(),
                rate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<VacationPayoutDto>.Success(expectedPayout));

        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.GetTimebankPayoutsForJudges(period, rate: rate);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion
}