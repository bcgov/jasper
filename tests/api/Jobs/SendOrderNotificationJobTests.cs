using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Scv.Api.SignalR.Notifications;
using Scv.Models;
using Scv.Models.AccessControlManagement;
using Scv.Models.Order;
using Xunit;

namespace tests.api.Jobs;

public class SendOrderNotificationJobTests
{
    private readonly Faker _faker;
    private readonly Mock<IJudgeService> _mockJudgeService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<OrderReceivedAckNotification>> _mockOrderReceivedAckLogger;
    private readonly Mock<ILogger<SendOrderNotificationJob>> _mockLogger;
    private readonly OrderReceivedAckNotification _orderReceivedAck;
    private readonly SendOrderNotificationJob _job;

    public SendOrderNotificationJobTests()
    {
        _faker = new Faker();
        _mockJudgeService = new Mock<IJudgeService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockUserService = new Mock<IUserService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockOrderReceivedAckLogger = new Mock<ILogger<OrderReceivedAckNotification>>();
        _mockLogger = new Mock<ILogger<SendOrderNotificationJob>>();

        _mockNotificationService.Setup(s => s.NotifyUserWithAckAsync(
            It.IsAny<string>(),
            It.IsAny<NotificationDto<OrderReceivedNotificationPayload>>()))
            .Returns(Task.CompletedTask);

        _orderReceivedAck = new OrderReceivedAckNotification(
            _mockNotificationService.Object,
            _mockOrderReceivedAckLogger.Object);

        // No notification filters are configured, so every order is emailed.
        _job = CreateJob();
    }

    private SendOrderNotificationJob CreateJob(Dictionary<string, string> settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? [])
            .Build();

