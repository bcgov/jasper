using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("permissions")]
    public class Permission
    {
        public const string UPDATE_PERMISSIONS_GROUPS = "UPDATE_PERMISSIONS_GROUPS";
        public const string UPDATE_POSITIONS = "UPDATE_POSITIONS";
        public const string UPDATE_POSITIONS_PCSS_MAPPINGS = "UPDATE_POSITIONS_PCSS_MAPPINGS";
        public const string LOCK_UNLOCK_USERS = "LOCK_UNLOCK_USERS";

        public const string VIEW_DASHBOARD = "VIEW_DASHBOARD";


        public static readonly List<Permission> ALL_PERMISIONS =
        [
            new Permission
            {
                Code = UPDATE_PERMISSIONS_GROUPS,
                Name = "Update Permission Groups",
                Description = "Permissions to update permission groups",
                IsActive = true,
            },
            new Permission
            {
                Code = UPDATE_POSITIONS,
                Name = "Update Positions",
                Description = "Permissions to update positions",
                IsActive = true,
            },
            new Permission
            {
                Code = UPDATE_POSITIONS_PCSS_MAPPINGS,
                Name = "Update Position Mappings relative to PCSS",
                Description = "Permissions to update posisiont mappings relative to PCSS",
                IsActive = true,
            },
            new Permission
            {
                Code = LOCK_UNLOCK_USERS,
                Name = "Lock/Unlock Users",
                Description = "Permissions to lock or unlock users",
                IsActive = true,
            },
            // new Permission
            // {
            //     Code = VIEW_DASHBOARD,
            //     Name = "View Dashboard",
            //     Description = "Permissions to allow or block viewing of Dashboard",
            //     IsActive = true,
            // }
        ];

        public ObjectId Id { get; set; }

        public required string Name { get; set; }

        public required string Code { get; set; }

        public required string Description { get; set; }

        public bool IsActive { get; set; }
    }
}
