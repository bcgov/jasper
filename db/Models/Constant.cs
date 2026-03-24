using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.CONSTANTS)]
public class Constant : EntityBase
{
    public required string Key { get; set; }
    public List<string> Values { get; set; } = [];
}
