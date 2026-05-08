using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.GROUP_ALIASES)]
public class GroupAlias : EntityBase
{
    public static readonly List<GroupAlias> ALL_GROUP_ALIASES =
    [
        new GroupAlias { Name = RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE },
        new GroupAlias { Name = RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE_ALT },
        new GroupAlias { Name = RoleAlias.RAJ_WITH_CC_VIEW },
        new GroupAlias { Name = RoleAlias.JUDGE },
        new GroupAlias { Name = RoleAlias.JUDGE_ALT },
        new GroupAlias { Name = RoleAlias.JUDGE_NO_CLDC },
        new GroupAlias { Name = RoleAlias.JUDGE_WITH_COURT_LIST },
        new GroupAlias { Name = RoleAlias.JUDGE_TRAINING_ROLE },
        new GroupAlias { Name = RoleAlias.SENIOR_JUDGE },
        new GroupAlias { Name = RoleAlias.SENIOR_JUDGE_COURT_LIST },
        new GroupAlias { Name = RoleAlias.JUDGE_JJ_OUTLOOK_INTEGRATION },
        new GroupAlias { Name = RoleAlias.USER_ROLE_ADMIN },
        new GroupAlias { Name = RoleAlias.PRODUCT_MANAGER },
        new GroupAlias { Name = RoleAlias.PRODUCT_MANAGER_ALT },
        new GroupAlias { Name = RoleAlias.OCJ_HELP_DESK },
        new GroupAlias { Name = RoleAlias.OCJ_HELP_DESK_ALT },
        new GroupAlias { Name = RoleAlias.CHIEF_JUDGE_ACJ },
        new GroupAlias { Name = RoleAlias.CHIEF_JUDGE_ACJ_ALT }
    ];

    public required string Name { get; set; }

    public string GroupId { get; set; }
}