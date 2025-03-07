using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Seeders
{
    public class RoleSeeder(ILogger<RoleSeeder> logger) : SeederBase<JasperDbContext>(logger)
    {
        public override int Order => 2;

        protected override async Task ExecuteAsync(JasperDbContext context)
        {
            var permissions = await context.Permissions.ToListAsync();

            var adminPermissions = new List<string>
            {
                Permission.UPDATE_PERMISSIONS_GROUPS,
                Permission.UPDATE_POSITIONS,
                Permission.UPDATE_POSITIONS_PCSS_MAPPINGS,
                Permission.LOCK_UNLOCK_USERS,
                Permission.VIEW_DASHBOARD,
                Permission.VIEW_OWN_SCHEDULE,
                Permission.VIEW_OTHER_JUDGE_SCHEDULE,
                Permission.VIEW_OTHER_CALENDARS,
                Permission.VIEW_RESERVED_JUDGMENTS,
                Permission.VIEW_COURT_LIST,
                Permission.RETRIEVE_OWN_COURT_LIST,
                Permission.SEARCH_WITHIN_COURT_LIST,
                Permission.FILTER_WITHIN_COURT_LIST,
                Permission.SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE,
                Permission.SEARCH_FOR_COURT_FILES,
                Permission.ADVANCED_SEARCH_FOR_COURT_FILES,
                Permission.SORT_SEARCH_RESULTS,
                Permission.VIEW_COURT_FILE,
                Permission.VIEW_YOUTH_CRIMINAL_FILE,
                Permission.VIEW_SUMMARY,
                Permission.VIEW_ADJUDICATOR_RESTRICTIONS,
                Permission.VIEW_ACCUSED,
                Permission.VIEW_PARTIES,
                Permission.VIEW_CHILDREN,
                Permission.VIEW_CASE_DETAILS,
                Permission.VIEW_APPEARANCES,
                Permission.VIEW_SENTENCE_ORDER_DETAILS,
                Permission.VIEW_SINGLE_DOCUMENT,
                Permission.VIEW_MULTIPLE_DOCUMENTS,
                Permission.VIEW_SIDE_BY_SIDE_DOCUMENTS,
                Permission.ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY,
                Permission.VIEW_QUICK_LINKS,
                Permission.SET_DEFAULT_HOME_SCREEN,
                Permission.ADD_EDIT_OWN_NOTES_ONLY
            };

            var judgePermissions = new List<string>
            {
                Permission.VIEW_DASHBOARD,
                Permission.VIEW_OWN_SCHEDULE,
                Permission.VIEW_OTHER_CALENDARS,
                Permission.VIEW_RESERVED_JUDGMENTS,
                Permission.VIEW_COURT_LIST,
                Permission.RETRIEVE_OWN_COURT_LIST,
                Permission.SEARCH_WITHIN_COURT_LIST,
                Permission.FILTER_WITHIN_COURT_LIST,
                Permission.SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE,
                Permission.SEARCH_FOR_COURT_FILES,
                Permission.ADVANCED_SEARCH_FOR_COURT_FILES,
                Permission.SORT_SEARCH_RESULTS,
                Permission.VIEW_COURT_FILE,
                Permission.VIEW_YOUTH_CRIMINAL_FILE,
                Permission.VIEW_SUMMARY,
                Permission.VIEW_ADJUDICATOR_RESTRICTIONS,
                Permission.VIEW_ACCUSED,
                Permission.VIEW_PARTIES,
                Permission.VIEW_CHILDREN,
                Permission.VIEW_CASE_DETAILS,
                Permission.VIEW_APPEARANCES,
                Permission.VIEW_SENTENCE_ORDER_DETAILS,
                Permission.VIEW_SINGLE_DOCUMENT,
                Permission.VIEW_MULTIPLE_DOCUMENTS,
                Permission.VIEW_SIDE_BY_SIDE_DOCUMENTS,
                Permission.ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY,
                Permission.VIEW_QUICK_LINKS,
                Permission.SET_DEFAULT_HOME_SCREEN,
                Permission.ADD_EDIT_OWN_NOTES_ONLY
            };

            var trainerPermissions = new List<string>
            {
                Permission.VIEW_DASHBOARD,
                Permission.VIEW_OWN_SCHEDULE,
                Permission.VIEW_OTHER_JUDGE_SCHEDULE,
                Permission.VIEW_OTHER_CALENDARS,
                Permission.VIEW_RESERVED_JUDGMENTS,
                Permission.VIEW_COURT_LIST,
                Permission.RETRIEVE_OWN_COURT_LIST,
                Permission.SEARCH_WITHIN_COURT_LIST,
                Permission.FILTER_WITHIN_COURT_LIST,
                Permission.SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE,
                Permission.SEARCH_FOR_COURT_FILES,
                Permission.ADVANCED_SEARCH_FOR_COURT_FILES,
                Permission.SORT_SEARCH_RESULTS,
                Permission.VIEW_COURT_FILE,
                Permission.VIEW_YOUTH_CRIMINAL_FILE,
                Permission.VIEW_SUMMARY,
                Permission.VIEW_ADJUDICATOR_RESTRICTIONS,
                Permission.VIEW_ACCUSED,
                Permission.VIEW_PARTIES,
                Permission.VIEW_CHILDREN,
                Permission.VIEW_CASE_DETAILS,
                Permission.VIEW_APPEARANCES,
                Permission.VIEW_SENTENCE_ORDER_DETAILS,
                Permission.VIEW_SINGLE_DOCUMENT,
                Permission.VIEW_MULTIPLE_DOCUMENTS,
                Permission.VIEW_SIDE_BY_SIDE_DOCUMENTS,
                Permission.ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY,
                Permission.VIEW_QUICK_LINKS,
                Permission.SET_DEFAULT_HOME_SCREEN,
                Permission.ADD_EDIT_OWN_NOTES_ONLY
            };

            var roles = Role.ALL_ROLES;

            var rolePermissions = new Dictionary<string, IEnumerable<string>>
            {
                [Role.ADMIN] = adminPermissions,
                [Role.TRAINER] = trainerPermissions,
                [Role.JUDGE] = judgePermissions
            };

            foreach (var role in roles)
            {
                if (rolePermissions.TryGetValue(role.Name, out var permissionCodes))
                {
                    role.PermissionIds = permissions
                        .Where(p => permissionCodes.Contains(p.Code))
                        .Select(p => p.Id)
                        .ToList();
                }
                else
                {
                    this.Logger.LogInformation("\tPermissions for Role {name} is missing...", role.Name);
                }
            }

            this.Logger.LogInformation("\tUpdating roles...");

            foreach (var role in roles)
            {
                var r = await context.Roles.AsQueryable().FirstOrDefaultAsync(r => r.Name == role.Name);
                if (r == null)
                {
                    this.Logger.LogInformation("\t{name} does not exist, adding it...", role.Name);
                    await context.Roles.AddAsync(role);
                }
                else
                {
                    this.Logger.LogInformation("\tUpdating fields for {name}...", role.Name);
                    r.Description = role.Description;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
