using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scv.Db.Models
{
    public abstract class EntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    }
}
