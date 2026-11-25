using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

/// <summary>
/// Background job to submit a reviewed order to CSO.
/// </summary>
[JobFailureEmailFilter]
public class SubmitOrderJob(
    IOrderService orderService,
    ILogger<SubmitOrderJob> logger,
    IEmailTemplateService emailTemplateService,
    IConfiguration configuration)
{
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<SubmitOrderJob> _logger = logger;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly IConfiguration _configuration = configuration;

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task Execute(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        var result = await _orderService.SubmitOrder(orderId);
        if (!result.Succeeded)
        {
            var errorMessage = string.Join(", ", result.Errors);

            await this.NotifyForJobSubmissionFailure(orderId, errorMessage);

            _logger.LogWarning(
                "Submit order failed for order {OrderId}: {Errors}",
                orderId,
                errorMessage);
            throw new InvalidOperationException($"Failed to submit order {orderId}.");
        }
    }

    private async Task NotifyForJobSubmissionFailure(string orderId, string errorMessage)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        var supportAccount = _configuration.GetNonEmptyValue("SUPPORT_ACCOUNT");
        var emailData = CreateEmailData(order, errorMessage);

        await _emailTemplateService.SendEmailTemplateAsync(
            EmailTemplate.ORDER_SUBMISSION_FAILED,
            supportAccount,
            emailData);
    }

    private static object CreateEmailData(OrderDto order, string errorMessage) => new
    {
        OrderId = order.Id,
        JudgeName = order?.OrderRequest?.Referral?.SentToName,
        PackageId = order?.OrderRequest?.Referral?.PackageId,
        JudgeId = order.JudgeId,
        CourtListType = order?.OrderRequest?.Referral?.CourtListTypeCd,
        PriorityType = order?.OrderRequest?.Referral?.PriorityType,
        OrderStatus = order?.Status,
        SubmitStatus = order?.SubmitStatus,
        ErrorMessage = errorMessage
    };
}
