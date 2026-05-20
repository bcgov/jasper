using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    public class UserSeeder(ILogger<UserSeeder> logger, IConfiguration config) : SeederBase<JasperDbContext>(logger)
    {
        private readonly IConfiguration _config = config;

        public override int Order => 4;


        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            try
            {
                var groups = await context.Groups.ToListAsync();
                var roles = await context.Roles.ToListAsync();

                var adminRole = roles.FirstOrDefault(r => r.Name == Role.ADMIN);
                if (adminRole == null)
                {
                    this.Logger.LogError("Admin role not found, cannot seed users without it.");
                    return;
                }

                // Get default users from env variable
                var defaultUsersJson = _config["DEFAULT_USERS"];
                Console.WriteLine(defaultUsersJson);
                var usersObj = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(defaultUsersJson);
                var users = new List<User>();
                foreach (var userObj in usersObj)
                {
                    var newUser = new User
                    {
                        FirstName = userObj["FirstName"],
                        LastName = userObj["LastName"],
                        Email = userObj["Email"],
                        IsActive = true,
                        IsPendingRegistration = false,
                        GroupIds = [groups.First(g => g.Name == userObj["Group"]).Id],
                        RoleIds = [userObj.TryGetValue("Role", out var roleName)
                            ? roles.First(r => r.Name == roleName).Id
                            : adminRole.Id]
                    };
                    users.Add(newUser);
                }

                this.Logger.LogInformation("\tUpdating users...");

                foreach (var user in users)
                {
                    var u = await context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Email == user.Email);
                    if (u == null)
                    {
                        this.Logger.LogInformation("\t{email} does not exist, adding it...", user.Email);
                        await context.Users.AddAsync(user);
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error when seeding User data: {message}", ex.Message);
            }
        }
    }
}
