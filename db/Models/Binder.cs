using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;
using System.Collections.Generic;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.BINDERS)]
public class Binder : EntityBase
{
    public Dictionary<string, string> Labels { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<BinderDocument> Documents { get; set; } = [];
}
