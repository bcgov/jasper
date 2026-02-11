using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Jobs;

public class SubmitOrderJobTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<SubmitOrderJob>> _mockLogger;

    public SubmitOrderJobTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<SubmitOrderJob>>();
    }

    [Fact]
    public async Task Execute_Throws_WhenOrderIdIsMissing()
    {
        var job = new SubmitOrderJob(_mockOrderService.Object, _mockLogger.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(null));
        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(" "));
    }

    [Fact]
    public async Task Execute_SubmitsAndClears_WhenSuccess()
    {
        var job = new SubmitOrderJob(_mockOrderService.Object, _mockLogger.Object);
        var orderId = "order-123";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Success());

        await job.Execute(orderId);

        _mockOrderService.Verify(s => s.SubmitOrder(orderId), Times.Once);
    }

    [Fact]
    public async Task Execute_Throws_WhenSubmitFails()
    {
        var job = new SubmitOrderJob(_mockOrderService.Object, _mockLogger.Object);
        var orderId = "order-456";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Failure("failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(orderId));

        _mockOrderService.Verify(s => s.SubmitOrder(orderId), Times.Once);
    }
}
