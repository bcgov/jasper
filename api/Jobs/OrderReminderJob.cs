using System;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

/// <summary>
/// Recurring job to remind judges about pending orders and reassign orders that have been pending too long.
/// </summary>
public class OrderReminderJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<OrderReminderJob> logger,
    IRepositoryBase<Order> orderRepo,
    IJudgeService judgeService,
    IEmailTemplateService emailTemplateService,
    IUserService userService,
    IOptions<JobsOrderReminderOptions> options) : RecurringJobBase<OrderReminderJob>(configuration, cache, mapper, logger)
{
    private readonly IRepositoryBase<Order> _orderRepo = orderRepo;
    private readonly IJudgeService _judgeService = judgeService;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly IUserService _userService = userService;
    private readonly JobsOrderReminderOptions _options = options.Value;

    public override string JobName => nameof(OrderReminderJob);
    public override string CronSchedule => _options.CronSchedule;

    public override async Task Execute()
    {
        Logger.LogInformation("Starting order reminder job");

        // Retrieve only orders that have not been processed and are pending submission
        var unresolvedOrders = await _orderRepo.FindAsync(o => o.Status == OrderStatus.Pending && o.SubmitStatus == SubmitStatus.Pending);
        if (unresolvedOrders == null || !unresolvedOrders.Any())
        {
            Logger.LogInformation("No unresolved orders found");
            return;
        }

        var reminderThresholdDays = int.TryParse(Configuration.GetNonEmptyValue("ORDER_REMINDER_THRESHOLD_DAYS"), out var reminderDays) 
            ? reminderDays : 5;
        var reassignmentThresholdDays = int.TryParse(Configuration.GetNonEmptyValue("ORDER_REASSIGNMENT_THRESHOLD_DAYS"), out var reassignDays) 
            ? reassignDays : 10;
        var maxReminderNotifications = int.TryParse(Configuration.GetNonEmptyValue("ORDER_MAX_REMINDER_NOTIFICATIONS"), out var maxReminders) 
            ? maxReminders : 1;
        var maxReassignmentNotifications = int.TryParse(Configuration.GetNonEmptyValue("ORDER_MAX_REASSIGNMENT_NOTIFICATIONS"), out var maxReassignments) 
            ? maxReassignments : 1;

        var reminderFromNow = DateTime.UtcNow.AddDays(-reminderThresholdDays);
        var reassignmentFromNow = DateTime.UtcNow.AddDays(-reassignmentThresholdDays);

        var ordersNeedingReminder = unresolvedOrders
            .Where(o => o.Ent_Dtm <= reminderFromNow && 
                       o.Ent_Dtm > reassignmentFromNow && 
                       o.ReminderNotificationsSent < maxReminderNotifications)
            .ToList();
        var ordersNeedingReassignment = unresolvedOrders
            .Where(o => o.Ent_Dtm <= reassignmentFromNow && 
                       o.ReassignmentNotificationsSent < maxReassignmentNotifications)
            .ToList();

        Logger.LogInformation(
            "Found {ReminderCount} orders needing reminders and {ReassignCount} orders needing reassignment",
            ordersNeedingReminder.Count,
            ordersNeedingReassignment.Count);

        foreach (var order in ordersNeedingReminder)
            await SendReminderToJudge(order);

        foreach (var order in ordersNeedingReassignment)
            await ReassignOrderToRAJ(order);

        Logger.LogInformation("Order reminder job completed");
    }

    private async Task SendReminderToJudge(Order order)
    {
        try
        {
            var (judge, user) = await GetJudgeAndUserAsync(order);
            if (judge == null || user == null)
            {
                Logger.LogWarning("Skipping reminder for order {OrderId} due to missing user judge information", order.Id);
                return;
            }

            var supportAccount = Configuration.GetNonEmptyValue("SUPPORT_ACCOUNT");
            var emailData = CreateEmailData(order, GetJudgeName(judge), supportAccount);

            await _emailTemplateService.SendEmailTemplateAsync(
                "Order Reminder",
                user.Email,
                emailData);

            order.ReminderNotificationsSent++;
            await _orderRepo.UpdateAsync(order);

            Logger.LogInformation("Reminder sent to judge {JudgeId} for order {OrderId} (count: {Count})", 
                user.JudgeId, order.Id, order.ReminderNotificationsSent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send reminder for order {OrderId}", order.Id);
        }
    }

    private async Task ReassignOrderToRAJ(Order order)
    {
        try
        {
            var (judge, _) = await GetJudgeAndUserAsync(order);
            if (judge == null) return;

            var raj = await GetRAJForJudge(judge);
            if (raj == null)
            {
                Logger.LogWarning("No RAJ found for judge {JudgeId} for order {OrderId}",
                    order.OrderRequest.Referral.SentToPartId, order.Id);
                return;
            }

            // Only reassign if the RAJ is not already assigned
            if (order.OrderRequest.Referral.SentToPartId != raj.UserId)
            {
                order.OrderRequest.Referral.SentToPartId = raj.UserId;
                order.OrderRequest.Referral.SentToName = GetRajName(raj);
                await _orderRepo.UpdateAsync(order);

                Logger.LogInformation("Order {OrderId} reassigned from judge {JudgeId} to RAJ {RajId}",
                    order.Id, judge.UserId, raj.UserId);
            }
            else
            {
                Logger.LogInformation("Order {OrderId} already assigned to RAJ {RajId}, skipping reassignment",
                    order.Id, raj.UserId);
            }

            // Always send notification regardless of whether reassignment occurred
            await SendReassignmentNotifications(order, raj);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reassign order {OrderId}", order.Id);
        }
    }

    private async Task<PersonSearchItem> GetRAJForJudge(Models.Person judge)
    {
        if (!judge.HomeLocationId.HasValue)
        {
            Logger.LogWarning("Judge {JudgeId} has no HomeLocationId set", judge.UserId);
            return null;
        }

        var relatedRaj = await _judgeService.GetJudges(
            [JudgeService.REGIONAL_ADMIN_JUDGE],
            [judge.HomeLocationId.Value.ToString()]);
        
        return relatedRaj.FirstOrDefault();
    }

    private async Task SendReassignmentNotifications(Order order, PersonSearchItem raj)
    {
        var rajUser = await _userService.GetByJudgeIdAsync(raj.UserId);
        if (rajUser == null || string.IsNullOrWhiteSpace(rajUser.Email)) return;

        var supportAccount = Configuration.GetNonEmptyValue("SUPPORT_ACCOUNT");
        var emailData = CreateEmailData(order, GetRajName(raj), supportAccount);

        await _emailTemplateService.SendEmailTemplateAsync(
            "Order Reassignment",
            rajUser.Email,
            emailData);
        
        order.ReassignmentNotificationsSent++;
        await _orderRepo.UpdateAsync(order);
        
        Logger.LogInformation("Reassignment notification sent to RAJ {RajId} for order {OrderId} (count: {Count})",
            raj.UserId, order.Id, order.ReassignmentNotificationsSent);
    }

    private async Task<(Models.Person judge, Scv.Models.AccessControlManagement.UserDto user)> GetJudgeAndUserAsync(Order order)
    {
        var judgeId = order.OrderRequest?.Referral?.SentToPartId;
        if (!judgeId.HasValue)
        {
            Logger.LogWarning("No judge assigned to order {OrderId}", order.Id);
            return (null, null);
        }

        var judge = await _judgeService.GetJudge(judgeId.Value);
        if (judge == null)
        {
            Logger.LogWarning("Judge {JudgeId} not found for order {OrderId}", judgeId.Value, order.Id);
            return (null, null);
        }

        var user = await _userService.GetByJudgeIdAsync(judgeId.Value);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            Logger.LogWarning("No valid user/email for judge {JudgeId} for order {OrderId}", judgeId.Value, order.Id);
            return (judge, null);
        }

        return (judge, user);
    }

    private static object CreateEmailData(Order order, string judgeName, string supportAccount) => new
    {
        JudgeName = judgeName,
        CaseFileNumber = order.OrderRequest?.CourtFile?.CourtFileNo,
        DateReceived = order.Ent_Dtm.ToString("MMMM dd, yyyy"),
        LocationName = order.OrderRequest?.CourtFile?.CourtLocationDesc,
        Priority = order.OrderRequest?.Referral?.PriorityType,
        SupportAccount = supportAccount
    };

    private static string GetJudgeName(Models.Person judge)
    {
        var latestName = judge.Names?.FirstOrDefault();
        if (latestName == null)
            return "Judge";

        return $"{latestName.FirstName} {latestName.LastName}".Trim();
    }
    
    private static string GetRajName(PersonSearchItem raj) => $"{raj.FirstName} {raj.LastName}".Trim();
}
