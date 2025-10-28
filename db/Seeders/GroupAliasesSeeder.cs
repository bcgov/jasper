using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    public class GroupAliasSeeder(ILogger<GroupSeeder> logger) : SeederBase<JasperDbContext>(logger)
    {
        public override int Order => 5;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var groups = await context.Groups.ToListAsync();
            var aliases = GroupAlias.ALL_GROUP_ALIASES;

            var groupAliases = new Dictionary<string, string>
            {
                [GroupAlias.JUDGE] = Group.JUDICIARY,
                [GroupAlias.CHIEF_JUDGE_ACJ] = Group.JUDICIARY,
                [GroupAlias.REGIONAL_ADMINISTRATIVE_JUDGE] = Group.JUDICIARY,
                [GroupAlias.JUDGE_NO_CLDC] = Group.JUDICIARY,
                [GroupAlias.JUDGE_WITH_COURT_LIST] = Group.JUDICIARY,
                [GroupAlias.JUDGE_JJ_OUTLOOK_INTEGRATION] = Group.JUDICIARY,
                [GroupAlias.SENIOR_JUDGE_COURT_LIST] = Group.JUDICIARY,
                [GroupAlias.SENIOR_JUDGE] = Group.JUDICIARY,
                [GroupAlias.PRODUCT_MANAGER] = Group.TRAINING_AND_ADMIN,
                [GroupAlias.OCJ_HELP_DESK] = Group.TRAINING_AND_ADMIN,
                [GroupAlias.USER_ROLE_ADMIN] = Group.TRAINING_AND_ADMIN,
                [GroupAlias.JUDGE_TRAINING_ROLE] = Group.TRAINING_AND_ADMIN,
                [GroupAlias.RAJ_WITH_CC_VIEW] = Group.TRAINING_AND_ADMIN,
            };

            foreach (var alias in aliases)
            {
                if (groupAliases.TryGetValue(alias.Name, out var groupName))
                {
                    alias.GroupId = groups.FirstOrDefault(g => g.Name == groupName)?.Id;
                }
                else
                {
                    this.Logger.LogInformation("\tGroup for {name} Group alias is missing...", alias.Name);
                }
            }

            this.Logger.LogInformation("\tUpdating group aliases...");

            foreach (var alias in aliases)
            {
                var ga = await context.GroupAliases.AsQueryable().FirstOrDefaultAsync(g => g.Name == alias.Name);
                if (ga == null)
                {
                    this.Logger.LogInformation("\tGroup alias {name} does not exist, adding it...", alias.Name);
                    await context.GroupAliases.AddAsync(alias);
                }
                else
                {
                    this.Logger.LogInformation("\tUpdating from group {group} to group {newGroup} for alias for {name}...", ga.GroupId, alias?.GroupId, alias.Name);
                    ga.GroupId = alias.GroupId;

                }
            }

            await context.SaveChangesAsync();
        }
    }
}
