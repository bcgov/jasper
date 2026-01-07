using Scv.Models.Document;

namespace Scv.Models
{
    public class DocumentBundleResponse
    {
        public List<BinderDto> Binders { get; set; }
        public PdfDocumentResponse PdfResponse { get; set; }
    }
}
