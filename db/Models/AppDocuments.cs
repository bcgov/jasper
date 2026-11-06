using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.APP_DOCUMENTS)]
public class AppDocument : EntityBase
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public int Size { get; set; }
    public byte[] Data { get; set; }
}
