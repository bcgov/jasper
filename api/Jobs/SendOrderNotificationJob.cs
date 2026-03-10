using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Models.Order;
using Scv.Api.Services;
using Scv.Api.SignalR;

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
                order.OrderRequest.CourtFile.PhysicalFileId);

            await NotifyJudgeOfNewOrderAsync(order);

            _logger.LogInformation("Order notification job completed for file {FileId}",
                order.OrderRequest.CourtFile.PhysicalFileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order notification for file {FileId}",
                order.OrderRequest.CourtFile.PhysicalFileId);
            throw; // Hangfire will retry based on configured retry policy
        }
    }

    private async Task NotifyJudgeOfNewOrderAsync(OrderDto order)
    {
        var judgeId = order.OrderRequest.Referral.SentToPartId;
        if (!judgeId.HasValue)
        {
            _logger.LogWarning("Cannot send notification - no judge assigned to order for file {FileId}",
                order.OrderRequest.CourtFile.PhysicalFileId);
            return;
        }

        var judge = await _judgeService.GetJudge(judgeId.Value);
        if (judge == null)
        {
            _logger.LogWarning("Judge with id {JudgeId} not found", judgeId.Value);
            return;
        }

        // Check if judge is active
        if (!ValidUserHelper.IsPersonActive(judge))
        {
            _logger.LogInformation("Judge {JudgeId} is inactive - skipping notification", judgeId.Value);
            return;
        }

        var databaseUser = await _userService.GetByJudgeIdAsync(judgeId.Value);
        if (databaseUser == null)
        {
            _logger.LogWarning("No database user found for judge {JudgeId}", judgeId.Value);
            return;
        }

        await _orderReceivedAck.SendAsync(order, databaseUser.Id, judgeId.Value);

        var judgeEmail = databaseUser.Email;
        if (string.IsNullOrWhiteSpace(judgeEmail))
        {
            _logger.LogWarning("Judge {JudgeId} has no email address - cannot send notification", judgeId.Value);
            return;
        }

        // Send notification email
        var emailData = new
        {
            JudgeName = GetJudgeName(judge),
            LastName = judge.Names?.FirstOrDefault()?.LastName ?? "",
            CaseFileNumber = order.OrderRequest.CourtFile?.CourtFileNo,
            ReferralNotes = order.OrderRequest.Referral?.ReferralNotesTxt,
            ReferredBy = order.OrderRequest.Referral?.ReferredByName,
            LocationShortname = order.OrderRequest.CourtFile?.CourtLocationDesc,
            LocationName = order.OrderRequest.CourtFile?.CourtLocationDesc,
            Priority = order.OrderRequest.Referral.PriorityType,
            DateReceived = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
        };

        await _emailTemplateService.SendEmailTemplateAsync("Order Received", judgeEmail, emailData);

        _logger.LogInformation("Notification sent to judge {JudgeId} for order on file {FileId}",
            judgeId.Value, order.OrderRequest.CourtFile.PhysicalFileId);
    }

    private static string GetJudgeName(Models.Person judge)
    {
        var latestName = judge.Names?.FirstOrDefault();
        if (latestName == null)
            return "Judge";

        return $"{latestName.FirstName} {latestName.LastName}".Trim();
    }
}
