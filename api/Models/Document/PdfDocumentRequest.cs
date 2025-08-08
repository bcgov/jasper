namespace Scv.Api.Models.Document;

public class PdfDocumentRequest
{
    public string Type { get; set; }
    public PdfDocumentRequestDetails Data { get; set; }
}