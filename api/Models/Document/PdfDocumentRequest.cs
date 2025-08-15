using Scv.Api.Documents;

namespace Scv.Api.Models.Document;

public class PdfDocumentRequest
{
    public DocumentType Type { get; set; }
    public PdfDocumentRequestDetails Data { get; set; }
}