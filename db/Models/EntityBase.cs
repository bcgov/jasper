using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scv.Db.Models
{

    public abstract class AuditableObject
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedDate { get; set; }

        public string CreatedById { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedDate { get; set; }

        public string UpdatedById { get; set; }
    }

    public abstract class EntityBase : AuditableObject
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    }
}
