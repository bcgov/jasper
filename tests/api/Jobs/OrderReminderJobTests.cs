using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Jobs;
using Scv.Models.AccessControlManagement;
using Scv.Models.Order;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using tests.api.Services;
using Xunit;

namespace tests.api.Jobs;

public class OrderReminderJobTests : ServiceTestBase
{
    private readonly Faker _faker;
    private readonly Mock<ILogger<OrderReminderJob>> _logger;
    private readonly Mock<IAppCache> _mockCache;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IRepositoryBase<Order>> _mockOrderRepo;
    private readonly Mock<IJudgeService> _mockJudgeService;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly IOptions<JobsOrderReminderOptions> _options;
    private readonly OrderReminderJob _job;

    public OrderReminderJobTests()
    {
        _faker = new Faker();
        _logger = new Mock<ILogger<OrderReminderJob>>();
        _mockCache = new Mock<IAppCache>();
        _mockMapper = new Mock<IMapper>();
        _mockOrderRepo = new Mock<IRepositoryBase<Order>>();
        _mockJudgeService = new Mock<IJudgeService>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _mockUserService = new Mock<IUserService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _options = Options.Create(new JobsOrderReminderOptions
        {
            CronSchedule = "0 0 * * *",
        });

        _job = new OrderReminderJob(
            _mockConfiguration.Object,
            _mockCache.Object,
            _mockMapper.Object,
            _logger.Object,
            _mockOrderRepo.Object,
            _mockJudgeService.Object,
            _mockEmailTemplateService.Object,
            _mockUserService.Object,
            _options
        );
    }

    private Order CreateTestOrder(int judgeId, DateTime entryDate, string priorityType = "High")
    {
        return new Order
        {
            Id = _faker.Random.Guid().ToString(),
            Status = OrderStatus.Pending,
            SubmitStatus = SubmitStatus.Pending,
            Ent_Dtm = entryDate,
            ReminderNotificationsSent = 0,
            ReassignmentNotificationsSent = 0,
            OrderRequest = new OrderRequest
            {
                CourtFile = new CourtFile
                {
                    CourtFileNo = _faker.Random.AlphaNumeric(10),
                    CourtLocationDesc = _faker.Random.Int(1, 100),
                    StyleOfCause = "Test v. Case"
                },
                Referral = new Referral
                {
                    SentToPartId = judgeId,
                    SentToName = "Test Judge",
                    PriorityType = priorityType
                }
            }
        };
    }

    private Scv.Api.Models.Person CreateTestJudge(int judgeId)
    {
        return new Scv.Api.Models.Person
        {
            UserId = judgeId,
            HomeLocationId = 123,
            Names =
            [
                new()
                {
                    FirstName = _faker.Name.FirstName(),
                    LastName = _faker.Name.LastName()
                }
            ]
        };
    }

    private PersonSearchItem CreateTestRAJ(int rajId)
    {
        return new PersonSearchItem
        {
            UserId = rajId,
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            PositionCode = "RAJ",
            FullName = "RAJ Test"
        };
    }

    private UserDto CreateTestUser(int judgeId, string email)
    {
        return new UserDto
        {
            JudgeId = judgeId,
            Email = email,
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName()
        };
    }

    #region Execute Tests

