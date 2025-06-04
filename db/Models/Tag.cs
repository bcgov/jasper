using MongoDB.EntityFrameworkCore;

namespace Scv.Db.Models;

/// <summary>
/// User-defined keywords or terms to help judges to personalize/organize their content.
/// </summary>
[Collection("tags")]
public class Tag : EntityBase
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string UserId { get; set; }
}
