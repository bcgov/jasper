using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        // Find all unresolved orders
        var unresolvedOrders = await _orderRepo.FindAsync(o => o.Status == OrderStatus.Pending); // check this
        if (unresolvedOrders == null || !unresolvedOrders.Any())
        {
            _logger.LogInformation("No unresolved orders found");
            return;
        }

        var reminderThresholdDays = int.TryParse(_configuration.GetNonEmptyValue("ORDER_REMINDER_THRESHOLD_DAYS"), out var reminderDays) 
            ? reminderDays 
            : 5;
        var reassignmentThresholdDays = int.TryParse(_configuration.GetNonEmptyValue("ORDER_REASSIGNMENT_THRESHOLD_DAYS"), out var reassignDays) 
            ? reassignDays 
            : 10;

        var ordersNeedingReminder = new List<Order>();
        var ordersNeedingReassignment = new List<Order>();
        var reminderFromNow = DateTime.UtcNow.AddDays(-reminderThresholdDays);
        var reassignmentFromNow = DateTime.UtcNow.AddDays(-reassignmentThresholdDays);

        foreach (var order in unresolvedOrders)
        {
            if (order.Ent_Dtm <= reassignmentFromNow)
            {
                ordersNeedingReassignment.Add(order);
            }
            else if (order.Ent_Dtm <= reminderFromNow)
            {
                ordersNeedingReminder.Add(order);
            }
        }

        _logger.LogInformation(
            "Found {ReminderCount} orders needing reminders and {ReassignCount} orders needing reassignment",
            ordersNeedingReminder.Count,
            ordersNeedingReassignment.Count);

        // Process reminders
        foreach (var order in ordersNeedingReminder)
        {
            await SendReminderToJudge(order);
        }

        // Process reassignments
        foreach (var order in ordersNeedingReassignment)
        {
            await ReassignOrderToRAJ(order);
        }

        _logger.LogInformation("Order reminder job completed");
    }

    private async Task SendReminderToJudge(Order order)
    {
        try
        {
            var judgeId = order.OrderRequest?.Referral?.SentToPartId;
            if (!judgeId.HasValue)
            {
                _logger.LogWarning("Cannot send reminder - no judge assigned to order {OrderId}", order.Id);
                return;
            }

            var judge = await _judgeService.GetJudge(judgeId.Value);
            if (judge == null)
            {
                _logger.LogWarning("Judge with id {JudgeId} not found for order {OrderId}", judgeId.Value, order.Id);
                return;
            }

            var databaseUser = await _userService.GetByJudgeIdAsync(judgeId.Value);
            if (databaseUser == null)
            {
                _logger.LogWarning("No judge found for judge {JudgeId} for order {OrderId}",
                    judgeId.Value, order.Id);
                return;
            }
            if (string.IsNullOrWhiteSpace(databaseUser.Email))
            {
                _logger.LogWarning("No email found for judge {JudgeId} for order {OrderId}",
                    judgeId.Value, order.Id);
                return;
            }

            var daysPending = (DateTime.UtcNow - order.Ent_Dtm).Days;
            var emailData = new
            {
                JudgeName = GetJudgeName(judge),
                CaseFileNumber = order.OrderRequest?.CourtFile?.CourtFileNo, 
                StyleOfCause = order.OrderRequest?.CourtFile?.StyleOfCause,
                DaysPending = daysPending,
                LocationName = order.OrderRequest?.CourtFile?.CourtLocationDesc
            };

            await _emailTemplateService.SendEmailTemplateAsync(
                "Order Reminder",
                databaseUser.Email,
                emailData);

            _logger.LogInformation("Reminder sent to judge {JudgeId} for order {OrderId}", judgeId.Value, order.Id);
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
            var judgeId = order.OrderRequest?.Referral?.SentToPartId;
            if (!judgeId.HasValue)
            {
                _logger.LogWarning("Cannot reassign - no judge assigned to order {OrderId}", order.Id);
                return;
            }

            var judge = await _judgeService.GetJudge(judgeId.Value);
            if (judge == null)
            {
                _logger.LogWarning("Judge with id {JudgeId} not found for order {OrderId}", judgeId.Value, order.Id);
                return;
            }

            // Get the judge's RAJ (Regional Administrative Judge)
            var raj = await GetRAJForJudge(judge);
            if (raj == null)
            {
                _logger.LogWarning("No RAJ found for judge {JudgeId} for order {OrderId}",
                    judgeId.Value, order.Id);
                return;
            }

            // Reassign the order to the RAJ
            order.OrderRequest.Referral.SentToPartId = raj.UserId;
            order.OrderRequest.Referral.SentToName = GetJudgeName(raj);
            await _orderRepo.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} reassigned from judge {JudgeId} to RAJ {RajId}",
                order.Id, judgeId.Value, raj.UserId);

            // Send notification emails
            await SendReassignmentNotifications(order, judge, raj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reassign order {OrderId}", order.Id);
        }
    }

    private async Task<Models.Person> GetRAJForJudge(Models.Person judge)
    {
        // TODO: Implement logic to find the RAJ for a given judge
        // This might involve looking up the judge's location/region and finding the corresponding RAJ
        // For now, returning null - this needs to be implemented based on your business logic
        
        _logger.LogWarning("GetRAJForJudge not yet implemented - needs business logic for judge {JudgeId}",
            judge.UserId);
        return null;
    }

    private async Task SendReassignmentNotifications(Order order, Models.Person originalJudge, Models.Person raj)
    {
        var daysPending = (DateTime.UtcNow - order.Ent_Dtm).Days;
        var emailData = new
        {
            OriginalJudgeName = GetJudgeName(originalJudge),
            RAJName = GetJudgeName(raj),
            CaseFileNumber = order.OrderRequest?.CourtFile?.CourtFileNo,
            StyleOfCause = order.OrderRequest?.CourtFile?.StyleOfCause,
            DaysPending = daysPending,
            LocationName = order.OrderRequest?.CourtFile?.CourtLocationDesc
        };

        // Send email to RAJ
        var rajUser = await _userService.GetByJudgeIdAsync(raj.UserId.GetValueOrDefault());
        if (rajUser != null && !string.IsNullOrWhiteSpace(rajUser.Email))
        {
            await _emailTemplateService.SendEmailTemplateAsync(
                "Order Reassignment",
                rajUser.Email,
                emailData);
            _logger.LogInformation("Reassignment notification sent to RAJ {RajId} for order {OrderId}",
                raj.UserId, order.Id);
        }

        // Send email to product manager
        if (!string.IsNullOrWhiteSpace(_options.ProductManagerEmail))
        {
            await _emailTemplateService.SendEmailTemplateAsync(
                "Order Reassignment Alert",
                _options.ProductManagerEmail,
                emailData);
            _logger.LogInformation("Reassignment alert sent to product manager for order {OrderId}", order.Id);
        }
    }

    private static string GetJudgeName(Models.Person judge)
    {
        var latestName = judge.Names?.FirstOrDefault();
        if (latestName == null)
            return "Judge";

        return $"{latestName.FirstName} {latestName.LastName}".Trim();
    }
}
