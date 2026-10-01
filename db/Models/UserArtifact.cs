using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;
using Scv.Models.UserArtifacts;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.USER_ARTIFACTS)]
public class UserArtifact : EntityBase
{
    public string UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ArtifactType ArtifactType { get; set; }

    public Dictionary<string, string> Labels { get; set; } = [];

    public DateTime? DeletedDate { get; set; }

    public string DeletedByUserId { get; set; }
}

public class Note : UserArtifact
{
    public string Content { get; set; }
    public DateTime? RelatedDate { get; set; }
    public List<string> Tags { get; set; } = [];
}

public class Annotation : UserArtifact
{
    public string DocumentId { get; set; }
    public string DocumentHash { get; set; }
    public DateTime? DocumentSourceDate { get; set; }
    public bool IsStale { get; set; }
}