        return new SendOrderNotificationJob(
            _mockJudgeService.Object,
            _mockEmailTemplateService.Object,
            _mockUserService.Object,
            _orderReceivedAck,
            _mockLogger.Object,
            configuration);
    }

    private UserDto SetupJudgeAndUser(int judgeId, string email)
    {
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = email,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(CreateActiveJudge(judgeId));
        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);
        _mockEmailTemplateService.Setup(s => s.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        return databaseUser;
    }

    [Fact]
    public async Task Execute_SendsEmail_WhenAllDataIsValid()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
                data.GetType().GetProperty("CaseFileNumber").GetValue(data).ToString() == orderRequestDto.CourtFileNo)),
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
    public async Task Execute_LogsInformation_AndSkips_WhenOrderDoesNotMatchPriorityFilter()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var physicalFileId = _faker.Random.Int(1, 9999);
        var orderRequestDto = CreateValidOrderRequestDto(priorityType: null);
        orderRequestDto.PhysicalFileId = physicalFileId;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);
        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync(judge);

        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Email = _faker.Person.Email,
            JudgeId = judgeId
        };

        _mockUserService.Setup(s => s.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(databaseUser);

        var job = CreateJob(PriorityFilter("PRO,CRTD,OTHR"));
        await job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Order for file {physicalFileId} does not match the NOTIFY_PRIORITY_TYPES notification filter - skipping email notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockJudgeService.Verify(s => s.GetJudge(It.IsAny<int>()), Times.Once);
        _mockUserService.Verify(s => s.GetByJudgeIdAsync(It.IsAny<int>()), Times.Once);
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    private static Dictionary<string, string> PriorityFilter(string value) =>
        new() { [OrderNotificationFilter.PriorityTypesKey] = value };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Execute_SendsEmail_ForNonPriorityOrder_WhenNoPriorityFilterIsConfigured(string value)
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderDto = CreateOrderDto(judgeId, CreateValidOrderRequestDto(priorityType: null));
        SetupJudgeAndUser(judgeId, judgeEmail);

        var job = CreateJob(value == null ? null : PriorityFilter(value));
        await job.Execute(orderDto);

        VerifyEmailSent(judgeEmail);
    }

    [Theory]
    [InlineData("PRO")]
    [InlineData("pro, CRTD")]
    [InlineData("PRO,CRTD,OTHR")]
    public async Task Execute_SendsEmail_WhenPriorityTypeIsInFilter(string filter)
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderDto = CreateOrderDto(judgeId, CreateValidOrderRequestDto(PriorityTypeDescriptor.PROTECTION_ORDER_TYPE));
        SetupJudgeAndUser(judgeId, judgeEmail);

        var job = CreateJob(PriorityFilter(filter));
        await job.Execute(orderDto);

        VerifyEmailSent(judgeEmail);
    }

    [Fact]
    public async Task Execute_SkipsEmail_WhenPriorityTypeIsNotInFilter()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateOrderDto(judgeId, CreateValidOrderRequestDto(PriorityTypeDescriptor.OTHER_ORDER_TYPE));
        SetupJudgeAndUser(judgeId, _faker.Internet.Email());

        var job = CreateJob(PriorityFilter("PRO"));
        await job.Execute(orderDto);

        VerifyEmailNotSent();
    }

    [Fact]
    public async Task Execute_SkipsEmail_WhenPriorityFilterIsSetAndReferralIsNull()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.Referral = null;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);
        SetupJudgeAndUser(judgeId, _faker.Internet.Email());

        var job = CreateJob(PriorityFilter("PRO"));
        await job.Execute(orderDto);

        VerifyEmailNotSent();
    }

    [Theory]
    [InlineData("Order", "PSC", true)]
    [InlineData("Order", "PFA", true)]
    [InlineData("Order", "PSM", false)]
    [InlineData("Order", "PFM", false)]
    [InlineData("Desk Order", "PSM", true)]
    [InlineData("Desk Order", "PFM", true)]
    [InlineData("Desk Order", "PSC", false)]
    [InlineData("Order,Desk Order", "PSC", true)]
    [InlineData("Order,Desk Order", "PSM", true)]
    [InlineData("Order", null, false)]
    [InlineData("Order", "XYZ", false)]
    public async Task Execute_AppliesOrderTypeFilter(string filter, string courtListTypeCd, bool expectEmail)
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.Referral.CourtListTypeCd = courtListTypeCd;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);
        SetupJudgeAndUser(judgeId, judgeEmail);

        var job = CreateJob(new() { [OrderNotificationFilter.OrderTypesKey] = filter });
        await job.Execute(orderDto);

        VerifyEmail(expectEmail, judgeEmail);
    }

    [Theory]
    [InlineData("F", "F", true)]
    [InlineData("F", "C", false)]
    [InlineData("F", "M", false)]
    [InlineData("C", "C", true)]
    [InlineData("C", "M", false)]
    [InlineData("C", "L", false)]
    [InlineData("F,C", "F", true)]
    [InlineData("F,C", "C", true)]
    [InlineData("F,C", "A", false)]
    [InlineData("F", "INVALID", false)]
    public async Task Execute_AppliesCourtClassFilter(string filter, string courtClassCd, bool expectEmail)
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtClassCd = courtClassCd;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);
        SetupJudgeAndUser(judgeId, judgeEmail);

        var job = CreateJob(new() { [OrderNotificationFilter.CourtClassesKey] = filter });
        await job.Execute(orderDto);

        VerifyEmail(expectEmail, judgeEmail);
    }

    [Theory]
    [InlineData("PRO", "Order", "F", "PFA", "F", true)]
    [InlineData("PRO", "Order", "F", "PSM", "F", false)]
    [InlineData("PRO", "Order", "F", "PFA", "C", false)]
    [InlineData("CRTD", "Order", "F", "PFA", "F", false)]
    [InlineData("PRO", "Desk Order", "C", "PSM", "C", true)]
    public async Task Execute_RequiresAllFiltersToMatch(
        string priority, string orderType, string courtClass, string courtListTypeCd, string courtClassCd, bool expectEmail)
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto(PriorityTypeDescriptor.PROTECTION_ORDER_TYPE);
        orderRequestDto.Referral.CourtListTypeCd = courtListTypeCd;
        orderRequestDto.CourtClassCd = courtClassCd;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);
        SetupJudgeAndUser(judgeId, judgeEmail);

        var job = CreateJob(new()
        {
            [OrderNotificationFilter.PriorityTypesKey] = priority,
            [OrderNotificationFilter.OrderTypesKey] = orderType,
            [OrderNotificationFilter.CourtClassesKey] = courtClass
        });
        await job.Execute(orderDto);

        VerifyEmail(expectEmail, judgeEmail);
    }

    [Fact]
    public async Task Execute_LogsWarning_AndSkips_WhenFilterValueIsUnrecognized()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateOrderDto(judgeId, CreateValidOrderRequestDto());
        SetupJudgeAndUser(judgeId, _faker.Internet.Email());

        var job = CreateJob(new() { [OrderNotificationFilter.CourtClassesKey] = "Criminal" });
        await job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("NOTIFY_COURT_CLASSES=Criminal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        VerifyEmailNotSent();
    }

    [Fact]
    public async Task Execute_StillSendsAcknowledgement_WhenEmailIsSkippedByFilter()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderDto = CreateOrderDto(judgeId, CreateValidOrderRequestDto(priorityType: null));
        var databaseUser = SetupJudgeAndUser(judgeId, _faker.Internet.Email());

        var job = CreateJob(PriorityFilter("PRO"));
        await job.Execute(orderDto);

        _mockNotificationService.Verify(s => s.NotifyUserWithAckAsync(
            databaseUser.Id,
            It.IsAny<NotificationDto<OrderReceivedNotificationPayload>>()), Times.Once);
        VerifyEmailNotSent();
    }

    private void VerifyEmail(bool expectEmail, string judgeEmail)
    {
        if (expectEmail)
        {
            VerifyEmailSent(judgeEmail);
        }
        else
        {
            VerifyEmailNotSent();
        }
    }

    private void VerifyEmailSent(string judgeEmail) =>
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            "Order Received",
            judgeEmail,
            It.IsAny<object>()), Times.Once);

    private void VerifyEmailNotSent() =>
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeIdIsNull()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(null, orderRequestDto);

        await _job.Execute(orderDto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Cannot send notification - no judge assigned")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockJudgeService.Verify(s => s.GetJudge(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(s => s.SendEmailTemplateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Execute_LogsWarning_WhenJudgeNotFound()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync((Scv.Models.Person)null);

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
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var inactiveJudge = CreateInactiveJudge(judgeId);

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = string.Empty,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = null,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        var referralNotes = _faker.Lorem.Sentence();
        var referredBy = _faker.Name.FullName();

        var orderRequestDto = new OrderRequestDto
        {
            PhysicalFileId = _faker.Random.Int(1, 9999),
            CourtFileNo = courtFileNumber,
            Referral = new ReferralDto
            {
                SentToPartId = judgeId,
                ReferralNotesTxt = referralNotes,
                ReferredByName = referredBy,
                PriorityType = PriorityTypeDescriptor.PROTECTION_ORDER_TYPE
            }
        };

        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = new Scv.Models.Person
        {
            Id = judgeId,
            Names =
        [
            new PersonName
                {
                    FirstName = firstName,
                    LastName = lastName,
                    EffDate = DateTime.Today.AddDays(-1)
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

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        Assert.Equal(courtFileNumber, emailDataType.GetProperty("CaseFileNumber").GetValue(capturedEmailData));
        Assert.Equal(referralNotes, emailDataType.GetProperty("ReferralNotes").GetValue(capturedEmailData));
        Assert.Equal(referredBy, emailDataType.GetProperty("ReferredBy").GetValue(capturedEmailData));
    }

    [Fact]
    public async Task Execute_UsesJudgeFallback_WhenNamesCollectionIsEmpty()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = new Scv.Models.Person
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

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
    public async Task Execute_UsesEffectiveJudgeName_AndFormatsWithoutExtraSpaces()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var judgeEmail = _faker.Internet.Email();
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = new Scv.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = "Expired",
                    LastName = "Name",
                    EffDate = DateTime.Today.AddDays(-10),
                    ExpDate = DateTime.Today.AddDays(-1)
                },
                new PersonName
                {
                    FirstName = "",
                    LastName = "Current",
                    EffDate = DateTime.Today.AddDays(-2),
                    ExpDate = null
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

        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        Assert.Equal("Current", emailDataType.GetProperty("JudgeName").GetValue(capturedEmailData));
        Assert.Equal("Current", emailDataType.GetProperty("LastName").GetValue(capturedEmailData));
    }

    [Fact]
    public async Task Execute_LogsError_AndThrows_WhenExceptionOccurs()
    {
        var judgeId = _faker.Random.Int(1, 1000);
        var orderRequestDto = CreateValidOrderRequestDto();
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.PhysicalFileId = physicalFileId;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
            .ReturnsAsync((Scv.Models.Person)null);

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
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.PhysicalFileId = physicalFileId;
        var orderDto = CreateOrderDto(judgeId, orderRequestDto);

        var judge = CreateActiveJudge(judgeId);
        var databaseUser = new UserDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            FirstName = "Test",
            LastName = "Judge",
            Email = judgeEmail,
            JudgeId = judgeId
        };

        _mockJudgeService.Setup(s => s.GetJudge(judgeId))
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

    private OrderRequestDto CreateValidOrderRequestDto(string priorityType = PriorityTypeDescriptor.PROTECTION_ORDER_TYPE)
    {
        return new OrderRequestDto
        {
            PhysicalFileId = _faker.Random.Int(1, 100),
            CourtFileNo = _faker.Random.AlphaNumeric(10),
            CourtLocationDesc = _faker.Address.City(),
            Referral = new ReferralDto
            {
                SentToPartId = _faker.Random.Double(),
                ReferralNotesTxt = _faker.Lorem.Sentence(),
                ReferredByName = _faker.Name.FullName(),
                PriorityType = priorityType
            },
        };
    }

    private OrderDto CreateOrderDto(int? judgeId, OrderRequestDto orderRequestDto)
    {
        return new OrderDto
        {
            Id = _faker.Random.AlphaNumeric(24),
            JudgeId = judgeId ?? default,
            OrderRequest = orderRequestDto
        };
    }

    private Scv.Models.Person CreateActiveJudge(int judgeId)
    {
        return new Scv.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = _faker.Name.FirstName(),
                    LastName = _faker.Name.LastName(),
                    EffDate = DateTime.Today.AddDays(-1)
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

    private Scv.Models.Person CreateInactiveJudge(int judgeId)
    {
        return new Scv.Models.Person
        {
            Id = judgeId,
            Names =
            [
                new PersonName
                {
                    FirstName = _faker.Name.FirstName(),
                    LastName = _faker.Name.LastName(),
                    EffDate = DateTime.Today.AddDays(-1)
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
