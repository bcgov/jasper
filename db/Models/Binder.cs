using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models;

[Collection("binders")]
public class Binder : EntityBase
{
    public Dictionary<string, string> Labels { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<BinderDocument> Documents { get; set; } = [];
}