    [Fact]
    public async Task Execute_NoUnresolvedOrders_LogsAndReturns()
    {
        SetupConfiguration("5", "10");
        
        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockOrderRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    private void SetupConfiguration(string reminderDays, string reassignmentDays, string maxReminders = "1", string maxReassignments = "1", string supportAccount = "support@example.com")
    {
        var reminderSection = new Mock<IConfigurationSection>();
        reminderSection.Setup(s => s.Value).Returns(reminderDays);
        
        var reassignmentSection = new Mock<IConfigurationSection>();
        reassignmentSection.Setup(s => s.Value).Returns(reassignmentDays);
        
        var maxRemindersSection = new Mock<IConfigurationSection>();
        maxRemindersSection.Setup(s => s.Value).Returns(maxReminders);
        
        var maxReassignmentsSection = new Mock<IConfigurationSection>();
        maxReassignmentsSection.Setup(s => s.Value).Returns(maxReassignments);
        
        var supportAccountSection = new Mock<IConfigurationSection>();
        supportAccountSection.Setup(s => s.Value).Returns(supportAccount);
        
        _mockConfiguration
            .Setup(c => c.GetSection("ORDER_REMINDER_THRESHOLD_DAYS"))
            .Returns(reminderSection.Object);
        
        _mockConfiguration
            .Setup(c => c.GetSection("ORDER_REASSIGNMENT_THRESHOLD_DAYS"))
            .Returns(reassignmentSection.Object);
        
        _mockConfiguration
            .Setup(c => c.GetSection("ORDER_MAX_REMINDER_NOTIFICATIONS"))
            .Returns(maxRemindersSection.Object);
        
        _mockConfiguration
            .Setup(c => c.GetSection("ORDER_MAX_REASSIGNMENT_NOTIFICATIONS"))
            .Returns(maxReassignmentsSection.Object);
        
        _mockConfiguration
            .Setup(c => c.GetSection("SUPPORT_ACCOUNT"))
            .Returns(supportAccountSection.Object);
    }

    [Fact]
    public async Task Execute_WithReminderOrders_SendsReminders()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6); // Past reminder threshold (5 days)
        var order = CreateTestOrder(judgeId, reminderDate);
        var judge = CreateTestJudge(judgeId);
        var user = CreateTestUser(judgeId, "judge@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(user);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(
                "Order Reminder",
                user.Email,
                It.Is<object>(obj => 
                    obj.GetType().GetProperty("JudgeName") != null &&
                    obj.GetType().GetProperty("CaseFileNumber") != null &&
                    obj.GetType().GetProperty("DateReceived") != null &&
                    obj.GetType().GetProperty("LocationName") != null &&
                    obj.GetType().GetProperty("Priority") != null &&
                    obj.GetType().GetProperty("SupportAccount") != null
                )),
            Times.Once
        );

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => o.ReminderNotificationsSent == 1)),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_WithReassignmentOrders_ReassignsToRAJ()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11); // Past reassignment threshold (10 days)
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);
        var rajUser = CreateTestUser(rajId, "raj@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.Is<List<string>>(l => l.Contains("RAJ")),
                It.Is<List<string>>(l => l.Contains("123"))))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => 
                o.OrderRequest.Referral.SentToPartId == rajId &&
                o.OrderRequest.Referral.SentToName == $"{raj.FirstName} {raj.LastName}".Trim())),
            Times.AtLeastOnce
        );

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.IsAny<Order>()),
            Times.AtLeast(2)
        );

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(
                "Order Reassignment",
                rajUser.Email,
                It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_UsesDefaultThresholds_WhenConfigurationMissing()
    {
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);

        // Setup config to return null/empty strings to trigger the default behavior in TryParse
        SetupConfiguration(null, null);

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        await Assert.ThrowsAsync<Scv.Core.Helpers.Exceptions.ConfigurationException>(async () =>
        {
            await _job.Execute();
        });
    }

    #endregion

    #region SendReminderToJudge Tests

    [Fact]
    public async Task Execute_SendReminder_SkipsWhenNoJudgeAssigned()
    {
        SetupConfiguration("5", "10");
        
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(101, reminderDate);
        order.OrderRequest.Referral.SentToPartId = null; // No judge assigned

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        await _job.Execute();

        _mockJudgeService.Verify(j => j.GetJudge(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_SendReminder_SkipsWhenJudgeNotFound()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync((Scv.Api.Models.Person)null);

        await _job.Execute();

        _mockUserService.Verify(u => u.GetByJudgeIdAsync(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_SendReminder_SkipsWhenUserNotFound()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);
        var judge = CreateTestJudge(judgeId);

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync((UserDto)null);

        await _job.Execute();

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_SendReminder_SkipsWhenUserHasNoEmail()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);
        var judge = CreateTestJudge(judgeId);
        var user = CreateTestUser(judgeId, null); // No email

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(user);

        await _job.Execute();

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_SendReminder_IncludesCorrectEmailData()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var expectedPriority = "Urgent";
        var expectedCaseNumber = "TEST-123";
        var expectedLocation = 123;
        
        var order = CreateTestOrder(judgeId, reminderDate, expectedPriority);
        order.OrderRequest.CourtFile.CourtFileNo = expectedCaseNumber;
        order.OrderRequest.CourtFile.CourtLocationDesc = expectedLocation;

        var judge = CreateTestJudge(judgeId);
        var expectedJudgeName = $"{judge.Names.First().FirstName} {judge.Names.First().LastName}".Trim();
        var user = CreateTestUser(judgeId, "judge@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(user);

        object capturedEmailData = null;
        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Callback<string, string, object>((template, email, data) => capturedEmailData = data)
            .Returns(Task.CompletedTask);

        await _job.Execute();

        Assert.NotNull(capturedEmailData);
        var dataType = capturedEmailData.GetType();
        
        var judgeNameProp = dataType.GetProperty("JudgeName");
        var caseFileNumberProp = dataType.GetProperty("CaseFileNumber");
        var dateReceivedProp = dataType.GetProperty("DateReceived");
        var locationNameProp = dataType.GetProperty("LocationName");
        var priorityProp = dataType.GetProperty("Priority");
        var supportAccountProp = dataType.GetProperty("SupportAccount");

        Assert.Equal(expectedJudgeName, judgeNameProp?.GetValue(capturedEmailData));
        Assert.Equal(expectedCaseNumber, caseFileNumberProp?.GetValue(capturedEmailData));
        Assert.Equal(expectedLocation, locationNameProp?.GetValue(capturedEmailData));
        Assert.Equal(expectedPriority, priorityProp?.GetValue(capturedEmailData));
        Assert.NotNull(dateReceivedProp?.GetValue(capturedEmailData));
        Assert.NotNull(supportAccountProp?.GetValue(capturedEmailData));
    }

    #endregion

    #region ReassignOrderToRAJ Tests

    [Fact]
    public async Task Execute_ReassignOrder_SkipsWhenNoRAJFound()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_UpdatesOrderWithRAJDetails()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);
        var rajUser = CreateTestUser(rajId, "raj@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.Is<List<string>>(l => l.Contains("RAJ")),
                It.Is<List<string>>(l => l.Contains(judge.HomeLocationId.ToString()))))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        var expectedRajName = $"{raj.FirstName} {raj.LastName}".Trim();
        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => 
                o.OrderRequest.Referral.SentToPartId == rajId &&
                o.OrderRequest.Referral.SentToName == expectedRajName)),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_SendsEmailToRAJ()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);
        var rajUser = CreateTestUser(rajId, "raj@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(
                "Order Reassignment",
                rajUser.Email,
                It.Is<object>(obj => 
                    obj.GetType().GetProperty("JudgeName") != null &&
                    obj.GetType().GetProperty("CaseFileNumber") != null &&
                    obj.GetType().GetProperty("LocationName") != null &&
                    obj.GetType().GetProperty("DateReceived") != null &&
                    obj.GetType().GetProperty("Priority") != null &&
                    obj.GetType().GetProperty("SupportAccount") != null
                )),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_SkipsEmailWhenRAJUserNotFound()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync((UserDto)null);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_SkipsEmailWhenRAJUserHasNoEmail()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);
        var rajUser = CreateTestUser(rajId, ""); // Empty email

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_SendReminder_SkipsWhenMaxRemindersReached()
    {
        SetupConfiguration("5", "10", "2", "1");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);
        order.ReminderNotificationsSent = 2; // Already at max

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        await _job.Execute();

        _mockJudgeService.Verify(j => j.GetJudge(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_SkipsWhenMaxReassignmentsReached()
    {
        SetupConfiguration("5", "10", "1", "2");
        
        var judgeId = 101;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        order.ReassignmentNotificationsSent = 2; // Already at max

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        await _job.Execute();

        _mockJudgeService.Verify(j => j.GetJudge(It.IsAny<int>()), Times.Never);
        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()),
            Times.Never
        );
    }

    #endregion

    #region GetRAJForJudge Tests

    [Fact]
    public async Task Execute_GetRAJForJudge_CallsJudgeServiceWithCorrectParameters()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        judge.HomeLocationId = 456;

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([]);

        await _job.Execute();

        _mockJudgeService.Verify(
            j => j.GetJudges(
                It.Is<List<string>>(l => l.Contains("RAJ")),
                It.Is<List<string>>(l => l.Contains("456"))),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_GetRAJForJudge_ReturnsFirstRAJ_WhenMultipleFound()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var raj1Id = 201;
        var raj2Id = 202;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj1 = CreateTestRAJ(raj1Id);
        var raj2 = CreateTestRAJ(raj2Id);
        var rajUser = CreateTestUser(raj1Id, "raj1@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj1, raj2]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(raj1Id))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => o.OrderRequest.Referral.SentToPartId == raj1Id)),
            Times.AtLeastOnce
        );
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public async Task Execute_GetJudgeName_HandlesJudgeWithoutNames()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order = CreateTestOrder(judgeId, reminderDate);
        var judge = CreateTestJudge(judgeId);
        judge.Names = null; // No names
        var user = CreateTestUser(judgeId, "judge@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId))
            .ReturnsAsync(user);

        object capturedEmailData = null;
        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Callback<string, string, object>((template, email, data) => capturedEmailData = data)
            .Returns(Task.CompletedTask);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        var judgeNameProp = capturedEmailData?.GetType().GetProperty("JudgeName");
        Assert.NotNull(capturedEmailData);
        Assert.Equal("Judge", judgeNameProp?.GetValue(capturedEmailData));
    }

    [Fact]
    public async Task Execute_GetRajName_FormatsNameCorrectly()
    {
        SetupConfiguration("5", "10");
        
        var judgeId = 101;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order = CreateTestOrder(judgeId, reassignmentDate);
        var judge = CreateTestJudge(judgeId);
        var raj = CreateTestRAJ(rajId);
        raj.FirstName = "John";
        raj.LastName = "Doe";
        var rajUser = CreateTestUser(rajId, "raj@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order]);

        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId))
            .ReturnsAsync(judge);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => o.OrderRequest.Referral.SentToName == "John Doe")),
            Times.AtLeastOnce
        );
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Execute_SendReminder_ContinuesOnException()
    {
        SetupConfiguration("5", "10");
        
        var judgeId1 = 101;
        var judgeId2 = 102;
        var reminderDate = DateTime.UtcNow.AddDays(-6);
        var order1 = CreateTestOrder(judgeId1, reminderDate);
        var order2 = CreateTestOrder(judgeId2, reminderDate);
        var judge2 = CreateTestJudge(judgeId2);
        var user2 = CreateTestUser(judgeId2, "judge2@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order1, order2]);

        // First judge throws exception
        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId1))
            .ThrowsAsync(new Exception("Test exception"));

        // Second judge succeeds
        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId2))
            .ReturnsAsync(judge2);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(judgeId2))
            .ReturnsAsync(user2);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockEmailTemplateService.Verify(
            e => e.SendEmailTemplateAsync(
                "Order Reminder",
                "judge2@example.com",
                It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Execute_ReassignOrder_ContinuesOnException()
    {
        SetupConfiguration("5", "10");
        
        var judgeId1 = 101;
        var judgeId2 = 102;
        var rajId = 201;
        var reassignmentDate = DateTime.UtcNow.AddDays(-11);
        var order1 = CreateTestOrder(judgeId1, reassignmentDate);
        var order2 = CreateTestOrder(judgeId2, reassignmentDate);
        var judge2 = CreateTestJudge(judgeId2);
        var raj = CreateTestRAJ(rajId);
        var rajUser = CreateTestUser(rajId, "raj@example.com");

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([order1, order2]);

        // First judge throws exception
        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId1))
            .ThrowsAsync(new Exception("Test exception"));

        // Second judge succeeds
        _mockJudgeService
            .Setup(j => j.GetJudge(judgeId2))
            .ReturnsAsync(judge2);

        _mockJudgeService
            .Setup(j => j.GetJudges(
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync([raj]);

        _mockUserService
            .Setup(u => u.GetByJudgeIdAsync(rajId))
            .ReturnsAsync(rajUser);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        _mockEmailTemplateService
            .Setup(e => e.SendEmailTemplateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        await _job.Execute();

        _mockOrderRepo.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => o.Id == order2.Id)),
            Times.AtLeastOnce
        );
    }

    #endregion
}