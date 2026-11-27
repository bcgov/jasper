using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scriban;
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

    private SubmitOrderJob CreateJob(IConfiguration configuration = null) =>
        new(_mockOrderService.Object, _mockLogger.Object, _mockEmailTemplateService.Object, configuration ?? _configuration);

    [Fact]
    public async Task Execute_Throws_WhenOrderIdIsMissing()
    {
        var job = CreateJob();

        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(null));
        await Assert.ThrowsAsync<ArgumentException>(() => job.Execute(" "));
    }

    [Fact]
    public async Task Execute_DoesNotSendFailureEmail_WhenSubmissionSucceeds()
    {
        var job = CreateJob();
        var orderId = "order-123";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Success());

        await job.Execute(orderId);

        _mockOrderService.Verify(s => s.SubmitOrder(orderId), Times.Once);
        _mockEmailTemplateService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ThrowsWithoutDetailedEmail_WhenNonExtractionFailureOccurs()
    {
        var job = CreateJob();
        var orderId = "order-456";

        _mockOrderService.Setup(s => s.SubmitOrder(orderId))
            .ReturnsAsync(OperationResult.Failure("Private CSO response body"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(orderId));

        Assert.Equal($"Failed to submit order {orderId}.", exception.Message);
        _mockOrderService.Verify(s => s.GetByIdAsync(orderId), Times.Never);
        _mockEmailTemplateService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_SendsSupportEmailWithPackageJudgeAndSafeError_WhenExtractionFails()
    {
        const string orderId = "order-456";
        var job = CreateJob();
        var failure = new DeskOrderExtractionFailure();
        _mockOrderService.Setup(s => s.SubmitOrder(orderId)).ReturnsAsync(failure);
        _mockOrderService.Setup(s => s.GetByIdAsync(orderId))
            .ReturnsAsync(new OrderDto
            {
                Id = orderId,
                JudgeId = 42,
                OrderRequest = new OrderRequestDto { Referral = new ReferralDto { PackageId = 12345 } }
            });
        object emailData = null;
        _mockEmailTemplateService
            .Setup(s => s.SendEmailTemplateAsync(EmailTemplate.ORDER_SUBMISSION_FAILED, "support@test.com", It.IsAny<object>()))
            .Callback<string, string, object>((_, _, data) => emailData = data)
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(orderId));

        Assert.NotNull(emailData);
        var template = EmailTemplate.ALL_EMAIL_TEMPLATES.Single(t => t.TemplateName == EmailTemplate.ORDER_SUBMISSION_FAILED);
        var body = await Template.Parse(template.Body).RenderAsync(emailData);
        Assert.Contains("Package Id: 12345", body);
        Assert.Contains("Judge Id: 42", body);
        Assert.Contains("The order was not sent to CSO.", body);
        Assert.Contains("Replace the supporting document and retry.", body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_PreservesOriginalFailure_WhenNotificationOrderIsMissingOrLookupFails(bool lookupFails)
    {
        const string orderId = "order-456";
        _mockOrderService.Setup(s => s.SubmitOrder(orderId)).ReturnsAsync(new DeskOrderExtractionFailure());
        var lookup = _mockOrderService.Setup(s => s.GetByIdAsync(orderId));
        if (lookupFails)
        {
            lookup.ThrowsAsync(new Exception("Database unavailable"));
        }
        else
        {
            lookup.ReturnsAsync((OrderDto)null);
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().Execute(orderId));

        Assert.Equal($"Failed to submit order {orderId}.", exception.Message);
        _mockEmailTemplateService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_PreservesOriginalFailure_WhenSupportConfigurationOrDeliveryFails(bool deliveryFails)
    {
        const string orderId = "order-456";
        _mockOrderService.Setup(s => s.SubmitOrder(orderId)).ReturnsAsync(new DeskOrderExtractionFailure());
        _mockOrderService.Setup(s => s.GetByIdAsync(orderId)).ReturnsAsync(new OrderDto { Id = orderId });
        _mockEmailTemplateService
            .Setup(s => s.SendEmailTemplateAsync(EmailTemplate.ORDER_SUBMISSION_FAILED, "support@test.com", It.IsAny<object>()))
            .ThrowsAsync(new Exception("Email delivery failed"));
        var configuration = deliveryFails ? _configuration : new ConfigurationBuilder().Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob(configuration).Execute(orderId));

        Assert.Equal($"Failed to submit order {orderId}.", exception.Message);
        _mockEmailTemplateService.Verify(
            s => s.SendEmailTemplateAsync(EmailTemplate.ORDER_SUBMISSION_FAILED, "support@test.com", It.IsAny<object>()),
            deliveryFails ? Times.Once() : Times.Never());
    }
}
