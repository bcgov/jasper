using System.Collections.Generic;
using Scv.Api.Models.Document;

namespace Scv.Api.Models;

public class DocumentBundleResponse
{
    public List<BinderDto> Binders { get; set; }
    public PdfDocumentResponse PdfResponse { get; set; }
}
