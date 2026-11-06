using Scv.Models.Document;

namespace Scv.Api.Documents;

public class TransitoryDocumentPdfMergePreparationStrategy : IPdfMergePreparationStrategy
{
    public PdfMergePreparationMode PreparationMode => PdfMergePreparationMode.Flatten;

    public bool CanHandle(PdfDocumentRequest documentRequest)
    {
        return documentRequest?.Type == DocumentType.TransitoryDocument;
    }
}
