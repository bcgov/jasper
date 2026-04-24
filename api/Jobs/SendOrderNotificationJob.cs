using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Api.SignalR.Notifications;
using Scv.Core.Helpers;
using Scv.Models;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

/// <summary>
/// Background job for sending order notifications to judges.
/// This is a fire-and-forget job triggered when new orders are created.
/// </summary>
public class SendOrderNotificationJob(
    IJudgeService judgeService,
    IEmailTemplateService emailTemplateService,
    IUserService userService,
    OrderReceivedAckNotification orderReceivedAck,
    ILogger<SendOrderNotificationJob> logger)
{
    private readonly IJudgeService _judgeService = judgeService;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly IUserService _userService = userService;
    private readonly OrderReceivedAckNotification _orderReceivedAck = orderReceivedAck;
    private readonly ILogger<SendOrderNotificationJob> _logger = logger;

    public async Task Execute(OrderDto order)
    {
        try
        {
            _logger.LogInformation("Processing order notification job for file {FileId}",
                order.OrderRequest.PhysicalFileId);

            await NotifyJudgeOfNewOrderAsync(order);

            _logger.LogInformation("Order notification job completed for file {FileId}",
                order.OrderRequest.PhysicalFileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order notification for file {FileId}",
                order.OrderRequest.PhysicalFileId);
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    private async Task NotifyJudgeOfNewOrderAsync(OrderDto order)
    {
        var judgeId = order.JudgeId;
        if (order.JudgeId <= 0)
        {
            _logger.LogWarning("Cannot send notification - no judge assigned to order for file {FileId}",
                order.OrderRequest.PhysicalFileId);
            return;
        }

        var judge = await _judgeService.GetJudge(judgeId);
        if (judge == null)
        {
            _logger.LogWarning("Judge with id {JudgeId} not found", judgeId);
            return;
        }

        // Check if judge is active
        if (!ValidUserHelper.IsPersonActive(judge))
        {
            _logger.LogInformation("Judge {JudgeId} is inactive - skipping notification", judgeId);
            return;
        }

        var databaseUser = await _userService.GetByJudgeIdAsync(judgeId);
        if (databaseUser == null)
        {
            _logger.LogWarning("No database user found for judge {JudgeId}", judgeId);
            return;
        }

        await _orderReceivedAck.SendAsync(order, databaseUser.Id);

        var judgeEmail = databaseUser.Email;
        if (string.IsNullOrWhiteSpace(judgeEmail))
        {
            _logger.LogWarning("Judge {JudgeId} has no email address - cannot send notification", judgeId);
            return;
        }

        // Send notification email
        var emailData = new
        {
            JudgeName = GetJudgeName(judge),
            LastName = judge.Names?.FirstOrDefault()?.LastName ?? "",
            CaseFileNumber = order.OrderRequest?.CourtFileNo,
            ReferralNotes = order.OrderRequest?.Referral?.ReferralNotesTxt,
            ReferredBy = order.OrderRequest?.Referral?.ReferredByName,
            LocationShortname = order.OrderRequest?.CourtLocationDesc,
            LocationName = order.OrderRequest?.CourtLocationDesc,
            Priority = order.OrderRequest?.Referral?.PriorityType,
            DateReceived = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
        };

        await _emailTemplateService.SendEmailTemplateAsync("Order Received", judgeEmail, emailData);

        _logger.LogInformation("Notification sent to judge {JudgeId} for order on file {FileId}",
            judgeId, order.OrderRequest.PhysicalFileId);
    }

    private static string GetJudgeName(Person judge)
    {
        var latestName = judge.Names?.FirstOrDefault();
        if (latestName == null)
            return "Judge";

        return $"{latestName.FirstName} {latestName.LastName}".Trim();
    }
}
