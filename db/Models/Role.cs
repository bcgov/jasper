using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("roles")]
    public class Role : EntityBase
    {
        public const string ADMIN = "Admin";
        public const string TRAINER = "Trainer";
        public const string JUDGE = "Judge";

        public static readonly List<Role> ALL_ROLES =
        [
            new Role
            {
                Name = ADMIN,
                Description = "Role for JASPER system administrators",
            },
            new Role
            {
                Name = TRAINER,
                Description = "Role for JASPER trainers",
            },
            new Role
            {
                Name = JUDGE,
                Description = "Role for provincial judge"
            }
        ];


        public required string Name { get; set; }

        public required string Description { get; set; }

        public List<string> PermissionIds { get; set; } = [];
    }
}
