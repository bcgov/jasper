using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Jobs;
using Scv.Api.Services;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Models.Order;
using Xunit;

namespace tests.api.Jobs;

public class SubmitOrderJobTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<ILogger<SubmitOrderJob>> _mockLogger;
    private readonly Mock<IEmailTemplateService> _mockEmailTemplateService;
    private readonly IConfiguration _configuration;

    public SubmitOrderJobTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _mockLogger = new Mock<ILogger<SubmitOrderJob>>();
        _mockEmailTemplateService = new Mock<IEmailTemplateService>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["SUPPORT_ACCOUNT"] = "support@test.com" })
            .Build();
    }

    private SubmitOrderJob CreateJob() =>
        new(_mockOrderService.Object, _mockLogger.Object, _mockEmailTemplateService.Object, _configuration);

    [Fact]
    public async Task Execute_Throws_WhenOrderIdIsMissing()
    {
        var job = CreateJob();

        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(null));
        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(" "));
    }

    [Fact]
    public async Task Execute_SubmitsAndClears_WhenSuccess()
    {
        var job = CreateJob();
        var orderId = "order-123";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Success());

        await job.Execute(orderId);

        _mockOrderService.Verify(s => s.SubmitOrder(orderId), Times.Once);
    }

    [Fact]
    public async Task Execute_Throws_WhenSubmitFails()
    {
        var job = CreateJob();
        var orderId = "order-456";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Failure("failure"));
        _mockOrderService.Setup(s => s.GetByIdAsync(orderId))
            .ReturnsAsync(new OrderDto { Id = orderId });

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(orderId));

        _mockOrderService.Verify(s => s.SubmitOrder(orderId), Times.Once);
        _mockEmailTemplateService.Verify(
            s => s.SendEmailTemplateAsync(EmailTemplate.ORDER_SUBMISSION_FAILED, "support@test.com", It.IsAny<object>()),
            Times.Once);
    }
}
