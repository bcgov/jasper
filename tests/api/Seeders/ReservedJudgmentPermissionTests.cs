using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Seeders;
using tests.api.Services;
using Xunit;

namespace tests.api.Seeders;

public class ReservedJudgmentPermissionTests
{
    [Fact]
    public void Permission_IsRegisteredOnceAndActive()
    {
        var permission = Assert.Single(Permission.ALL_PERMISIONS,
            p => p.Code == Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE);
        Assert.True(permission.IsActive);
        Assert.Equal("Edit Reserved Judgment Due Dates", permission.Name);
        Assert.False(string.IsNullOrWhiteSpace(permission.Description));
        Assert.Equal(Permission.ALL_PERMISIONS.Count,
            Permission.ALL_PERMISIONS.Select(p => p.Code).Distinct().Count());
    }

    [MongoCaseFact]
    public async Task Seed_AssignsExactlyFourRolesIsIdempotentAndPreservesEstablishedResetBehaviour()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("JASPER_CASE_TEST_MONGO_URI"));
        var databaseName = "jasper_due_date_permissions_test_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<JasperDbContext>().UseMongoDB(client, databaseName).Options;
        var permissionSeeder = new PermissionSeeder(NullLogger<PermissionSeeder>.Instance);
        var roleSeeder = new RoleSeeder(NullLogger<RoleSeeder>.Instance);
        try
        {
            Dictionary<string, string[]> defaults;
            string permissionId;
            await using (var context = new JasperDbContext(options))
            {
                await permissionSeeder.SeedAsync(context);
                await roleSeeder.SeedAsync(context);
                var permissions = await context.Permissions.ToListAsync();
                var roles = await context.Roles.ToListAsync();
                permissionId = Assert.Single(permissions,
                    p => p.Code == Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE).Id;
                Assert.Equal(new[] { Role.JUDGE, Role.RAJ, Role.ACJ_CHIEF_JUDGE, Role.DEVELOPER }.Order(),
                    roles.Where(r => r.PermissionIds.Contains(permissionId)).Select(r => r.Name).Order());
                var dashboard = permissions.Single(p => p.Code == Permission.VIEW_DASHBOARD).Id;
                var download = permissions.Single(p => p.Code == Permission.DOWNLOAD_TRANSITORY_DOCUMENTS).Id;
                Assert.Contains(dashboard, roles.Single(r => r.Name == Role.JUDGE).PermissionIds);
                Assert.Contains(dashboard, roles.Single(r => r.Name == Role.ADMIN).PermissionIds);
                Assert.Contains(download, roles.Single(r => r.Name == Role.DEVELOPER).PermissionIds);
                Assert.DoesNotContain(dashboard, roles.Single(r => r.Name == Role.DEVELOPER).PermissionIds);
                defaults = roles.ToDictionary(r => r.Name, r => r.PermissionIds.Order().ToArray());

                // Existing role seeding resets administrator-managed grants and removals.
                roles.Single(r => r.Name == Role.JUDGE).PermissionIds.Remove(permissionId);
                roles.Single(r => r.Name == Role.ADMIN).PermissionIds.Add(permissionId);
                await context.SaveChangesAsync();
            }
            await using (var context = new JasperDbContext(options))
            {
                await permissionSeeder.SeedAsync(context);
                await roleSeeder.SeedAsync(context);
                Assert.Equal(Permission.ALL_PERMISIONS.Count, await context.Permissions.CountAsync());
                var roles = await context.Roles.ToListAsync();
                Assert.Equal(Role.ALL_ROLES.Count, roles.Count);
                Assert.All(roles, role =>
                {
                    Assert.Equal(defaults[role.Name], role.PermissionIds.Order());
                    Assert.Equal(role.PermissionIds.Count, role.PermissionIds.Distinct().Count());
                });
            }
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
}
