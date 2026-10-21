using System;
using System.Collections.Generic;
using System.Linq;
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
using Scv.Models.Order;
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

    private async Task<Func<Order, bool>> CaptureErroredOrdersFilter(RetryErroredOrderSubmitJob job)
    {
        Expression<Func<Order, bool>> captured = null;
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .Callback<Expression<Func<Order, bool>>>(e => captured = e)
            .ReturnsAsync([]);

        await job.Execute();

        Assert.NotNull(captured);
        return captured.Compile();
    }

    private RetryErroredOrderSubmitJob CreateJob() => new(
        _mockRepo.Object,
        _mockBackgroundJobClient.Object,
        Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 }),
        _mockLogger.Object);

    [Theory]
    [InlineData(OrderStatus.Unapproved, true)]
    [InlineData(OrderStatus.Approved, true)]
    [InlineData(OrderStatus.AwaitingDocumentation, true)]
    [InlineData(OrderStatus.OrderMade, true)]
    [InlineData(OrderStatus.Pending, false)]
    public async Task Execute_QueriesErroredOrders_OnlyForRetryableStatuses(OrderStatus status, bool expected)
    {
        var filter = await CaptureErroredOrdersFilter(CreateJob());

        var matches = filter(new Order { Status = status, SubmitStatus = SubmitStatus.Error });

        Assert.Equal(expected, matches);
    }

    [Fact]
    public async Task Execute_QueriesErroredOrders_ForEveryOrderStatusExceptPending()
    {
        var filter = await CaptureErroredOrdersFilter(CreateJob());

        var matched = Enum.GetValues<OrderStatus>()
            .Where(s => filter(new Order { Status = s, SubmitStatus = SubmitStatus.Error }))
            .ToList();

        Assert.DoesNotContain(OrderStatus.Pending, matched);
        Assert.Equal(Enum.GetValues<OrderStatus>().Length - 1, matched.Count);
    }

    [Theory]
    [InlineData(SubmitStatus.Pending)]
    [InlineData(SubmitStatus.Sending)]
    [InlineData(SubmitStatus.Submitted)]
    public async Task Execute_QueriesErroredOrders_ExcludesNonErrorSubmitStatuses(SubmitStatus submitStatus)
    {
        var filter = await CaptureErroredOrdersFilter(CreateJob());

        Assert.False(filter(new Order { Status = OrderStatus.OrderMade, SubmitStatus = submitStatus }));
    }

    [Fact]
    public async Task Execute_Enqueues_ErroredOrderMadeOrders()
    {
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([new Order { Id = "ORDER-MADE", SubmitStatus = SubmitStatus.Error, Status = OrderStatus.OrderMade }]);

        await CreateJob().Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(
                It.Is<Hangfire.Common.Job>(j => j.Type == typeof(SubmitOrderJob) && (string)j.Args[0] == "ORDER-MADE"),
                It.IsAny<Hangfire.States.IState>()),
            Times.Once);
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
            .ReturnsAsync([]);

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

    [Fact]
    public async Task Execute_DoesNotEnqueue_UrgentPriorityOrders()
    {
        var options = Options.Create(new JobsRetrySubmitOrderOptions { CronSchedule = "0 0 * * 0", MaxRetries = 9 });
        var job = new RetryErroredOrderSubmitJob(
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
                OrderRequest = new OrderRequest
                {
                    Referral = new Referral { PriorityType = "URG" }
                }
            }
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(orders);

        await job.Execute();

        _mockBackgroundJobClient.Verify(
            c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()),
            Times.Never);
    }
}
