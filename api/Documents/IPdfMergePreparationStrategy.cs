using Scv.Models.Document;

namespace Scv.Api.Documents;

public interface IPdfMergePreparationStrategy
{
    bool CanHandle(PdfDocumentRequest documentRequest);

    PdfMergePreparationMode PreparationMode { get; }
}