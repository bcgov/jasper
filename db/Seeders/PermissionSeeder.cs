using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
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

            this.Logger.LogInformation("Updating permissions...");
            foreach (var permission in permissions)
            {
                this.Logger.LogInformation($"Looking up {permission.Code}...");

                var filter = Builders<Permission>.Filter.Eq(p => p.Code, permission.Code);

                var p = await context.Permissions.Find(filter).FirstOrDefaultAsync();
                if (p == null)
                {
                    this.Logger.LogInformation($"{permission.Code} does not exist, adding it...");
                    await context.Permissions.InsertOneAsync(permission);
                    this.Logger.LogInformation("Inserted");
                }
                else
                {
                    this.Logger.LogInformation($"Updating fields for {permission.Code}...");
                    p.Name = permission.Name;
                    p.Description = permission.Description;
                    p.IsActive = permission.IsActive;

                    await context.Permissions.ReplaceOneAsync(filter, p);
                }
            }

            this.Logger.LogInformation("Removing permissions that don't exist anymore...");
            var findByCodeToDeleteFilter = Builders<Permission>.Filter.Nin(p => p.Code, permissions.Select(pp => pp.Code));

            var obsoletePermissions = await context.Permissions
                .Find(findByCodeToDeleteFilter)
                .ToListAsync();

            foreach (var permission in obsoletePermissions)
            {
                this.Logger.LogInformation($"Removing {permission.Code}...");
                var findByCodeFilter = Builders<Permission>.Filter.Eq(p => p.Code, permission.Code);
                await context.Permissions.DeleteOneAsync(findByCodeFilter);
            }

            this.Logger.LogInformation("Listing permissions...");
            var savedPermissions = await context.Permissions.Find(p => true).ToListAsync();
            foreach (var permission in savedPermissions)
            {
                this.Logger.LogInformation(permission.Code);
            }

        }
    }
}
