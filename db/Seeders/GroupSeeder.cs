using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    internal class GroupSeeder(ILogger logger) : SeederBase<JasperDbContext>(logger)
    {
        public const string TRAINING_AND_ADMIN = "Training and Administration";
        public const string JUDICIARY = "Judiciary";

        public override int Order => 3;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var roles = await context.Roles.ToListAsync();

            var groups = new List<Group>
            {
                new() {
                    Name = TRAINING_AND_ADMIN,
                    Description = "Training and Admin group",
                    RoleIds = [..roles
                        .Where(r => r.Name == "Admin" || r.Name == "Trainer")
                        .Select(r => r.Id)
                    ]
                },
                new() {
                    Name = JUDICIARY,
                    Description = "Judiciary group",
                    RoleIds = [..roles
                        .Where(r => r.Name == "Judge")
                        .Select(r => r.Id)
                    ]
                }
            };

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
