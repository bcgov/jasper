using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Scv.Db.Contexts;
using Scv.Db.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Db.Seeders
{
    public class PermissionSeeder(ILogger<PermissionSeeder> logger) : SeederBase<JasperDbContext>(logger)
    {
        public override int Order => 1;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var permissions = Permission.ALL_PERMISIONS;

            this.Logger.LogInformation("\tUpdating permissions...");
            foreach (var permission in permissions)
            {
                var p = await context.Permissions.AsQueryable().FirstOrDefaultAsync(r => r.Code == permission.Code);
                if (p == null)
                {
                    await context.Permissions.AddAsync(permission);
                }
                else
                {
                    p.Name = permission.Name;
                    p.Description = permission.Description;
                    p.IsActive = permission.IsActive;
                }
            }

            var obsoletePermissions = await context.Permissions
                .Where(p => !permissions.Select(pp => pp.Code).Contains(p.Code))
                .ToListAsync();

            foreach (var permission in obsoletePermissions)
            {
                this.Logger.LogInformation("\tRemoving {code}...", permission.Code);
                context.Permissions.Remove(permission);
            }

            await context.SaveChangesAsync();
        }
    }
}
