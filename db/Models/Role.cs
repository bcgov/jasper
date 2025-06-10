
using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models
{
    [Collection(CollectionNameConstants.ROLES)]
    public class Role : EntityBase
    {
        public const string ADMIN = "System Administrator";
        public const string TRAINER = "Trainer";
        public const string PO_MANAGER = "Product Owner/Manager";
        public const string OCJ_SERVICE_DESK = "OCJ Service Desk";

        public const string JUDGE = "Judge";
        public const string ACJ_CHIEF_JUDGE = "ACJ/Chief Judge";
        public const string RAJ = "RAJ";

        public static readonly List<Role> ALL_ROLES =
        [
            // Training and Admin
            new Role
            {
                Name = ADMIN,
                Description = "Role for system administrators",
            },
            new Role
            {
                Name = TRAINER,
                Description = "Role for trainers",
            },
            new Role
            {
                Name = PO_MANAGER,
                Description = "Role for product owners or managers"
            },
            new Role
            {
                Name = OCJ_SERVICE_DESK,
                Description = "Role for OCJ service desk"
            },
            // Judiciary
            new Role
            {
                Name = JUDGE,
                Description = "Role for provincial judge"
            },
            new Role
            {
                Name = ACJ_CHIEF_JUDGE,
                Description = "Role for ACJ or chief judge"
            },
            new Role
            {
                Name = RAJ,
                Description = "Role for regional administrative judges"
            },
        ];


        public required string Name { get; set; }

        public required string Description { get; set; }

        public List<string> PermissionIds { get; set; } = [];
    }
}
