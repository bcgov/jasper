namespace Scv.Models.Archive;

public class ArchiveRequest
{
    public string ZipName { get; set; }
    public List<CsrRequest> CsrRequests { get; set; } = [];
    public List<DocumentRequest> DocumentRequests { get; set; } = [];
    public List<RopRequest> RopRequests { get; set; } = [];
    public int TotalDocuments => CsrRequests.Count + DocumentRequests.Count + RopRequests.Count;
    public string VcCivilFileId { get; set; }
}

