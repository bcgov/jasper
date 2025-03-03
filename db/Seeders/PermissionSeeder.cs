using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    internal class PermissionSeeder : SeederBase<JasperDbContext>
    {
        public PermissionSeeder(ILogger logger) : base(logger)
        {
            this.Order = 1;
        }

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var permissions = Permission.ALL_PERMISIONS;

            this.Logger.LogInformation("\tUpdating permissions...");
            foreach (var permission in permissions)
            {
                this.Logger.LogInformation("\tLooking up {code}...", permission.Code);

                var p = context.Permissions.FirstOrDefault(p => p.Code == permission.Code);
                if (p == null)
                {
                    this.Logger.LogInformation("\t{code} does not exist, adding it...", permission.Code);
                    await context.Permissions.AddAsync(permission);
                }
                else
                {
                    this.Logger.LogInformation("\tUpdating fields for {code}...", permission.Code);
                    p.Name = permission.Name;
                    p.Description = permission.Description;
                    p.IsActive = permission.IsActive;
                }
            }

            this.Logger.LogInformation("\tRemoving permissions that don't exist anymore...");
            var obsoletePermissions = context.Permissions
                .Where(p => !permissions.Select(pp => pp.Code).Contains(p.Code))
                .ToList();

            foreach (var permission in obsoletePermissions)
            {
                this.Logger.LogInformation("\tRemoving {code}...", permission.Code);
                context.Permissions.Remove(permission);
            }

            this.Logger.LogInformation("\tListing permissions...");
            var savedPermissions = context.Permissions.ToList();
            foreach (var permission in savedPermissions)
            {
                this.Logger.LogInformation("\t{code}", permission.Code);
            }

            await context.SaveChangesAsync();
        }
    }
}
