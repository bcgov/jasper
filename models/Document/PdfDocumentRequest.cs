namespace Scv.Models.Document
{
    public class PdfDocumentRequest
    {
        public DocumentType Type { get; set; }
        public PdfDocumentRequestDetails Data { get; set; }
    }
}