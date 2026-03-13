using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Jobs;

/// <summary>
/// Recurring job to retry submission for errored urgent-priority orders.
/// </summary>
public class RetryUrgentErroredOrderSubmitJob(
    IRepositoryBase<Order> orderRepo,
    IBackgroundJobClient backgroundJobClient,
    IOptions<JobsRetryUrgentSubmitOrderOptions> options,
    ILogger<RetryUrgentErroredOrderSubmitJob> logger) : IRecurringJob
{
    private readonly IRepositoryBase<Order> _orderRepo = orderRepo;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly JobsRetryUrgentSubmitOrderOptions _options = options.Value;
    private readonly ILogger<RetryUrgentErroredOrderSubmitJob> _logger = logger;

    public string JobName => nameof(RetryUrgentErroredOrderSubmitJob);
    public string CronSchedule => _options.CronSchedule;

    public async Task Execute()
    {
        var erroredOrders = await _orderRepo.FindAsync(o =>
            (o.Status == OrderStatus.Approved || o.Status == OrderStatus.Unapproved || o.Status == OrderStatus.AwaitingDocumentation)
            && o.SubmitStatus == SubmitStatus.Error);

        var orderIds = erroredOrders?
            .Where(HasUrgentPriority)
            .Where(o => o.SubmitAttempts < _options.MaxRetries || o.SubmitAttempts == null)
            .Select(o => o.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (orderIds.Count == 0)
        {
            _logger.LogInformation("No errored urgent orders found for resubmission.");
            return;
        }

        _logger.LogInformation("Retrying submission for {Count} errored urgent orders.", orderIds.Count);

        foreach (var orderId in orderIds)
        {
            _backgroundJobClient.Enqueue<SubmitOrderJob>(job => job.Execute(orderId));
        }
    }

    private bool HasUrgentPriority(Order order)
    {
        var priorityType = order?.OrderRequest?.Referral?.PriorityType;
        return string.Equals(priorityType, _options.PriorityType, StringComparison.OrdinalIgnoreCase);
    }
}
