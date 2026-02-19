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
/// Recurring job to retry submission for errored orders that are approved or unapproved.
/// </summary>
public class RetryErroredOrderSubmitJob(
    IRepositoryBase<Order> orderRepo,
    IBackgroundJobClient backgroundJobClient,
    IOptions<JobsRetrySubmitOrderOptions> options,
    ILogger<RetryErroredOrderSubmitJob> logger) : IRecurringJob
{
    private readonly IRepositoryBase<Order> _orderRepo = orderRepo;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly JobsRetrySubmitOrderOptions _options = options.Value;
    private readonly ILogger<RetryErroredOrderSubmitJob> _logger = logger;

    public string JobName => nameof(RetryErroredOrderSubmitJob);
    public string CronSchedule => _options.CronSchedule;

    public async Task Execute()
    {
        var erroredOrders = await _orderRepo.FindAsync(o =>
            (o.Status == OrderStatus.Approved || o.Status == OrderStatus.Unapproved)
            && o.SubmitStatus == SubmitStatus.Error);

        var orderIds = erroredOrders?.Where(o => o.SubmitAttempts < _options.MaxRetries || o.SubmitAttempts == null)
            .Select(o => o.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        if (orderIds.Count == 0)
        {
            _logger.LogInformation("No errored orders found for resubmission.");
            return;
        }

        _logger.LogInformation("Retrying submission for {Count} errored orders.", orderIds.Count);

        foreach (var orderId in orderIds)
        {
            _backgroundJobClient.Enqueue<SubmitOrderJob>(job => job.Execute(orderId));
        }
    }
}
