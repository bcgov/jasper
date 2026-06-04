using Scv.Models.Document;

namespace Scv.Api.Documents;

public interface IPdfMergePreparationStrategyResolver
{
    PdfMergePreparationMode Resolve(PdfDocumentRequest documentRequest);
}