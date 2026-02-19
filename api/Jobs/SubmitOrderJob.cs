using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

/// <summary>
/// Background job to submit a reviewed order to CSO.
/// </summary>
[JobFailureEmailFilter]
public class SubmitOrderJob(
    IOrderService orderService,
    ILogger<SubmitOrderJob> logger)
{
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<SubmitOrderJob> _logger = logger;

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task Execute(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        var result = await _orderService.SubmitOrder(orderId);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Submit order failed for order {OrderId}: {Errors}",
                orderId,
                string.Join(", ", result.Errors));
            throw new InvalidOperationException($"Failed to submit order {orderId}.");
        }
    }
}
