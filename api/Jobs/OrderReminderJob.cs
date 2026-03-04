using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCSSCommon.Models;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Jobs;

/// <summary>
/// Recurring job to remind judges about pending orders and reassign orders that have been pending too long.
/// </summary>
public class OrderReminderJob(
    IRepositoryBase<Order> orderRepo,
    IJudgeService judgeService,
    IEmailTemplateService emailTemplateService,
    IUserService userService,
    IConfiguration configuration,
    IOptions<JobsOrderReminderOptions> options,
    ILogger<OrderReminderJob> logger) : IRecurringJob
{
    private readonly IRepositoryBase<Order> _orderRepo = orderRepo;
    private readonly IJudgeService _judgeService = judgeService;
    private readonly IEmailTemplateService _emailTemplateService = emailTemplateService;
    private readonly IUserService _userService = userService;
    private readonly IConfiguration _configuration = configuration;
    private readonly JobsOrderReminderOptions _options = options.Value;
    private readonly ILogger<OrderReminderJob> _logger = logger;

    public string JobName => nameof(OrderReminderJob);
    public string CronSchedule => _options.CronSchedule;

    public async Task Execute()
    {
        _logger.LogInformation("Starting order reminder job");

        // Retrieve only orders that have not been processed and are pending submission
        var unresolvedOrders = await _orderRepo.FindAsync(o => o.Status == OrderStatus.Unapproved && o.SubmitStatus == SubmitStatus.Pending);
        if (unresolvedOrders == null || !unresolvedOrders.Any())
        {
            _logger.LogInformation("No unresolved orders found");
            return;
        }

        var reminderThresholdDays = int.TryParse(_configuration.GetNonEmptyValue("ORDER_REMINDER_THRESHOLD_DAYS"), out var reminderDays) 
            ? reminderDays : 5;
        var reassignmentThresholdDays = int.TryParse(_configuration.GetNonEmptyValue("ORDER_REASSIGNMENT_THRESHOLD_DAYS"), out var reassignDays) 
            ? reassignDays : 10;

        var reminderFromNow = DateTime.UtcNow.AddDays(-reminderThresholdDays);
        var reassignmentFromNow = DateTime.UtcNow.AddDays(-reassignmentThresholdDays);

        var ordersNeedingReminder = unresolvedOrders.Where(o => o.Ent_Dtm <= reminderFromNow && o.Ent_Dtm > reassignmentFromNow).ToList();
        var ordersNeedingReassignment = unresolvedOrders.Where(o => o.Ent_Dtm <= reassignmentFromNow).ToList();

        _logger.LogInformation(
            "Found {ReminderCount} orders needing reminders and {ReassignCount} orders needing reassignment",
            ordersNeedingReminder.Count,
            ordersNeedingReassignment.Count);

        foreach (var order in ordersNeedingReminder)
            await SendReminderToJudge(order);

        foreach (var order in ordersNeedingReassignment)
            await ReassignOrderToRAJ(order);

        _logger.LogInformation("Order reminder job completed");
    }

    private async Task SendReminderToJudge(Order order)
    {
        try
        {
            var (judge, user) = await GetJudgeAndUserAsync(order);
            if (judge == null || user == null) return;

            var emailData = CreateEmailData(order, GetJudgeName(judge));

            await _emailTemplateService.SendEmailTemplateAsync(
                "Order Reminder",
                user.Email,
                emailData);

            _logger.LogInformation("Reminder sent to judge {JudgeId} for order {OrderId}", user.JudgeId, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder for order {OrderId}", order.Id);
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
                _logger.LogWarning("No RAJ found for judge {JudgeId} for order {OrderId}",
                    order.OrderRequest.Referral.SentToPartId, order.Id);
                return;
            }

            order.OrderRequest.Referral.SentToPartId = raj.UserId;
            order.OrderRequest.Referral.SentToName = GetRajName(raj);
            await _orderRepo.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} reassigned from judge {JudgeId} to RAJ {RajId}",
                order.Id, judge.UserId, raj.UserId);

            await SendReassignmentNotifications(order, raj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reassign order {OrderId}", order.Id);
        }
    }

    private async Task<PersonSearchItem> GetRAJForJudge(Models.Person judge)
    {
        var relatedRaj = await _judgeService.GetJudges(
            [JudgeService.REGIONAL_ADMIN_JUDGE],
            [judge.HomeLocationId.ToString()]);
        
        return relatedRaj.FirstOrDefault();
    }

    private async Task SendReassignmentNotifications(Order order, PersonSearchItem raj)
    {
        var rajUser = await _userService.GetByJudgeIdAsync(raj.UserId);
        if (rajUser == null || string.IsNullOrWhiteSpace(rajUser.Email)) return;

        var emailData = CreateEmailData(order, GetRajName(raj));

        await _emailTemplateService.SendEmailTemplateAsync(
            "Order Reassignment",
            rajUser.Email,
            emailData);
        
        _logger.LogInformation("Reassignment notification sent to RAJ {RajId} for order {OrderId}",
            raj.UserId, order.Id);
    }

    private async Task<(Models.Person judge, Models.AccessControlManagement.UserDto user)> GetJudgeAndUserAsync(Order order)
    {
        var judgeId = order.OrderRequest?.Referral?.SentToPartId;
        if (!judgeId.HasValue)
        {
            _logger.LogWarning("No judge assigned to order {OrderId}", order.Id);
            return (null, null);
        }

        var judge = await _judgeService.GetJudge(judgeId.Value);
        if (judge == null)
        {
            _logger.LogWarning("Judge {JudgeId} not found for order {OrderId}", judgeId.Value, order.Id);
            return (null, null);
        }

        var user = await _userService.GetByJudgeIdAsync(judgeId.Value);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("No valid user/email for judge {JudgeId} for order {OrderId}", judgeId.Value, order.Id);
            return (judge, null);
        }

        return (judge, user);
    }

    private static object CreateEmailData(Order order, string judgeName) => new
    {
        JudgeName = judgeName,
        CaseFileNumber = order.OrderRequest?.CourtFile?.CourtFileNo,
        DateReceived = order.Ent_Dtm.ToString("MMMM dd, yyyy"),
        LocationName = order.OrderRequest?.CourtFile?.CourtLocationDesc,
        Priority = order.OrderRequest.Referral.PriorityType
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
