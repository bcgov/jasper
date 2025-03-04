using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models
{
    [Collection("users")]
    public class User : EntityBase
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }

        public bool IsActive { get; set; }

        public required string GroupId { get; set; }
    }
}
