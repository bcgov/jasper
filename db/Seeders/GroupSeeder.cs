using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    public class GroupSeeder(ILogger<GroupSeeder> logger) : SeederBase<JasperDbContext>(logger)
    {
        public override int Order => 3;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var roles = await context.Roles.ToListAsync();
            var trainingAdminRoles = new List<string> { Role.ADMIN, Role.TRAINER };
            var judiciaryRoles = new List<string> { Role.JUDGE };
            var groups = Group.ALL_GROUPS;

            var groupRoles = new Dictionary<string, IEnumerable<string>>
            {
                [Group.TRAINING_AND_ADMIN] = trainingAdminRoles,
                [Group.JUDICIARY] = judiciaryRoles,
            };

            foreach (var group in groups)
            {
                if (groupRoles.TryGetValue(group.Name, out var roleNames))
                {
                    group.RoleIds = roles
                        .Where(r => roleNames.Contains(r.Name))
                        .Select(r => r.Id)
                        .ToList();
                }
                else
                {
                    this.Logger.LogInformation("\tRoles for {name} Group is missing...", group.Name);
                }
            }

            this.Logger.LogInformation("\tUpdating groups...");

            foreach (var group in groups)
            {
                var g = await context.Groups.AsQueryable().FirstOrDefaultAsync(g => g.Name == group.Name);
                if (g == null)
                {
                    this.Logger.LogInformation("\t{name} does not exist, adding it...", group.Name);
                    await context.Groups.AddAsync(group);
                }
                else
                {
                    this.Logger.LogInformation("\tUpdating fields for {name}...", group.Name);
                    g.Description = group.Description;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
