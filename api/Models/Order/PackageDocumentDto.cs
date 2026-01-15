namespace Scv.Api.Models.Order;

public class PackageDocumentDto
{
    public int? DocumentId { get; set; }
    public string DocumentTypeCd { get; set; }
    public string DocumentTypeDesc { get; set; }
    public bool Order { get; set; }
    public bool ReferredDocument { get; set; }
}
