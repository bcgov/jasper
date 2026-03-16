namespace Scv.Models.Archive
{
    public class DocumentRequest
    {
        public bool IsCriminal { get; set; }
        public string PdfFileName { get; set; }
        public string Base64UrlEncodedDocumentId { get; set; }
        public string FileId { get; set; }
    }
}
