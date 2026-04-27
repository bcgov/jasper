using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public interface INotificationRepository : IPostgresRepositoryBase<SignalROutboxMessage, long>
{
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SignalROutboxMessage>> GetPendingAsync(
        string channel,
        DateTimeOffset minimumAge,
        int batchSize,
        CancellationToken cancellationToken);
    Task<SignalROutboxMessage> GetByIdNoTrackingAsync(long id, CancellationToken cancellationToken);
    Task<SignalROutboxMessage> GetByAckGuidNoTrackingAsync(Guid ackGuid, CancellationToken cancellationToken);
    Task<bool> TryClaimDeliveryAsync(long id, string instanceId, CancellationToken cancellationToken);
    Task ResetDeliveryAsync(long id, CancellationToken cancellationToken);
}

public class NotificationRepository(ScvDbContext dbContext)
    : PostgresRepositoryBase<SignalROutboxMessage, long>(dbContext), INotificationRepository
{
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var expired = await _dbSet
            .Where(message => message.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return 0;
        }

        _dbSet.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }

    public async Task<IReadOnlyList<SignalROutboxMessage>> GetPendingAsync(
        string channel,
        DateTimeOffset minimumAge,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var pending = await _dbSet
            .AsNoTracking()
            .Where(message => message.DeliveredAt == null &&
                message.Channel == channel &&
                message.CreatedAt <= minimumAge &&
                message.CreatedAt.AddMinutes(message.OfflineMinutes) >= now)
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return pending;
    }

    public Task<SignalROutboxMessage> GetByIdNoTrackingAsync(long id, CancellationToken cancellationToken)
    {
        return _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
    }

    public Task<SignalROutboxMessage> GetByAckGuidNoTrackingAsync(Guid ackGuid, CancellationToken cancellationToken)
    {
        return _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(message => message.AckGuid == ackGuid, cancellationToken);
    }

    public async Task<bool> TryClaimDeliveryAsync(long id, string instanceId, CancellationToken cancellationToken)
    {
        var message = await _dbSet
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
        if (message == null || message.DeliveredAt != null)
        {
            return false;
        }

        message.DeliveredAt = DateTimeOffset.UtcNow;
        message.DeliveredBy = instanceId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task ResetDeliveryAsync(long id, CancellationToken cancellationToken)
    {
        return ResetDeliveryInternalAsync(id, cancellationToken);
    }

    private async Task ResetDeliveryInternalAsync(long id, CancellationToken cancellationToken)
    {
        var message = await _dbSet.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
        if (message == null)
        {
            return;
        }

        message.DeliveredAt = null;
        message.DeliveredBy = null;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
