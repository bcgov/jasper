using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

/// <summary>
/// Recurring job to retry submission for errored orders that are approved or unapproved.
/// </summary>
public class RetryErroredOrderSubmitJob(
    IRepositoryBase<Order> orderRepo,
    IBackgroundJobClient backgroundJobClient,
    IOptions<JobsRetrySubmitOrderOptions> options,
    ILogger<RetryErroredOrderSubmitJob> logger) : IRecurringJob
{
    private const string UrgentPriorityType = "URG";

    private readonly IRepositoryBase<Order> _orderRepo = orderRepo;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly JobsRetrySubmitOrderOptions _options = options.Value;
    private readonly ILogger<RetryErroredOrderSubmitJob> _logger = logger;

    public string JobName => nameof(RetryErroredOrderSubmitJob);
    public string CronSchedule => _options.CronSchedule;

    public async Task Execute()
    {
        await RetryErroredOrderSubmitJobHelper.ExecuteAsync(
            _orderRepo,
            _backgroundJobClient,
            _options.MaxRetries,
            o => !HasUrgentPriority(o),
            _logger,
            "No errored orders found for resubmission.",
            "Retrying submission for {Count} errored orders.");
    }

    private static bool HasUrgentPriority(Order order)
    {
        return RetryErroredOrderSubmitJobHelper.HasPriorityType(order, UrgentPriorityType);
    }
}
