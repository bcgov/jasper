using System;
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
        await RetryErroredOrderSubmitJobHelper.ExecuteAsync(
            _orderRepo,
            _backgroundJobClient,
            _options.MaxRetries,
            HasUrgentPriority,
            _logger,
            "No errored urgent orders found for resubmission.",
            "Retrying submission for {Count} errored urgent orders.");
    }

    private bool HasUrgentPriority(Order order)
    {
        return RetryErroredOrderSubmitJobHelper.HasPriorityType(order, _options.PriorityType);
    }
}
