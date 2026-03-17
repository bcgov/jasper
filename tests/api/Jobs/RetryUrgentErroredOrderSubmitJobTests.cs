using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Jobs;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Jobs;

public class RetryUrgentErroredOrderSubmitJobTests
{
    private readonly Mock<IRepositoryBase<Order>> _mockRepo;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<ILogger<RetryUrgentErroredOrderSubmitJob>> _mockLogger;

    public RetryUrgentErroredOrderSubmitJobTests()
    {
        _mockRepo = new Mock<IRepositoryBase<Order>>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _mockLogger = new Mock<ILogger<RetryUrgentErroredOrderSubmitJob>>();
    }

    [Fact]
    public async Task Execute_EnqueuesOnlyUrgentPriorityOrders()
    {
        var options = Options.Create(new JobsRetryUrgentSubmitOrderOptions
        {
            CronSchedule = "*/15 * * * *",
            MaxRetries = 9,
            PriorityType = "URG"
        });

        var job = new RetryUrgentErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        var orders = new List<Order>
        {
            new()
            {
                Id = "ORDER-URG",
                SubmitStatus = SubmitStatus.Error,
                Status = OrderStatus.Approved,
                OrderRequest = new OrderRequest { Referral = new Referral { PriorityType = "URG" } }
            },
            new()
            {
                Id = "ORDER-HIGH",
                SubmitStatus = SubmitStatus.Error,
                Status = OrderStatus.Approved,
                OrderRequest = new OrderRequest { Referral = new Referral { PriorityType = "HIGH" } }
            }
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>() ))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_DoesNotEnqueue_WhenNoUrgentOrdersFound()
    {
        var options = Options.Create(new JobsRetryUrgentSubmitOrderOptions
        {
            CronSchedule = "*/15 * * * *",
            MaxRetries = 9,
            PriorityType = "URG"
        });

        var job = new RetryUrgentErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>() ))
            .ReturnsAsync(new List<Order>());

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_MatchesPriorityType_CaseInsensitive()
    {
        var options = Options.Create(new JobsRetryUrgentSubmitOrderOptions
        {
            CronSchedule = "*/15 * * * *",
            MaxRetries = 9,
            PriorityType = "urg"
        });

        var job = new RetryUrgentErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        var orders = new List<Order>
        {
            new()
            {
                Id = "ORDER-URG",
                SubmitStatus = SubmitStatus.Error,
                Status = OrderStatus.Unapproved,
                OrderRequest = new OrderRequest { Referral = new Referral { PriorityType = "URG" } }
            }
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>() ))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Once);
    }
}
