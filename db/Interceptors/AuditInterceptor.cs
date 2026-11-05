using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scv.Core.Infrastructure;
using Scv.Db.Models;

namespace Scv.Db.Interceptors
{
    public class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
    {
        // Used when there is no authenticated user (e.g. seeders, migrations, background jobs)
        public const string SystemUserId = "system";

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyAudit(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            ApplyAudit(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAudit(DbContext context)
        {
            if (context is null)
            {
                return;
            }

            var userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? SystemUserId : currentUser.UserId;
            var now = DateTime.UtcNow;

            var entries = context.ChangeTracker
                .Entries<AuditableObject>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var entity = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.Ent_Dtm = now;
                    entity.Ent_UserId = userId;
                }

                entity.Upd_Dtm = now;
                entity.Upd_UserId = userId;
            }
        }
    }
}
