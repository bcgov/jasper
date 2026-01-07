using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Scv.Db.Models
{

    public abstract class AuditableObject
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Ent_Dtm { get; set; }

        public string Ent_UserId { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Upd_Dtm { get; set; }

        public string Upd_UserId { get; set; }
    }

    public abstract class EntityBase : AuditableObject
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    }
}
