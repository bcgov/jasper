using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.ROLES_ALIASES)]
public class RoleAlias : EntityBase
{
    public const string CHIEF_JUDGE_ACJ = "Chief Judge/ACJ";
    public const string CHIEF_JUDGE_ACJ_ALT = "Chief Judge/ACJs";
    public const string JUDGE = "JUDGE";
    public const string JUDGE_ALT = "Judge";
    public const string JUDGE_TRAINING = "Judge Training";
    public const string OCJ_HELP_DESK = "OCJ_HELP_DESK";
    public const string OCJ_HELP_DESK_ALT = "OCJ Help Desk";
    public const string OCJ_IT = "OCJ_IT";
    public const string PRODUCT_MANAGER = "PRODUCT_MANAGER";
    public const string PRODUCT_MANAGER_ALT = "Product Manager";
    public const string REGIONAL_ADMINISTRATIVE_JUDGE = "REGIONAL_ADMINISTRATIVE_JUDGE";
    public const string REGIONAL_ADMINISTRATIVE_JUDGE_ALT = "Regional Administrative Judge";
    public const string SENIOR_JUDGE = "Senior Judge";
    public const string USER_ROLE_ADMIN = "Admin User Edit";

    public static readonly List<RoleAlias> ALL_ROLE_ALIASES =
    [
        new RoleAlias { Name = RoleAlias.CHIEF_JUDGE_ACJ },
        new RoleAlias { Name = RoleAlias.CHIEF_JUDGE_ACJ_ALT },
        new RoleAlias { Name = RoleAlias.JUDGE },
        new RoleAlias { Name = RoleAlias.JUDGE_ALT },
        new RoleAlias { Name = RoleAlias.JUDGE_TRAINING },
        new RoleAlias { Name = RoleAlias.OCJ_HELP_DESK },
        new RoleAlias { Name = RoleAlias.OCJ_HELP_DESK_ALT },
        new RoleAlias { Name = RoleAlias.OCJ_IT },
        new RoleAlias { Name = RoleAlias.PRODUCT_MANAGER },
        new RoleAlias { Name = RoleAlias.PRODUCT_MANAGER_ALT },
        new RoleAlias { Name = RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE },
        new RoleAlias { Name = RoleAlias.REGIONAL_ADMINISTRATIVE_JUDGE_ALT },
        new RoleAlias { Name = RoleAlias.SENIOR_JUDGE },
        new RoleAlias { Name = RoleAlias.USER_ROLE_ADMIN },
    ];

    public required string Name { get; set; }

    public string RoleId { get; set; }
}
