using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders;

public class RoleAliasSeeder(ILogger<RoleAliasSeeder> logger) : SeederBase<JasperDbContext>(logger)
{
    override public int Order => 9;

    protected override async Task ExecuteAsync(JasperDbContext context)
    {
        var roles = await context.Roles.ToListAsync();
        var aliases = RoleAlias.ALL_ROLE_ALIASES;

        var roleAliases = new Dictionary<string, string>
        {
            [RoleAlias.CHIEF_JUDGE_ACJ] = Role.ACJ_CHIEF_JUDGE,
            [RoleAlias.CHIEF_JUDGE_ACJ_ALT] = Role.ACJ_CHIEF_JUDGE,
            [RoleAlias.JUDGE] = Role.JUDGE,
            [RoleAlias.JUDGE_ALT] = Role.JUDGE,
            [RoleAlias.JUDGE_TRAINING] = Role.ADMIN,
            [RoleAlias.OCJ_HELP_DESK] = Role.OCJ_SERVICE_DESK,
            [RoleAlias.OCJ_HELP_DESK_ALT] = Role.OCJ_SERVICE_DESK,
            [RoleAlias.OCJ_IT] = Role.OCJ_SERVICE_DESK,
            [RoleAlias.PRODUCT_MANAGER] = Role.PO_MANAGER,
            [RoleAlias.PRODUCT_MANAGER_ALT] = Role.PO_MANAGER,
            [RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE] = Role.RAJ,
            [RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE_ALT] = Role.RAJ,
            [RoleAlias.SENIOR_JUDGE] = Role.JUDGE,
            [RoleAlias.USER_ROLE_ADMIN] = Role.ADMIN,
        };

        foreach (var alias in aliases)
        {
            if (roleAliases.TryGetValue(alias.Name, out var roleName))
            {
                alias.RoleId = roles.FirstOrDefault(g => g.Name == roleName)?.Id;
            }
            else
            {
                this.Logger.LogInformation("\tRole for {Name} Role alias is missing...", alias.Name);
            }
        }

        this.Logger.LogInformation("\tUpdating role aliases...");

        foreach (var alias in aliases)
        {
            var ra = await context.RoleAliases.AsQueryable().FirstOrDefaultAsync(r => r.Name == alias.Name);
            if (ra == null)
            {
                this.Logger.LogInformation("\tRole alias {Name} does not exist, adding it...", alias.Name);
                await context.RoleAliases.AddAsync(alias);
            }
            else
            {
                this.Logger.LogInformation("\tUpdating from role {RoleId} to role {NewRole} for alias for {Name}...", ra.RoleId, alias?.RoleId, alias.Name);
                ra.RoleId = alias.RoleId;
            }
        }

        await context.SaveChangesAsync();
    }
}
