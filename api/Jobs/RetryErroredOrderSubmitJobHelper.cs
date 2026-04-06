using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Order;

namespace Scv.Api.Jobs;

internal static class RetryErroredOrderSubmitJobHelper
{
    public static async Task ExecuteAsync(
        IRepositoryBase<Order> orderRepo,
        IBackgroundJobClient backgroundJobClient,
        int maxRetries,
        Func<Order, bool> includeOrder,
        ILogger logger,
        string noOrdersLogMessage,
        string retryingLogMessage)
    {
        var erroredOrders = await orderRepo.FindAsync(o =>
            (o.Status == OrderStatus.Approved || o.Status == OrderStatus.Unapproved || o.Status == OrderStatus.AwaitingDocumentation)
            && o.SubmitStatus == SubmitStatus.Error);

        var orderIds = erroredOrders?
            .Where(includeOrder)
            .Where(o => o.SubmitAttempts < maxRetries || o.SubmitAttempts == null)
            .Select(o => o.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        if (orderIds.Count == 0)
        {
            logger.LogInformation(noOrdersLogMessage);
            return;
        }

        logger.LogInformation(retryingLogMessage, orderIds.Count);

        foreach (var orderId in orderIds)
        {
            backgroundJobClient.Enqueue<SubmitOrderJob>(job => job.Execute(orderId));
        }
    }

    public static bool HasPriorityType(Order order, string priorityType)
    {
        var orderPriorityType = order?.OrderRequest?.Referral?.PriorityType;
        return string.Equals(orderPriorityType, priorityType, StringComparison.OrdinalIgnoreCase);
    }
}