using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
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

        OperationResult result;
        try
        {
            result = await _orderService.SubmitOrder(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting order {OrderId}.", orderId);
            throw;
        }

        if (!result.Succeeded)
        {
            var errorMessage = string.Join(", ", result.Errors);
            _logger.LogWarning(
                "Submit order failed for order {OrderId}: {Errors}",
                orderId,
                errorMessage);

            if (result is DeskOrderExtractionFailure)
            {
                await NotifyForExtractionFailure(orderId, errorMessage);
            }

            throw new InvalidOperationException($"Failed to submit order {orderId}.");
        }
    }

    private async Task NotifyForExtractionFailure(string orderId, string errorMessage)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(orderId);
            if (order is null)
            {
                _logger.LogWarning("Order {OrderId} not found when preparing the extraction failure email.", orderId);
                return;
            }

            var supportAccount = _configuration.GetNonEmptyValue("SUPPORT_ACCOUNT");
            await _emailTemplateService.SendEmailTemplateAsync(
                EmailTemplate.ORDER_SUBMISSION_FAILED,
                supportAccount,
                CreateEmailData(order, errorMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify support of the extraction failure for order {OrderId}.", orderId);
        }
    }

    private static object CreateEmailData(OrderDto order, string errorMessage) => new
    {
        OrderId = order.Id,
        JudgeName = order.OrderRequest?.Referral?.SentToName,
        PackageId = order.OrderRequest?.Referral?.PackageId,
        JudgeId = order.JudgeId,
        CourtListType = order.OrderRequest?.Referral?.CourtListTypeCd,
        PriorityType = order.OrderRequest?.Referral?.PriorityType,
        OrderStatus = order.Status,
        SubmitStatus = order.SubmitStatus,
        ErrorMessage = errorMessage
    };
}
