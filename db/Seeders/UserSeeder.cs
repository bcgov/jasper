using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    public class UserSeeder(ILogger<UserSeeder> logger) : SeederBase<JasperDbContext>(logger)
    {
        public override int Order => 4;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var groups = await context.Groups.ToListAsync();

            var users = new List<User>
            {
                new() {
                    FirstName = "Ronaldo",
                    LastName = "Macapobre",
                    Email = "ronaldo.macapobre@gov.bc.ca",
                    IsActive = true,
                    GroupIds = [groups.First(g => g.Name == Group.TRAINING_AND_ADMIN).Id]
                }
            };

            this.Logger.LogInformation("\tUpdating users...");

            foreach (var user in users)
            {
                var u = await context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Email == user.Email);
                if (u == null)
                {
                    this.Logger.LogInformation("\t{email} does not exist, adding it...", user.Email);
                    await context.Users.AddAsync(user);
                }
                else
                {
                    this.Logger.LogInformation("\tUpdating fields for {email}...", user.Email);
                    u.FirstName = user.FirstName;
                    u.LastName = user.LastName;
                    u.GroupIds = user.GroupIds;
                    u.IsActive = user.IsActive;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
