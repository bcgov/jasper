using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.APP_DOCUMENTS)]
public class AppDocument : EntityBase
{
    public const string SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE = "SCL_ADDTL_PAGE_TEMPLATE";

    public static readonly List<AppDocument> ALL_APP_DOCUMENTS =
    [
        new AppDocument
        {
            Key = SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE,
            ContentType = "application/pdf",
            TargetSize = 500 * 1024, // 500 KB
        }
    ];

    public string Key { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public int TargetSize { get; set; }
    public int? Size { get; set; }
    public byte[]? Data { get; set; }
}
