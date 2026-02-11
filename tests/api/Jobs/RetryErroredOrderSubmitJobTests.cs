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

public class RetryErroredOrderSubmitJobTests
{
    private readonly Mock<IRepositoryBase<Order>> _mockRepo;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<ILogger<RetryErroredOrderSubmitJob>> _mockLogger;

    public RetryErroredOrderSubmitJobTests()
    {
        _mockRepo = new Mock<IRepositoryBase<Order>>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _mockLogger = new Mock<ILogger<RetryErroredOrderSubmitJob>>();
    }

    [Fact]
    public async Task Execute_DoesNotEnqueue_WhenNoOrdersFound()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 });
        var job = new RetryErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(new List<Order>());

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_DoesNotEnqueue_WhenRepositoryReturnsNull()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 });
        var job = new RetryErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync((IEnumerable<Order>)null);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_DoesNotEnqueue_WhenOrderIdsAreMissing()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 });
        var job = new RetryErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        var orders = new List<Order>
        {
            new() { Id = null },
            new() { Id = " " }
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_DeduplicatesAndEnqueues_ForDistinctIds()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 });
        var job = new RetryErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        var orders = new List<Order>
        {
            new() { Id = "ORDER-1", SubmitStatus = SubmitStatus.Error, Status = OrderStatus.Approved },
            new() { Id = "order-1", SubmitStatus = SubmitStatus.Error, Status = OrderStatus.Approved },
            new() { Id = "ORDER-2", SubmitStatus = SubmitStatus.Error, Status = OrderStatus.Approved },
            new() { Id = " " }
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Execute_DoesNotEnqueue_WhenSubmitAttemptsExceedMaxRetries()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 2 });
        var job = new RetryErroredOrderSubmitJob(
            _mockRepo.Object,
            _mockBackgroundJobClient.Object,
            options,
            _mockLogger.Object);

        var orders = new List<Order>
        {
            new() { Id = "ORDER-1", SubmitAttempts = 3 },
            new() { Id = "ORDER-2", SubmitAttempts = 10 }
        };

        _mockRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }
}
