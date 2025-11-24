using System;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.QUICK_LINKS)]
public class QuickLink : EntityBase
{
    public required string Name { get; set; }
    public string ParentName { get; set; }
    public bool IsMenu { get; set; } = false;
    public string URL { get; set; }
    public int Order { get; set; }
    public string JudgeId { get; set; }
}