using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;
using Scv.Api.Models.Order;

namespace Scv.Api.Services;

public interface IOrderNotificationService
{
    Task NotifyJudgeOfNewOrderAsync(OrderDto order);
}

public class OrderNotificationService(
    IDashboardService dashboardService,
    IEmailTemplateService emailTemplateService,
    IUserService userService,
    ILogger<OrderNotificationService> logger) : IOrderNotificationService
{
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly ILogger<OrderNotificationService> _logger = logger;
    private readonly IUserService _userService = userService;

    public async Task NotifyJudgeOfNewOrderAsync(OrderDto order)
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
        if(databaseUser == null)
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
            CourtFileNumber = order.CourtFile.FullFileNo,
            StyleOfCause = order.CourtFile.StyleOfCause,
            ReferralNotes = order.Referral.ReferralNotesTxt,
            ReferredBy = order.Referral.ReferredByName
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
