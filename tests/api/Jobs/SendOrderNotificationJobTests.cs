using System;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Jobs;
using Scv.Api.Models;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Models.Order;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Jobs;

public class SendOrderNotificationJobTests
{
    private readonly Faker _faker;
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<SendOrderNotificationJob>> _mockLogger;
    private readonly SendOrderNotificationJob _job;

    public SendOrderNotificationJobTests()
    {
        _faker = new Faker();
        _mockDashboardService = new Mock<IDashboardService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<SendOrderNotificationJob>>();

        _job = new SendOrderNotificationJob(
            _mockDashboardService.Object,
            _mockEmailTemplateService.Object,
            _mockUserService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Execute_SendsEmail_WhenAllDataIsValid()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderDto = CreateValidOrderDto(judgeId);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        _mockEmailTemplateService.Setup(s => s.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute(orderDto);

        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            "Order Received",
            judgeEmail,
            It.Is<object>(data =>
                data.GetType().GetProperty("CourtFileNumber").GetValue(data).ToString() == orderDto.CourtFile.FullFileNo)),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Notification sent to judge")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeIdIsNull()
    {
        var orderDto = CreateValidOrderDto(null);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Cannot send notification - no judge assigned")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockDashboardService.Verify(s => s.GetJudge(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeNotFound()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync((Scv.Api.Models.Person)null);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Judge with id {judgeId} not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockUserService.Verify(s => s.GetByJudgeIdAsync(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_SkipsNotification_WhenJudgeIsInactive()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        var inactiveJudge = CreateInactiveJudge(judgeId);

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(inactiveJudge);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Judge {judgeId} is inactive")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockUserService.Verify(s => s.GetByJudgeIdAsync(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenDatabaseUserNotFound()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        var judge = CreateActiveJudge(judgeId);

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync((UserDto)null);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"No database user found for judge {judgeId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeEmailIsEmpty()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = string.Empty,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Judge {judgeId} has no email address")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeEmailIsNull()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = null,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Judge {judgeId} has no email address")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_SendsEmailWithCorrectTemplateData()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var courtFileNumber = _faker.Random.AlphaNumeric(10);
        var styleOfCause = $"{_faker.Name.LastName()} vs {_faker.Name.LastName()}";
        var referralNotes = _faker.Lorem.Sentence();
        var referredBy = _faker.Name.FullName();

        var orderDto = new OrderDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 9999),
                FullFileNo = courtFileNumber,
                StyleOfCause = styleOfCause
            },
            Referral = new ReferralDto
            {
                SentToPartId = judgeId,
                ReferralNotesTxt = referralNotes,
                ReferredByName = referredBy
            }
        };

        var judge = new Scv.Api.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = firstName,
                    LastName = lastName
                }
            ]
        };

        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = firstName,
            LastName = lastName,
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        object capturedEmailData = null;
        _mockEmailTemplateService.Setup(s => s.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Callback<string, string, object>((template, email, data) => capturedEmailData = data)
            .Returns(Task.CompletedTask);

        await _job.Execute(orderDto);

        Assert.NotNull(capturedEmailData);
        var emailDataType = capturedEmailData.GetType();
        Assert.Equal($"{firstName} {lastName}", emailDataType.GetProperty("JudgeName").GetValue(capturedEmailData));
        Assert.Equal(courtFileNumber, emailDataType.GetProperty("CourtFileNumber").GetValue(capturedEmailData));
        Assert.Equal(styleOfCause, emailDataType.GetProperty("StyleOfCause").GetValue(capturedEmailData));
        Assert.Equal(referralNotes, emailDataType.GetProperty("ReferralNotes").GetValue(capturedEmailData));
        Assert.Equal(referredBy, emailDataType.GetProperty("ReferredBy").GetValue(capturedEmailData));
    }

    [Fact]
    public async Task Execute_UsesJudgeFallback_WhenNamesCollectionIsEmpty()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderDto = CreateValidOrderDto(judgeId);

        var judge = new Scv.Api.Models.Person
        {
            Id = judgeId,
            Names = []
        };

        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        object capturedEmailData = null;
        _mockEmailTemplateService.Setup(s => s.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Callback<string, string, object>((template, email, data) => capturedEmailData = data)
            .Returns(Task.CompletedTask);

        await _job.Execute(orderDto);

        Assert.NotNull(capturedEmailData);
        var emailDataType = capturedEmailData.GetType();
        Assert.Equal("Judge", emailDataType.GetProperty("JudgeName").GetValue(capturedEmailData));
    }

    [Fact]
    public async Task Execute_LogsError_AndThrows_WhenExceptionOccurs()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateValidOrderDto(judgeId);

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.Execute(orderDto));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Failed to send order notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_LogsProcessingStart()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var physicalFileId = _faker.Random.Int(1, 9999);
        var orderDto = CreateValidOrderDto(judgeId);
        orderDto.CourtFile.PhysicalFileId = physicalFileId;

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync((Scv.Api.Models.Person)null);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Processing order notification job for file {physicalFileId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_LogsProcessingComplete()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var physicalFileId = _faker.Random.Int(1, 9999);
        var judgeEmail = _faker.Internet.Email();
        var orderDto = CreateValidOrderDto(judgeId);
        orderDto.CourtFile.PhysicalFileId = physicalFileId;

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockDashboardService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        _mockEmailTemplateService.Setup(s => s.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Order notification job completed for file {physicalFileId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private OrderDto CreateValidOrderDto(int? judgeId)
    {
        return new OrderDto
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 9999),
                FullFileNo = _faker.Random.AlphaNumeric(10),
                StyleOfCause = $"{_faker.Name.LastName()} vs {_faker.Name.LastName()}"
            },
            Referral = new ReferralDto
            {
                SentToPartId = judgeId,
                ReferralNotesTxt = _faker.Lorem.Sentence(),
                ReferredByName = _faker.Name.FullName()
            }
        };
    }

    private Scv.Api.Models.Person CreateActiveJudge(int judgeId)
    {
        return new Scv.Api.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = _faker.Name.FirstName(),
                    LastName = _faker.Name.LastName()
                }
            ],
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Active",
                    EffDate = DateTime.Now.AddMonths(-1)
                }
            ]
        };
    }

    private Scv.Api.Models.Person CreateInactiveJudge(int judgeId)
    {
        return new Scv.Api.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = _faker.Name.FirstName(),
                    LastName = _faker.Name.LastName()
                }
            ],
            Statuses =
            [
                new PersonStatus
                {
                    StatusDescription = "Inactive",
                    EffDate = DateTime.Now.AddMonths(-1)
                }
            ]
        };
    }
}
