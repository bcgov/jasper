using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("groups")]
    public class Group : EntityBase
    {
        public const string TRAINING_AND_ADMIN = "Training and Administration";
        public const string JUDICIARY = "Judiciary";

        public string Name { get; set; }

        public string Description { get; set; }

        public List<string> RoleIds { get; set; } = [];
    }
}
