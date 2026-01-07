namespace Scv.Models.archive
{
    public class ArchiveRequest
    {
        public string ZipName { get; set; }
        public List<CsrRequest> CsrRequests { get; set; } = new List<CsrRequest>();
        public List<DocumentRequest> DocumentRequests { get; set; } = new List<DocumentRequest>();
        public List<RopRequest> RopRequests { get; set; } = new List<RopRequest>();
        public int TotalDocuments => CsrRequests.Count + DocumentRequests.Count + RopRequests.Count;
        public string VcCivilFileId { get; set; }
    }
}
