﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Scv.Db.Models;

namespace Scv.Db.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            var entries = context.ChangeTracker
                .Entries()
                .Where(e => e.Entity is AuditableObject
                    && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (AuditableObject)entry.Entity;
                var now = DateTime.UtcNow;

                // Get currentUserId will be implemented later

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = now;
                }
                entity.UpdatedDate = now;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
