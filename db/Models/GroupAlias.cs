using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models
{
    [Collection(CollectionNameConstants.GROUP_ALIASES)]
    public class GroupAlias : EntityBase
    {
        public const string REGIONAL_ADMINISTRATIVE_JUDGE = "REGIONAL_ADMINISTRATIVE_JUDGE";
        public const string RAJ_WITH_CC_VIEW = "RAJ with CC view";
        public const string JUDGE = "JUDGE";
        public const string JUDGE_NO_CLDC = "Judge - No CLDC";
        public const string JUDGE_WITH_COURT_LIST = "Court List Pilot Judge";
        public const string JUDGE_TRAINING_ROLE = "Judge Training";
        public const string SENIOR_JUDGE = "Senior Judge";
        public const string SENIOR_JUDGE_COURT_LIST = "Senior Judge - Court List";
        public const string JUDGE_JJ_OUTLOOK_INTEGRATION = "OUTLOOK_INTEGRATION";
        public const string USER_ROLE_ADMIN = "Admin User Edit";
        public const string PRODUCT_MANAGER = "PRODUCT_MANAGER";
        public const string OCJ_HELP_DESK = "OCJ_HELP_DESK";
        public const string CHIEF_JUDGE_ACJ = "Chief Judge/ACJ";


        public static readonly List<GroupAlias> ALL_GROUP_ALIASES =
        [
            new GroupAlias
            {
                Name = GroupAlias.REGIONAL_ADMINISTRATIVE_JUDGE,
            },
            new GroupAlias
            {
                Name = GroupAlias.RAJ_WITH_CC_VIEW,
            },
            new GroupAlias
            {
                Name = GroupAlias.JUDGE,
            },
            new GroupAlias
            {
                Name = GroupAlias.JUDGE_NO_CLDC,
            },
            new GroupAlias
            {
                Name = GroupAlias.JUDGE_WITH_COURT_LIST,
            },
            new GroupAlias
            {
                Name = GroupAlias.JUDGE_TRAINING_ROLE,
            },
            new GroupAlias
            {
                Name = GroupAlias.SENIOR_JUDGE,
            },
            new GroupAlias
            {
                Name = GroupAlias.SENIOR_JUDGE_COURT_LIST,
            },
            new GroupAlias
            {
                Name = GroupAlias.JUDGE_JJ_OUTLOOK_INTEGRATION,
            },
            new GroupAlias
            {
                Name = GroupAlias.USER_ROLE_ADMIN,
            },
            new GroupAlias
            {
                Name = GroupAlias.PRODUCT_MANAGER,
            },
            new GroupAlias
            {
                Name = GroupAlias.OCJ_HELP_DESK,
            },
            new GroupAlias
            {
                Name = GroupAlias.CHIEF_JUDGE_ACJ,
            }
        ];

        public required string Name { get; set; }

        public string GroupId { get; set; }

    }
}
