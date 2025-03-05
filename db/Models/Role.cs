using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("roles")]
    public class Role : EntityBase
    {
        public required string Name { get; set; }

        public required string Description { get; set; }

        public List<string> PermissionIds { get; set; } = [];
    }
}
