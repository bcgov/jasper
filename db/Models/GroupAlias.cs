using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models
{
    [Collection(CollectionNameConstants.GROUP_ALIASES)]
    public class GroupAlias : EntityBase
    {
        public const string REGIONAL_ADMINISTRATIVE_JUDGE = "Regional Administrative Judge";
        public const string RAJ_WITH_CC_VIEW = "RAJ with CC View";
        public const string JUDGE = "Judge";
        public const string JUDGE_NO_CLDC = "Judge - No CLDC";
        public const string JUDGE_WITH_COURT_LIST = "Judge with Court List";
        public const string JUDGE_TRAINING_ROLE = "Judge Training Role";
        public const string SENIOR_JUDGE = "Senior Judge";
        public const string SENIOR_JUDGE_COURT_LIST = "Senior Judge - Court List";
        public const string JUDGE_JJ_OUTLOOK_INTEGRATION = "Judge/JJ Outlook Integration";
        public const string USER_ROLE_ADMIN = "User Role Admin";
        public const string PRODUCT_MANAGER = "Product Manager";
        public const string OCJ_HELP_DESK = "OCJ Help Desk";
        public const string CHIEF_JUDGE_ACJ = "Chief Judge/ACJs";


        public static readonly List<GroupAlias> ALL_GROUP_ALIASES =
        [
            new GroupAlias
            {
                Name = GroupAlias.REGIONAL_ADMINISTRATIVE_JUDGE,
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
