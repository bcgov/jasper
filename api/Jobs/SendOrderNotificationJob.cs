using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Models.Order;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

/// <summary>
/// Background job for sending order notifications to judges.
/// This is a fire-and-forget job triggered when new orders are created.
/// </summary>
public class SendOrderNotificationJob(
    IDashboardService dashboardService,
    IEmailTemplateService emailTemplateService,
    IUserService userService,
    ILogger<SendOrderNotificationJob> logger)
{
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly IUserService _userService = userService;
    private readonly ILogger<SendOrderNotificationJob> _logger = logger;

    public async Task Execute(OrderRequestDto order)
    {
        try
        {
            _logger.LogInformation("Processing order notification job for file {FileId}", 
                order.CourtFile.PhysicalFileId);

            await NotifyJudgeOfNewOrderAsync(order);

            _logger.LogInformation("Order notification job completed for file {FileId}", 
                order.CourtFile.PhysicalFileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order notification for file {FileId}", 
                order.CourtFile.PhysicalFileId);
            throw; // Hangfire will retry based on configured retry policy
        }
    }

    private async Task NotifyJudgeOfNewOrderAsync(OrderRequestDto order)
    {
        var judgeId = order.Referral.SentToPartId;
        if (!judgeId.HasValue)
        {
            _logger.LogWarning("Cannot send notification - no judge assigned to order for file {FileId}", 
                order.CourtFile.PhysicalFileId);
            return;
        }

        var judge = await _dashboardService.GetJudge(judgeId.Value);
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
            CaseFileNumber = order.CourtFile.FullFileNo,
            ReferralNotes = order.Referral.ReferralNotesTxt,
            ReferredBy = order.Referral.ReferredByName,
            LocationShortname = order.CourtFile.CourtLocationDesc,
            LocationName = order.CourtFile.CourtLocationDesc
        };

        await _emailTemplateService.SendEmailTemplateAsync("Order Received", judgeEmail, emailData);
        
        _logger.LogInformation("Notification sent to judge {JudgeId} at {Email} for order on file {FileId}", 
            judgeId.Value, judgeEmail, order.CourtFile.PhysicalFileId);
    }

    private static string GetJudgeName(Models.Person judge)
    {
        var latestName = judge.Names?.FirstOrDefault();
        if (latestName == null)
            return "Judge";

        return $"{latestName.FirstName} {latestName.LastName}".Trim();
    }
}
