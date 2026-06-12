using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;
using Scv.Api.SignalR.Notifications;
using Scv.Core.Helpers;
using Scv.Db.Models;
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

            if (order.OrderRequest?.Referral?.IsPriority != true)
            {
                _logger.LogInformation("Order for file {FileId} is not marked as priority - skipping notification",
                    order.OrderRequest.PhysicalFileId);
                return;
            }

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

        var effectiveJudgeName = GetEffectiveJudgeName(judge);
        var formattedJudgeName = effectiveJudgeName == null
            ? "Judge"
            : FormatJudgeFullName(effectiveJudgeName);

        // Send notification email
        var emailData = new
        {
            JudgeName = formattedJudgeName,
            LastName = effectiveJudgeName?.LastName ?? "",
            CaseFileNumber = order.OrderRequest?.CourtFileNo,
            ReferralNotes = order.OrderRequest?.Referral?.ReferralNotesTxt,
            ReferredBy = order.OrderRequest?.Referral?.ReferredByName,
            LocationShortname = order.OrderRequest?.CourtLocationDesc,
            LocationName = order.OrderRequest?.CourtLocationDesc,
            Priority = order.OrderRequest?.Referral?.PriorityType,
            IsPriority = order.OrderRequest?.Referral?.IsPriority,
            PriorityTypeDesc = order.OrderRequest?.Referral?.PriorityTypeDesc,
            DateReceived = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
        };

        await _emailTemplateService.SendEmailTemplateAsync(EmailTemplate.ORDER_RECEIVED, judgeEmail, emailData);

        _logger.LogInformation("Notification sent to judge {JudgeId} for order on file {FileId}",
            judgeId, order.OrderRequest.PhysicalFileId);
    }

    private static PersonName GetEffectiveJudgeName(Person judge)
    {
        var today = DateTime.Today;

        return judge.Names?
            .Where(name =>
            {
                var effectiveDate = GetNameEffectiveDate(name);
                var expiryDate = GetNameExpiryDate(name);

                return effectiveDate.HasValue
                    && effectiveDate.Value.Date < today
                    && (!expiryDate.HasValue || expiryDate.Value.Date > today);
            })
            .OrderByDescending(name => GetNameEffectiveDate(name))
            .FirstOrDefault();
    }

    private static string FormatJudgeFullName(PersonName judgeName)
    {
        return string.Join(" ", new[] { judgeName.FirstName, judgeName.LastName }
            .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));
    }

    private static DateTime? GetNameEffectiveDate(PersonName name)
    {
        if (name.EffDate.HasValue)
        {
            return name.EffDate.Value;
        }

        return DateTime.TryParse(name.EffectiveDate, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var effectiveDate)
            ? effectiveDate
            : null;
    }

    private static DateTime? GetNameExpiryDate(PersonName name)
    {
        if (name.ExpDate.HasValue)
        {
            return name.ExpDate.Value;
        }

        if (string.IsNullOrWhiteSpace(name.ExpiryDate))
        {
            return null;
        }

        return DateTime.TryParse(name.ExpiryDate, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var expiryDate)
            ? expiryDate
            : null;
    }
}
