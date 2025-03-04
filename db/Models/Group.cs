using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("groups")]
    public class Group : EntityBase
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public List<string> RoleIds { get; set; } = [];
    }
}
